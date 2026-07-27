using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public interface IAdminActorAccessor
{
    Task<AdminActorSnapshot> GetRequiredSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record AdminActorSnapshot(string NormalizedEmail, int? AdminCredentialId);

public sealed class AdminActorAccessor(
    IHttpContextAccessor httpContextAccessor,
    AppDbContext db) : IAdminActorAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly AppDbContext _db = db;

    public async Task<AdminActorSnapshot> GetRequiredSnapshotAsync(CancellationToken cancellationToken = default)
    {
        string? email = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Authenticated admin email is required.");
        }

        string normalizedEmail = email.Trim().ToLowerInvariant();
        int? adminCredentialId = await _db.AdminCredentials
            .AsNoTracking()
            .Where(x => x.Email == normalizedEmail)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return new AdminActorSnapshot(normalizedEmail, adminCredentialId);
    }
}
