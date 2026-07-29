using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OpenRestoReservationBot.Options;

namespace OpenRestoReservationBot.Security;

public sealed class BotInternalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ReservationBotOptions> reservationBotOptions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly ReservationBotOptions _reservationBotOptions = reservationBotOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? header = Request.Headers.Authorization;
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string token = header["Bearer ".Length..].Trim();
        if (!ConstantTimeEquals(token, _reservationBotOptions.InternalCredential))
        {
            return Task.FromResult(AuthenticateResult.Fail("Credencial interna inválida."));
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "n8n-internal"),
            new Claim(ClaimTypes.Name, "n8n-internal")
        ], BotInternalAuthenticationDefaults.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), BotInternalAuthenticationDefaults.SchemeName)));
    }

    private static bool ConstantTimeEquals(string candidate, string configured)
    {
        byte[] left = Encoding.UTF8.GetBytes(candidate);
        byte[] right = Encoding.UTF8.GetBytes(configured);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }
}
