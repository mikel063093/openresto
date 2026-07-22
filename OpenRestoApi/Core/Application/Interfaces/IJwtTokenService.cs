using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Interfaces;

/// <summary>
/// Mints HS256 JWT bearer tokens carrying the admin email + Admin role claim,
/// 30-day expiry. Key/issuer/audience resolved from <c>Jwt:*</c> config with a
/// <c>JWT_KEY</c> env-var fallback for the signing key.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Returns a signed JWT for the given user email and role.</summary>
    string Generate(string email, AdminRole role = AdminRole.SuperAdmin);
}
