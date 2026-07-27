using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Integration;

public sealed class OperatorMcpIntegrationTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task AuthenticatedClient_CanDiscoverTools_AndRunReservationLifecycle()
    {
        (int restaurantId, int sectionId, int tableId) ids = await SeedRestaurantGraphAsync();
        string token = await SeedOperatorTokenAsync("mcp-owner@test.com", ids.restaurantId);

        await using McpClient mcp = await CreateAuthenticatedMcpClientAsync(token);
        var tools = await mcp.ListToolsAsync();

        string[] expectedTools =
        [
            "operator_get_availability",
            "operator_create_reservation",
            "operator_list_reservations",
            "operator_get_reservation",
            "operator_update_reservation",
            "operator_cancel_reservation",
            "operator_escalate_reservation"
        ];

        foreach (string tool in expectedTools)
        {
            Assert.Contains(tools, t => t.Name == tool);
        }

        DateTime bookingDate = DateTime.UtcNow.AddDays(5).Date.AddHours(12);
        var createResult = await mcp.CallToolAsync("operator_create_reservation", new Dictionary<string, object?>
        {
            ["request"] = new
            {
                restaurantId = ids.restaurantId,
                sectionId = ids.sectionId,
                tableId = ids.tableId,
                date = bookingDate,
                customerEmail = "guest@example.com",
                customerName = "Guest",
                seats = 2,
                specialRequests = "Window"
            }
        }!);

        Assert.False(createResult.IsError ?? false);
        int reservationId = createResult.StructuredContent!.Value.GetProperty("id").GetInt32();

        var updateResult = await mcp.CallToolAsync("operator_update_reservation", new Dictionary<string, object?>
        {
            ["reservationId"] = reservationId,
            ["request"] = new
            {
                sectionId = ids.sectionId,
                tableId = ids.tableId,
                date = bookingDate.AddHours(1),
                customerEmail = "guest@example.com",
                customerName = "Updated Guest",
                seats = 3,
                specialRequests = "Quiet"
            }
        }!);

        Assert.False(updateResult.IsError ?? false);
        Assert.Equal(3, updateResult.StructuredContent!.Value.GetProperty("seats").GetInt32());

        var listResult = await mcp.CallToolAsync("operator_list_reservations", new Dictionary<string, object?>());
        Assert.False(listResult.IsError ?? false);
        Assert.Equal(1, listResult.StructuredContent!.Value.GetArrayLength());

        var getResult = await mcp.CallToolAsync("operator_get_reservation", new Dictionary<string, object?>
        {
            ["reservationId"] = reservationId
        }!);
        Assert.False(getResult.IsError ?? false);
        Assert.Equal("Updated Guest", getResult.StructuredContent!.Value.GetProperty("customerName").GetString());

        var cancelResult = await mcp.CallToolAsync("operator_cancel_reservation", new Dictionary<string, object?>
        {
            ["reservationId"] = reservationId
        }!);
        Assert.False(cancelResult.IsError ?? false);
        Assert.True(cancelResult.StructuredContent!.Value.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task UnauthenticatedMcpRequests_AreRejected()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/mcp/operator", new
        {
            jsonrpc = "2.0",
            id = "1",
            method = "tools/list",
            @params = new { }
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RateLimit_IsPartitionedByCredential()
    {
        (int restaurantId, _, _) = await SeedRestaurantGraphAsync();
        string firstToken = await SeedOperatorTokenAsync("ratelimit-1@test.com", restaurantId);
        string secondToken = await SeedOperatorTokenAsync("ratelimit-2@test.com", restaurantId);

        HttpClient firstClient = _factory.CreateClient();
        firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstToken);
        HttpClient secondClient = _factory.CreateClient();
        secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondToken);

        bool threw = false;
        string date = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd");
        for (int i = 0; i < 40; i++)
        {
            HttpResponseMessage response = await firstClient.GetAsync(
                $"/api/internal/operators/restaurants/{restaurantId}/availability?date={date}&seats=2");

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                threw = true;
                break;
            }
        }

        Assert.True(threw);

        HttpResponseMessage secondResponse = await secondClient.GetAsync(
            $"/api/internal/operators/restaurants/{restaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    private async Task<McpClient> CreateAuthenticatedMcpClientAsync(string token)
    {
        HttpClient httpClient = _factory.CreateClient();
        HttpClientTransport transport = new(new HttpClientTransportOptions
        {
            Endpoint = new Uri(httpClient.BaseAddress!, "/api/mcp/operator"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}"
            }
        }, httpClient, NullLoggerFactory.Instance, ownsHttpClient: true);

        return await McpClient.CreateAsync(transport);
    }

    private async Task<(int restaurantId, int sectionId, int tableId)> SeedRestaurantGraphAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var restaurant = new Restaurant
        {
            Name = $"MCP Test {Guid.NewGuid():N}",
            OpenTime = "11:00",
            CloseTime = "13:00",
            Timezone = "UTC",
            Sections = new List<Section>
            {
                new()
                {
                    Name = "Main",
                    Tables = new List<Table> { new() { Name = "T1", Seats = 4 } }
                }
            }
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        Section section = restaurant.Sections.Single();
        Table table = section.Tables.Single();
        return (restaurant.Id, section.Id, table.Id);
    }

    private async Task<string> SeedOperatorTokenAsync(string email, int scopedRestaurantId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.OperatorPrincipals.Add(new OperatorPrincipal
        {
            Identifier = email,
            NormalizedIdentifier = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RestaurantScopes = new List<OperatorRestaurantScope>
            {
                new()
                {
                    RestaurantId = scopedRestaurantId,
                    CreatedAt = DateTime.UtcNow,
                }
            }
        });
        await db.SaveChangesAsync();

        var service = new OperatorCredentialService(db);
        int operatorId = db.OperatorPrincipals.Single(x => x.NormalizedIdentifier == email).Id;
        IssuedOperatorCredential issued = await service.IssueAsync(
            operatorId,
            OperatorCredentialExpirationPresetCatalog.TryResolve(OperatorCredentialExpirationPresetCatalog.EightHours, out var eightHours)
                ? eightHours
                : throw new InvalidOperationException());
        return issued.PlaintextToken;
    }
}
