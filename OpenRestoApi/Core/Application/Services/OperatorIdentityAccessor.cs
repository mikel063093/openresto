using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OpenRestoApi.Infrastructure.Auth;

namespace OpenRestoApi.Core.Application.Services;

public sealed class OperatorIdentityAccessor(IHttpContextAccessor httpContextAccessor)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public OperatorIdentityContext GetCurrent()
    {
        ClaimsPrincipal user = _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No current HTTP user is available.");

        string? operatorId = user.FindFirstValue(OperatorAuthenticationDefaults.OperatorIdClaim);
        string? operatorIdentifier = user.FindFirstValue(OperatorAuthenticationDefaults.OperatorIdentifierClaim);
        string? credentialId = user.FindFirstValue(OperatorAuthenticationDefaults.CredentialIdClaim);

        if (!int.TryParse(operatorId, out int parsedOperatorId) || !int.TryParse(credentialId, out int parsedCredentialId))
        {
            throw new InvalidOperationException("Operator claims are missing or invalid.");
        }

        int[] restaurantIds = user.FindAll(OperatorAuthenticationDefaults.RestaurantIdClaim)
            .Select(x => int.TryParse(x.Value, out int id) ? id : 0)
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        return new OperatorIdentityContext(
            parsedOperatorId,
            operatorIdentifier ?? parsedOperatorId.ToString(),
            parsedCredentialId,
            restaurantIds,
            _httpContextAccessor.HttpContext?.TraceIdentifier);
    }
}

public sealed record OperatorIdentityContext(
    int OperatorId,
    string OperatorIdentifier,
    int CredentialId,
    IReadOnlyList<int> RestaurantIds,
    string? CorrelationId);
