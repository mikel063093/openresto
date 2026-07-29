using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Infrastructure.Auth;

public sealed class WhatsAppChannelAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    ChannelIdempotencyService channelIdempotencyService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ChannelIdempotencyService _channelIdempotencyService = channelIdempotencyService;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!bool.TryParse(_configuration["WhatsAppChannel:Enabled"], out bool enabled) || !enabled)
        {
            return AuthenticateResult.Fail("WhatsApp channel is disabled.");
        }

        string? header = Request.Headers.Authorization;
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string token = header["Bearer ".Length..].Trim();
        string? configuredToken = _configuration["WhatsAppChannel:InternalCallerCredential"];
        if (!ConstantTimeEquals(token, configuredToken))
        {
            return AuthenticateResult.Fail("Invalid WhatsApp channel credential.");
        }

        string? assertion = Request.Headers[WhatsAppChannelAuthenticationDefaults.AssertionHeader];
        if (string.IsNullOrWhiteSpace(assertion))
        {
            return AuthenticateResult.Fail("Missing WhatsApp channel assertion.");
        }

        try
        {
            JwtSecurityTokenHandler handler = new() { MapInboundClaims = false };
            TokenValidationParameters validationParameters = BuildValidationParameters();
            ClaimsPrincipal validatedPrincipal = handler.ValidateToken(assertion, validationParameters, out SecurityToken validatedToken);
            if (validatedToken is not JwtSecurityToken jwt)
            {
                return AuthenticateResult.Fail("Invalid WhatsApp channel assertion.");
            }

            string? activeKid = _configuration["WhatsAppChannel:Assertion:ActiveKid"];
            string? previousKid = _configuration["WhatsAppChannel:Assertion:PreviousKid"];
            if (string.IsNullOrWhiteSpace(jwt.Header.Kid) ||
                (!string.Equals(jwt.Header.Kid, activeKid, StringComparison.Ordinal) &&
                 !string.Equals(jwt.Header.Kid, previousKid, StringComparison.Ordinal)))
            {
                return AuthenticateResult.Fail("Invalid WhatsApp channel assertion.");
            }

            string? action = validatedPrincipal.FindFirstValue(WhatsAppChannelAuthenticationDefaults.ActionClaim);
            string? scope = validatedPrincipal.FindFirstValue(WhatsAppChannelAuthenticationDefaults.ScopeClaim);
            string? requiredScope = _configuration["WhatsAppChannel:Assertion:RequiredScope"];
            string? requiredAction = ResolveRequiredAction(Request);

            if (string.IsNullOrWhiteSpace(action) ||
                string.IsNullOrWhiteSpace(scope) ||
                string.IsNullOrWhiteSpace(requiredScope) ||
                !string.Equals(action, requiredAction, StringComparison.Ordinal) ||
                !ScopeContains(scope, requiredScope))
            {
                return AuthenticateResult.Fail("WhatsApp channel assertion scope is invalid.");
            }

            string? jwtId = jwt.Id;
            if (string.IsNullOrWhiteSpace(jwtId) || await HasAssertionBeenReplayedAsync(jwtId))
            {
                return AuthenticateResult.Fail("WhatsApp channel assertion replay detected.");
            }

            string? verifiedPhone = jwt.Subject;
            (string e164, string normalized) = WhatsAppPhoneOwnershipService.Normalize(verifiedPhone ?? string.Empty);
            var claims = new List<Claim>
            {
                new(WhatsAppChannelAuthenticationDefaults.VerifiedPhoneClaim, e164),
                new(WhatsAppChannelAuthenticationDefaults.VerifiedPhoneNormalizedClaim, normalized),
                new(WhatsAppChannelAuthenticationDefaults.ActionClaim, action),
            };

            var identity = new ClaimsIdentity(claims, WhatsAppChannelAuthenticationDefaults.SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, WhatsAppChannelAuthenticationDefaults.SchemeName);
            return AuthenticateResult.Success(ticket);
        }
        catch (InvalidOperationException)
        {
            return AuthenticateResult.Fail("Invalid WhatsApp channel assertion.");
        }
        catch (SecurityTokenException)
        {
            return AuthenticateResult.Fail("Invalid WhatsApp channel assertion.");
        }
    }

    private TokenValidationParameters BuildValidationParameters()
    {
        string signingKey = _configuration["WhatsAppChannel:Assertion:SigningKey"]
            ?? throw new InvalidOperationException("Missing WhatsApp assertion signing key.");
        string activeKid = _configuration["WhatsAppChannel:Assertion:ActiveKid"]
            ?? throw new InvalidOperationException("Missing WhatsApp assertion active kid.");
        string? previousSigningKey = _configuration["WhatsAppChannel:Assertion:PreviousSigningKey"];
        string? previousKid = _configuration["WhatsAppChannel:Assertion:PreviousKid"];
        string issuer = _configuration["WhatsAppChannel:Assertion:Issuer"]
            ?? throw new InvalidOperationException("Missing WhatsApp assertion issuer.");
        string audience = _configuration["WhatsAppChannel:Assertion:Audience"]
            ?? throw new InvalidOperationException("Missing WhatsApp assertion audience.");

        Dictionary<string, SecurityKey> signingKeys = new(StringComparer.Ordinal)
        {
            [activeKid] = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };

        if (!string.IsNullOrWhiteSpace(previousKid) && !string.IsNullOrWhiteSpace(previousSigningKey))
        {
            signingKeys[previousKid] = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(previousSigningKey));
        }

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, kid, _) =>
            {
                if (string.IsNullOrWhiteSpace(kid) || !signingKeys.TryGetValue(kid, out SecurityKey? key))
                {
                    return [];
                }

                return [key];
            },
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = WhatsAppChannelAuthenticationDefaults.ActionClaim,
        };
    }

    private async Task<bool> HasAssertionBeenReplayedAsync(string jwtId)
    {
        string requiredAction = ResolveRequiredAction(Request);
        string replayFingerprint = $"{Request.Method}:{Request.Path}:{requiredAction}";
        ChannelReplayRegistrationResult registration = await _channelIdempotencyService.RegisterReplayKeyAsync(
            channel: "whatsapp_assertion",
            replayKey: jwtId,
            mutationScope: requiredAction,
            fingerprint: replayFingerprint,
            expiresAtUtc: DateTime.UtcNow.AddMinutes(5));
        return registration.WasReplayed;
    }

    private static bool ConstantTimeEquals(string candidate, string? configured)
    {
        if (string.IsNullOrEmpty(configured))
        {
            return false;
        }

        byte[] left = Encoding.UTF8.GetBytes(candidate);
        byte[] right = Encoding.UTF8.GetBytes(configured);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static bool ScopeContains(string scope, string requiredScope)
        => scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(requiredScope, StringComparer.Ordinal);

    private static string ResolveRequiredAction(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method))
        {
            return "reservations.read";
        }

        return "reservations.mutate";
    }
}
