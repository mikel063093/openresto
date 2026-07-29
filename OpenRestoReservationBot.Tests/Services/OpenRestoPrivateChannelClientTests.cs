using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoReservationBot.Contracts;
using OpenRestoReservationBot.Options;
using OpenRestoReservationBot.Services;

namespace OpenRestoReservationBot.Tests.Services;

public sealed class OpenRestoPrivateChannelClientTests
{
    private static readonly System.Text.Json.JsonSerializerOptions CamelCaseJson = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task ListReservations_ForwardsAssertionCorrelationAndCredential_UsingFixedPath()
    {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<OpenRestoReservationDto>())
        });

        OpenRestoPrivateChannelClient client = CreateClient(handler);
        BotRequestContext context = new("assertion.raw.value", "corr-private-list");

        await client.ListReservationsAsync(context, CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("/api/private/channels/whatsapp/reservations", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("test-openresto-private-api-credential", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal("assertion.raw.value", handler.LastRequest.Headers.GetValues(BotRequestContextAccessor.AssertionHeaderName).Single());
        Assert.Equal("corr-private-list", handler.LastRequest.Headers.GetValues(BotRequestContextAccessor.CorrelationHeaderName).Single());
    }

    [Fact]
    public async Task CreateReservation_UsesFixedEndpointAndTypedBody()
    {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new OpenRestoReservationDto
            {
                Id = 71,
                RestaurantId = 5,
                CustomerEmail = "ana@example.com",
                CustomerName = "Ana",
                Seats = 2
            })
        });

        OpenRestoPrivateChannelClient client = CreateClient(handler);

        await client.CreateReservationAsync(
            new ReservationCreateRequest
            {
                RestaurantId = 5,
                Date = new DateTime(2026, 8, 11, 19, 0, 0, DateTimeKind.Utc),
                Seats = 2,
                CustomerEmail = "ana@example.com",
                CustomerName = "Ana",
                Confirmed = true,
                IdempotencyKey = "create-bot-1",
                OccasionCatalogItemIds = [3, 4]
            },
            new BotRequestContext("assertion.forwarded", "corr-create"),
            CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/private/channels/whatsapp/reservations", handler.LastRequest.RequestUri!.AbsolutePath);

        OpenRestoWhatsAppCreateReservationRequestDto payload =
            System.Text.Json.JsonSerializer.Deserialize<OpenRestoWhatsAppCreateReservationRequestDto>(
                handler.LastRequestContent!,
                CamelCaseJson)!;

        Assert.Equal(5, payload.RestaurantId);
        Assert.Equal("ana@example.com", payload.CustomerEmail);
        Assert.Equal([3, 4], payload.OccasionCatalogItemIds);
    }

    [Fact]
    public async Task OccasionCatalog_UsesFixedRestaurantPath()
    {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<OpenRestoOccasionCatalogItemDto>())
        });

        OpenRestoPrivateChannelClient client = CreateClient(handler);

        await client.GetOccasionCatalogAsync(22, new BotRequestContext("assertion", "corr-catalog"), CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("/api/private/channels/whatsapp/restaurants/22/occasion-catalog", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    private static OpenRestoPrivateChannelClient CreateClient(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(OpenRestoPrivateChannelClient.ClientName, client =>
        {
            client.BaseAddress = new Uri("https://private.test/");
            client.Timeout = TimeSpan.FromSeconds(15);
        }).ConfigurePrimaryHttpMessageHandler(() => handler);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        return new OpenRestoPrivateChannelClient(
            serviceProvider.GetRequiredService<IHttpClientFactory>(),
            Microsoft.Extensions.Options.Options.Create(new ReservationBotOptions
            {
                InternalCredential = "unused-here",
                OpenResto = new OpenRestoOptions
                {
                    AvailabilityBaseUrl = "https://availability.test/",
                    PrivateBaseUrl = "https://private.test/",
                    PrivateApiCredential = "test-openresto-private-api-credential",
                    TimeoutSeconds = 15
                }
            }));
    }
}
