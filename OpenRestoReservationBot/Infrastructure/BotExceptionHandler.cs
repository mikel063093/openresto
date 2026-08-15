using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace OpenRestoReservationBot.Infrastructure;

public sealed class BotExceptionHandler(ILogger<BotExceptionHandler> logger, PiiRedactor piiRedactor) : IExceptionHandler
{
    private readonly ILogger<BotExceptionHandler> _logger = logger;
    private readonly PiiRedactor _piiRedactor = piiRedactor;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int statusCode = exception is BotRequestException requestException
            ? requestException.StatusCode
            : StatusCodes.Status500InternalServerError;

        string publicMessage = exception is BotRequestException
            ? exception.Message
            : "Ocurrió un error interno en el bot.";

        _logger.LogError(
            exception,
            "Reservation bot failure. traceId={TraceId} status={StatusCode} message={Message}",
            httpContext.TraceIdentifier,
            statusCode,
            _piiRedactor.Redact(exception.Message));

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(new { message = publicMessage }),
            cancellationToken);

        return true;
    }
}
