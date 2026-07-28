using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenRestoApi.Core.Application.Services;

namespace OpenRestoApi.Infrastructure.Auth;

public sealed class WhatsAppChannelAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly IConfiguration _configuration = configuration;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string token = header["Bearer ".Length..].Trim();
        string? configuredToken = _configuration["WhatsAppChannel:Token"];
        if (string.IsNullOrWhiteSpace(configuredToken) ||
            !string.Equals(token, configuredToken, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid WhatsApp channel credential."));
        }

        string? verifiedPhone = Request.Headers[WhatsAppChannelAuthenticationDefaults.VerifiedPhoneHeader];
        if (string.IsNullOrWhiteSpace(verifiedPhone))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing verified WhatsApp phone header."));
        }

        try
        {
            (string e164, string normalized) = WhatsAppPhoneOwnershipService.Normalize(verifiedPhone);
            var claims = new List<Claim>
            {
                new(WhatsAppChannelAuthenticationDefaults.VerifiedPhoneClaim, e164),
                new(WhatsAppChannelAuthenticationDefaults.VerifiedPhoneNormalizedClaim, normalized)
            };

            var identity = new ClaimsIdentity(claims, WhatsAppChannelAuthenticationDefaults.SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, WhatsAppChannelAuthenticationDefaults.SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex.Message));
        }
    }
}
