using OpenRestoReservationBot.Infrastructure;

namespace OpenRestoReservationBot.Services;

internal static class BotHttpResponseExceptionFactory
{
    public static async Task<BotRequestException> CreateAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string fallbackMessage = $"OpenResto respondió con estado {(int)response.StatusCode}.";

        if (response.Content.Headers.ContentLength == 0)
        {
            return new BotRequestException((int)response.StatusCode, fallbackMessage);
        }

        try
        {
            BotUpstreamMessage? upstreamMessage = await response.Content.ReadFromJsonAsync<BotUpstreamMessage>(cancellationToken);
            if (!string.IsNullOrWhiteSpace(upstreamMessage?.Message))
            {
                return new BotRequestException((int)response.StatusCode, upstreamMessage.Message);
            }
        }
        catch
        {
            // Ignore parse failures and fall back to the generic message.
        }

        return new BotRequestException((int)response.StatusCode, fallbackMessage);
    }

    private sealed class BotUpstreamMessage
    {
        public string? Message { get; set; }
    }
}
