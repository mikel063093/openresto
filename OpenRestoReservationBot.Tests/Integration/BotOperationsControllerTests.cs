using System.Net;
using System.Net.Http.Json;
using OpenRestoReservationBot.Contracts;
using OpenRestoReservationBot.Services;

namespace OpenRestoReservationBot.Tests.Integration;

public sealed class BotOperationsControllerTests(BotTestWebAppFactory factory) : IClassFixture<BotTestWebAppFactory>
{
    private readonly BotTestWebAppFactory _factory = factory;

    [Fact]
    public async Task MissingCredential_IsRejected()
    {
        _factory.AvailabilityClient.Reset();
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/internal/reservation-bot/operations",
            new
            {
                operation = "availability",
                availability = new
                {
                    restaurantId = 3,
                    date = DateTime.UtcNow.AddDays(1),
                    seats = 2
                }
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _factory.AvailabilityClient.CallCount);
    }

    [Fact]
    public async Task InvalidCredential_IsRejected()
    {
        _factory.AvailabilityClient.Reset();
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid");
        client.DefaultRequestHeaders.TryAddWithoutValidation(BotRequestContextAccessor.AssertionHeaderName, "header.payload.signature");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/internal/reservation-bot/operations",
            new
            {
                operation = "availability",
                availability = new
                {
                    restaurantId = 3,
                    date = DateTime.UtcNow.AddDays(1),
                    seats = 2
                }
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _factory.AvailabilityClient.CallCount);
    }

    [Fact]
    public async Task ValidCredential_AllowsClosedAvailabilityContract()
    {
        _factory.AvailabilityClient.Reset();
        using HttpClient client = _factory.CreateInternalClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/internal/reservation-bot/operations",
            new
            {
                operation = "availability",
                availability = new
                {
                    restaurantId = 9,
                    date = new DateTime(2026, 8, 2, 19, 0, 0, DateTimeKind.Utc),
                    seats = 4
                }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, _factory.AvailabilityClient.CallCount);
        Assert.Equal(9, _factory.AvailabilityClient.LastRequest!.RestaurantId);
    }

    [Fact]
    public async Task MissingAssertion_IsRejected()
    {
        _factory.PrivateChannelClient.Reset();
        using HttpClient client = _factory.CreateInternalClient(correlationId: "corr-no-assertion");
        client.DefaultRequestHeaders.Remove(BotRequestContextAccessor.AssertionHeaderName);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/internal/reservation-bot/operations",
            new
            {
                operation = "list",
                list = new { }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, _factory.PrivateChannelClient.ListCalls);
    }

    [Fact]
    public async Task UnknownOperation_IsRejected()
    {
        using HttpClient client = _factory.CreateInternalClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/internal/reservation-bot/operations",
            new
            {
                operation = "proxy.call",
                list = new { }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Microsoft.AspNetCore.Mvc.ValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();
        Assert.NotNull(problem);
    }

    [Fact]
    public async Task UnexpectedRootField_IsRejected()
    {
        using HttpClient client = _factory.CreateInternalClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/internal/reservation-bot/operations",
            new
            {
                operation = "availability",
                availability = new
                {
                    restaurantId = 3,
                    date = DateTime.UtcNow.AddDays(1),
                    seats = 2
                },
                targetUrl = "https://evil.test/private"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnexpectedNestedField_IsRejected()
    {
        using HttpClient client = _factory.CreateInternalClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/internal/reservation-bot/operations",
            new
            {
                operation = "create",
                create = new
                {
                    restaurantId = 4,
                    date = DateTime.UtcNow.AddDays(1),
                    seats = 2,
                    customerEmail = "ana@example.com",
                    customerName = "Ana",
                    confirmed = true,
                    idempotencyKey = "wa-create-1",
                    headers = new
                    {
                        authorization = "Bearer injected"
                    }
                }
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
