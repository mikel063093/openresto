using System.Text.RegularExpressions;
using OpenRestoReservationBot.Infrastructure;

namespace OpenRestoReservationBot.Services;

public interface IBotRequestContextAccessor
{
    BotRequestContext GetRequiredContext();
}

public sealed partial class BotRequestContextAccessor(IHttpContextAccessor httpContextAccessor) : IBotRequestContextAccessor
{
    public const string AssertionHeaderName = "X-OpenResto-Channel-Assertion";
    public const string CorrelationHeaderName = "X-Correlation-Id";

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    [GeneratedRegex("^[A-Za-z0-9._:-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeCorrelationIdRegex();

    public BotRequestContext GetRequiredContext()
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext
            ?? throw new BotRequestException(StatusCodes.Status500InternalServerError, "No hay contexto HTTP activo.");

        string assertion = httpContext.Request.Headers[AssertionHeaderName].ToString().Trim();
        if (string.IsNullOrWhiteSpace(assertion))
        {
            throw new BotRequestException(StatusCodes.Status400BadRequest, "Falta la aserción interna de n8n.");
        }

        string? incomingCorrelationId = httpContext.Request.Headers[CorrelationHeaderName].ToString().Trim();
        string correlationId = SafeCorrelationIdRegex().IsMatch(incomingCorrelationId ?? string.Empty)
            ? incomingCorrelationId!
            : httpContext.TraceIdentifier;

        return new BotRequestContext(assertion, correlationId);
    }
}

public sealed record BotRequestContext(string Assertion, string CorrelationId);
