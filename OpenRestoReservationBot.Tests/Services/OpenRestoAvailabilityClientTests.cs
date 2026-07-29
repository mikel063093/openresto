using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoReservationBot.Contracts;
using OpenRestoReservationBot.Services;

namespace OpenRestoReservationBot.Tests.Services;

public sealed class OpenRestoAvailabilityClientTests
{
    [Fact]
    public async Task Lookup_UsesFixedPath_AndForwardsCorrelationId()
    {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new OpenRestoAvailabilityDto
            {
                RestaurantId = 7,
                Date = new DateTime(2026, 8, 10, 18, 0, 0, DateTimeKind.Utc),
                Slots = []
            })
        });

        ServiceProvider services = CreateServices(handler, "OpenRestoAvailability", "https://availability.test/");
        var client = new OpenRestoAvailabilityClient(services.GetRequiredService<IHttpClientFactory>());

        await client.LookupAsync(
            new AvailabilityLookupRequest
            {
                RestaurantId = 7,
                Date = new DateTime(2026, 8, 10, 18, 0, 0, DateTimeKind.Utc),
                Seats = 5
            },
            "corr-availability",
            CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("/api/restaurants/7/availability", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("seats=5", handler.LastRequest.RequestUri.Query);
        Assert.Equal("corr-availability", handler.LastRequest.Headers.GetValues(BotRequestContextAccessor.CorrelationHeaderName).Single());
    }

    private static ServiceProvider CreateServices(HttpMessageHandler handler, string clientName, string baseUrl)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        }).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }
}
