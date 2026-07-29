using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using OpenRestoReservationBot.Contracts;
using OpenRestoReservationBot.Infrastructure;
using OpenRestoReservationBot.Options;

namespace OpenRestoReservationBot.Services;

public interface IOpenRestoPrivateChannelClient
{
    Task<IReadOnlyList<OpenRestoReservationDto>> ListReservationsAsync(BotRequestContext context, CancellationToken cancellationToken);
    Task<OpenRestoReservationDto> GetReservationAsync(int reservationId, BotRequestContext context, CancellationToken cancellationToken);
    Task<OpenRestoReservationDto> CreateReservationAsync(ReservationCreateRequest request, BotRequestContext context, CancellationToken cancellationToken);
    Task<OpenRestoReservationDto> UpdateReservationAsync(ReservationUpdateRequest request, BotRequestContext context, CancellationToken cancellationToken);
    Task<OpenRestoReservationActionResultDto> CancelReservationAsync(ReservationCancelRequest request, BotRequestContext context, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpenRestoOccasionCatalogItemDto>> GetOccasionCatalogAsync(int restaurantId, BotRequestContext context, CancellationToken cancellationToken);
    Task<OpenRestoHandoffResultDto> CreateHandoffAsync(ReservationHandoffRequest request, BotRequestContext context, CancellationToken cancellationToken);
}

public sealed class OpenRestoPrivateChannelClient(
    IHttpClientFactory httpClientFactory,
    IOptions<ReservationBotOptions> reservationBotOptions) : IOpenRestoPrivateChannelClient
{
    public const string ClientName = "OpenRestoPrivateChannel";
    private const string OperationHeaderName = "X-OpenResto-Bot-Operation";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ReservationBotOptions _reservationBotOptions = reservationBotOptions.Value;

    public async Task<IReadOnlyList<OpenRestoReservationDto>> ListReservationsAsync(BotRequestContext context, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreatePrivateRequest(HttpMethod.Get, "/api/private/channels/whatsapp/reservations", context, "list");
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredContentAsync<List<OpenRestoReservationDto>>(response, cancellationToken);
    }

    public async Task<OpenRestoReservationDto> GetReservationAsync(int reservationId, BotRequestContext context, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreatePrivateRequest(HttpMethod.Get, $"/api/private/channels/whatsapp/reservations/{reservationId}", context, "detail");
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredContentAsync<OpenRestoReservationDto>(response, cancellationToken);
    }

    public async Task<OpenRestoReservationDto> CreateReservationAsync(
        ReservationCreateRequest request,
        BotRequestContext context,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = CreatePrivateRequest(HttpMethod.Post, "/api/private/channels/whatsapp/reservations", context, "create");
        httpRequest.Content = JsonContent.Create(new OpenRestoWhatsAppCreateReservationRequestDto
        {
            RestaurantId = request.RestaurantId,
            Date = request.Date,
            Seats = request.Seats,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            SpecialRequests = request.SpecialRequests,
            Confirmed = request.Confirmed,
            IdempotencyKey = request.IdempotencyKey,
            OccasionCatalogItemIds = request.OccasionCatalogItemIds
        });

        using HttpResponseMessage response = await SendAsync(httpRequest, cancellationToken);
        return await ReadRequiredContentAsync<OpenRestoReservationDto>(response, cancellationToken);
    }

    public async Task<OpenRestoReservationDto> UpdateReservationAsync(
        ReservationUpdateRequest request,
        BotRequestContext context,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = CreatePrivateRequest(HttpMethod.Patch, $"/api/private/channels/whatsapp/reservations/{request.ReservationId}", context, "update");
        httpRequest.Content = JsonContent.Create(new OpenRestoWhatsAppUpdateReservationRequestDto
        {
            Date = request.Date,
            Seats = request.Seats,
            Confirmed = request.Confirmed,
            IdempotencyKey = request.IdempotencyKey,
            ExpectedConcurrencyToken = request.ExpectedConcurrencyToken
        });

        using HttpResponseMessage response = await SendAsync(httpRequest, cancellationToken);
        return await ReadRequiredContentAsync<OpenRestoReservationDto>(response, cancellationToken);
    }

    public async Task<OpenRestoReservationActionResultDto> CancelReservationAsync(
        ReservationCancelRequest request,
        BotRequestContext context,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = CreatePrivateRequest(HttpMethod.Post, $"/api/private/channels/whatsapp/reservations/{request.ReservationId}/cancel", context, "cancel");
        httpRequest.Content = JsonContent.Create(new OpenRestoWhatsAppCancelReservationRequestDto
        {
            Confirmed = request.Confirmed,
            IdempotencyKey = request.IdempotencyKey,
            ExpectedConcurrencyToken = request.ExpectedConcurrencyToken,
            Reason = request.Reason
        });

        using HttpResponseMessage response = await SendAsync(httpRequest, cancellationToken);
        return await ReadRequiredContentAsync<OpenRestoReservationActionResultDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<OpenRestoOccasionCatalogItemDto>> GetOccasionCatalogAsync(
        int restaurantId,
        BotRequestContext context,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = CreatePrivateRequest(HttpMethod.Get, $"/api/private/channels/whatsapp/restaurants/{restaurantId}/occasion-catalog", context, "occasionCatalog");
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredContentAsync<List<OpenRestoOccasionCatalogItemDto>>(response, cancellationToken);
    }

    public async Task<OpenRestoHandoffResultDto> CreateHandoffAsync(
        ReservationHandoffRequest request,
        BotRequestContext context,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = CreatePrivateRequest(HttpMethod.Post, "/api/private/channels/whatsapp/handoffs", context, "handoff");
        httpRequest.Content = JsonContent.Create(new OpenRestoWhatsAppHandoffRequestDto
        {
            RestaurantId = request.RestaurantId,
            BookingId = request.BookingId,
            Summary = request.Summary,
            Confirmed = request.Confirmed,
            IdempotencyKey = request.IdempotencyKey
        });

        using HttpResponseMessage response = await SendAsync(httpRequest, cancellationToken);
        return await ReadRequiredContentAsync<OpenRestoHandoffResultDto>(response, cancellationToken);
    }

    private HttpRequestMessage CreatePrivateRequest(HttpMethod method, string relativePath, BotRequestContext context, string operation)
    {
        var request = new HttpRequestMessage(method, relativePath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _reservationBotOptions.OpenResto.PrivateApiCredential);
        request.Headers.TryAddWithoutValidation(BotRequestContextAccessor.AssertionHeaderName, context.Assertion);
        request.Headers.TryAddWithoutValidation(BotRequestContextAccessor.CorrelationHeaderName, context.CorrelationId);
        request.Headers.TryAddWithoutValidation(OperationHeaderName, operation);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient(ClientName);
        return await client.SendAsync(request, cancellationToken);
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
