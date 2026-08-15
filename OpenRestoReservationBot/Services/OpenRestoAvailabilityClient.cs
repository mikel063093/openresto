using System.Globalization;
using OpenRestoReservationBot.Contracts;
using OpenRestoReservationBot.Infrastructure;

namespace OpenRestoReservationBot.Services;

public interface IOpenRestoAvailabilityClient
{
    Task<OpenRestoAvailabilityDto> LookupAsync(AvailabilityLookupRequest request, BotRequestContext context, CancellationToken cancellationToken);
}

public sealed class OpenRestoAvailabilityClient(IHttpClientFactory httpClientFactory) : IOpenRestoAvailabilityClient
{
    public const string ClientName = "OpenRestoAvailability";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<OpenRestoAvailabilityDto> LookupAsync(
        AvailabilityLookupRequest request,
        BotRequestContext context,
        CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient(ClientName);
        string date = Uri.EscapeDataString(request.Date.ToString("O", CultureInfo.InvariantCulture));
        string path = $"/api/restaurants/{request.RestaurantId}/availability?date={date}&seats={request.Seats}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, path);
        httpRequest.Headers.TryAddWithoutValidation(BotRequestContextAccessor.AssertionHeaderName, context.Assertion);
        httpRequest.Headers.TryAddWithoutValidation(BotRequestContextAccessor.CorrelationHeaderName, context.CorrelationId);

        using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);
        return await ReadRequiredContentAsync<OpenRestoAvailabilityDto>(response, cancellationToken);
    }

    private static async Task<T> ReadRequiredContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await BotHttpResponseExceptionFactory.CreateAsync(response, cancellationToken);
        }

        T? payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        return payload ?? throw new BotRequestException(StatusCodes.Status502BadGateway, "OpenResto devolvió una respuesta vacía.");
    }
}
