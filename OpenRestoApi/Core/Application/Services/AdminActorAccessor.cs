using System.Security.Claims;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Services;

public interface IAdminActorAccessor
{
    Task<AdminActorSnapshot> GetRequiredSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record AdminActorSnapshot(string NormalizedEmail, int AdminCredentialId);

public sealed class AdminActorAccessor(
    IHttpContextAccessor httpContextAccessor,
    ICurrentSuperAdminManagementAuthorizer authorizer) : IAdminActorAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ICurrentSuperAdminManagementAuthorizer _authorizer = authorizer;

    public async Task<AdminActorSnapshot> GetRequiredSnapshotAsync(CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            throw new UnauthorizedAccessException("Authenticated admin context is required.");
        }

        AdminCredential verifiedAdmin = await _authorizer.TryAuthorizeAsync(user, cancellationToken)
            ?? throw new UnauthorizedAccessException("Current SuperAdmin authorization is required.");

        return new AdminActorSnapshot(verifiedAdmin.Email, verifiedAdmin.Id);
    }
}
