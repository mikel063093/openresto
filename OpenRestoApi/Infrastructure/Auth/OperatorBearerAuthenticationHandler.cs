using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenRestoApi.Core.Application.Services;

namespace OpenRestoApi.Infrastructure.Auth;

public sealed class OperatorBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    OperatorCredentialService credentialService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly OperatorCredentialService _credentialService = credentialService;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string token = header["Bearer ".Length..].Trim();
        OperatorCredentialValidationResult? validated = await _credentialService.ValidateAsync(token);
        if (validated is null)
        {
            return AuthenticateResult.Fail("Invalid operator credential.");
        }

        var claims = new List<Claim>
        {
            new(OperatorAuthenticationDefaults.OperatorIdClaim, validated.Operator.Id.ToString()),
            new(OperatorAuthenticationDefaults.OperatorIdentifierClaim, validated.Operator.Identifier),
            new(OperatorAuthenticationDefaults.CredentialIdClaim, validated.Credential.Id.ToString()),
        };
        claims.AddRange(validated.RestaurantIds.Select(id => new Claim(OperatorAuthenticationDefaults.RestaurantIdClaim, id.ToString())));

        var identity = new ClaimsIdentity(claims, OperatorAuthenticationDefaults.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, OperatorAuthenticationDefaults.SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
