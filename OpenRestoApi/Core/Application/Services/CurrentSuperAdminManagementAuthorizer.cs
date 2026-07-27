using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Auth;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public interface ICurrentSuperAdminManagementAuthorizer
{
    Task<AdminCredential?> TryAuthorizeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
}

public sealed class CurrentSuperAdminManagementAuthorizer(AppDbContext db) : ICurrentSuperAdminManagementAuthorizer
{
    private readonly AppDbContext _db = db;

    public async Task<AdminCredential?> TryAuthorizeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        string? emailClaim = user.FindFirstValue(ClaimTypes.Email);
        string? adminCredentialIdClaim = user.FindFirstValue(AdminAuthenticationDefaults.AdminCredentialIdClaim);
        if (string.IsNullOrWhiteSpace(emailClaim) || !int.TryParse(adminCredentialIdClaim, out int adminCredentialId))
        {
            return null;
        }

        string normalizedEmail = emailClaim.Trim().ToLowerInvariant();
        AdminCredential? credential = await _db.AdminCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == adminCredentialId, cancellationToken);

        if (credential is null ||
            !credential.IsActive ||
            credential.Role != AdminRole.SuperAdmin ||
            !string.Equals(credential.Email, normalizedEmail, StringComparison.Ordinal))
        {
            return null;
        }

        return credential;
    }
}

public sealed class CurrentSuperAdminManagementRequirement : IAuthorizationRequirement;

public sealed class CurrentSuperAdminManagementRequirementHandler(
    ICurrentSuperAdminManagementAuthorizer authorizer) : AuthorizationHandler<CurrentSuperAdminManagementRequirement>
{
    private readonly ICurrentSuperAdminManagementAuthorizer _authorizer = authorizer;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CurrentSuperAdminManagementRequirement requirement)
    {
        if (await _authorizer.TryAuthorizeAsync(context.User) is not null)
        {
            context.Succeed(requirement);
        }
    }
}
