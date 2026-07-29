using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoReservationBot.Contracts;
using OpenRestoReservationBot.Services;

namespace OpenRestoReservationBot.Tests.Integration;

public sealed class BotTestWebAppFactory : WebApplicationFactory<Program>
{
    public const string InternalCredential = "test-n8n-bot-internal-credential";

    public RecordingAvailabilityClient AvailabilityClient { get; } = new();
    public RecordingPrivateChannelClient PrivateChannelClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ReservationBot:InternalCredential", InternalCredential);
        builder.UseSetting("ReservationBot:OpenResto:AvailabilityBaseUrl", "https://availability.test");
        builder.UseSetting("ReservationBot:OpenResto:PrivateBaseUrl", "https://private.test");
        builder.UseSetting("ReservationBot:OpenResto:PrivateApiCredential", "test-openresto-private-api-credential");
        builder.UseSetting("ReservationBot:OpenResto:TimeoutSeconds", "15");

        builder.ConfigureServices(services =>
        {
            ServiceDescriptor? existingAvailabilityClient = services.FirstOrDefault(x => x.ServiceType == typeof(IOpenRestoAvailabilityClient));
            if (existingAvailabilityClient is not null)
            {
                services.Remove(existingAvailabilityClient);
            }

            ServiceDescriptor? existingPrivateClient = services.FirstOrDefault(x => x.ServiceType == typeof(IOpenRestoPrivateChannelClient));
            if (existingPrivateClient is not null)
            {
                services.Remove(existingPrivateClient);
            }

            services.AddSingleton<IOpenRestoAvailabilityClient>(AvailabilityClient);
            services.AddSingleton<IOpenRestoPrivateChannelClient>(PrivateChannelClient);
        });
    }

    public HttpClient CreateInternalClient(string assertion = "header.payload.signature", string? correlationId = "corr-bot-tests")
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", InternalCredential);
        client.DefaultRequestHeaders.TryAddWithoutValidation(BotRequestContextAccessor.AssertionHeaderName, assertion);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(BotRequestContextAccessor.CorrelationHeaderName, correlationId);
        }

        return client;
    }
}

public sealed class RecordingAvailabilityClient : IOpenRestoAvailabilityClient
{
    public int CallCount { get; private set; }
    public AvailabilityLookupRequest? LastRequest { get; private set; }
    public string? LastCorrelationId { get; private set; }

    public void Reset()
    {
        CallCount = 0;
        LastRequest = null;
        LastCorrelationId = null;
    }

    public Task<OpenRestoAvailabilityDto> LookupAsync(AvailabilityLookupRequest request, string correlationId, CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequest = request;
        LastCorrelationId = correlationId;

        return Task.FromResult(new OpenRestoAvailabilityDto
        {
            RestaurantId = request.RestaurantId,
            Date = request.Date,
            Slots = []
        });
    }
}

public sealed class RecordingPrivateChannelClient : IOpenRestoPrivateChannelClient
{
    public int ListCalls { get; private set; }

    public void Reset()
    {
        ListCalls = 0;
    }

    public Task<IReadOnlyList<OpenRestoReservationDto>> ListReservationsAsync(BotRequestContext context, CancellationToken cancellationToken)
    {
        ListCalls++;
        return Task.FromResult<IReadOnlyList<OpenRestoReservationDto>>([]);
    }

    public Task<OpenRestoReservationDto> GetReservationAsync(int reservationId, BotRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(new OpenRestoReservationDto { Id = reservationId });

    public Task<OpenRestoReservationDto> CreateReservationAsync(ReservationCreateRequest request, BotRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(new OpenRestoReservationDto { Id = 1, RestaurantId = request.RestaurantId });

    public Task<OpenRestoReservationDto> UpdateReservationAsync(ReservationUpdateRequest request, BotRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(new OpenRestoReservationDto { Id = request.ReservationId });

    public Task<OpenRestoReservationActionResultDto> CancelReservationAsync(ReservationCancelRequest request, BotRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(new OpenRestoReservationActionResultDto { Success = true, Message = "Cancelada." });

    public Task<IReadOnlyList<OpenRestoOccasionCatalogItemDto>> GetOccasionCatalogAsync(int restaurantId, BotRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<OpenRestoOccasionCatalogItemDto>>([]);

    public Task<OpenRestoHandoffResultDto> CreateHandoffAsync(ReservationHandoffRequest request, BotRequestContext context, CancellationToken cancellationToken)
        => Task.FromResult(new OpenRestoHandoffResultDto { AuditId = 1, RestaurantId = request.RestaurantId });
}
