using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Integration;

public sealed class OperatorAvailabilityIntegrationTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task ScopedOperatorCredential_CanReadScopedAvailability_AndAdminJwtCannot()
    {
        int scopedRestaurantId = await SeedRestaurantAsync("Scoped");
        int otherRestaurantId = await SeedRestaurantAsync("Other");
        string token = await SeedOperatorTokenAsync(scopedRestaurantId, "scoped-read");

        HttpClient operatorClient = _factory.CreateClient();
        operatorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string date = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        HttpResponseMessage okResponse = await operatorClient.GetAsync(
            $"/api/internal/operators/restaurants/{scopedRestaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        AvailabilityResponseDto? body = await okResponse.Content.ReadFromJsonAsync<AvailabilityResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(scopedRestaurantId, body!.RestaurantId);

        using (IServiceScope auditScope = _factory.Services.CreateScope())
        {
            AppDbContext auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Contains(auditDb.OperatorActionAudits.ToList(), x =>
                x.RestaurantId == scopedRestaurantId &&
                x.Action == "availability.read" &&
                x.Outcome == "success" &&
                x.Reason == null);
        }

        HttpResponseMessage outOfScopeResponse = await operatorClient.GetAsync(
            $"/api/internal/operators/restaurants/{otherRestaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.NotFound, outOfScopeResponse.StatusCode);

        HttpClient adminClient = _factory.CreateAuthenticatedClient();
        HttpResponseMessage adminResponse = await adminClient.GetAsync(
            $"/api/internal/operators/restaurants/{scopedRestaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.Unauthorized, adminResponse.StatusCode);
    }

    [Fact]
    public async Task ScopedOperatorCredential_RequestingNonexistentRestaurant_ReturnsNotFound_AndAuditsWithoutInvalidRestaurantFk()
    {
        int scopedRestaurantId = await SeedRestaurantAsync("Scoped");
        string token = await SeedOperatorTokenAsync(scopedRestaurantId, "nonexistent");

        HttpClient operatorClient = _factory.CreateClient();
        operatorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string date = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
        int nonexistentRestaurantId = scopedRestaurantId + 10_000;

        HttpResponseMessage response = await operatorClient.GetAsync(
            $"/api/internal/operators/restaurants/{nonexistentRestaurantId}/availability?date={date}&seats=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using IServiceScope auditScope = _factory.Services.CreateScope();
        AppDbContext auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        OperatorActionAudit audit = Assert.Single(auditDb.OperatorActionAudits.Where(x =>
            x.Action == "availability.read" &&
            x.Outcome == "denied" &&
            x.Reason == "restaurant_out_of_scope" &&
            x.RestaurantIdSnapshot == nonexistentRestaurantId));

        Assert.Null(audit.RestaurantId);
        Assert.Equal(string.Empty, audit.RestaurantNameSnapshot);
    }

    [Fact]
    public async Task ScopedOperatorCredential_RequestingExistingOutOfScopeRestaurant_ReturnsNotFound_AndAuditsWithValidRestaurantFk()
    {
        int scopedRestaurantId = await SeedRestaurantAsync("Scoped");
        int otherRestaurantId = await SeedRestaurantAsync("Other");
        string token = await SeedOperatorTokenAsync(scopedRestaurantId, "out-of-scope");

        HttpClient operatorClient = _factory.CreateClient();
        operatorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string date = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        HttpResponseMessage response = await operatorClient.GetAsync(
            $"/api/internal/operators/restaurants/{otherRestaurantId}/availability?date={date}&seats=2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using IServiceScope auditScope = _factory.Services.CreateScope();
        AppDbContext auditDb = auditScope.ServiceProvider.GetRequiredService<AppDbContext>();
        OperatorActionAudit audit = Assert.Single(auditDb.OperatorActionAudits.Where(x =>
            x.Action == "availability.read" &&
            x.Outcome == "denied" &&
            x.Reason == "restaurant_out_of_scope" &&
            x.RestaurantIdSnapshot == otherRestaurantId));

        Assert.Equal(otherRestaurantId, audit.RestaurantId);
        Assert.Equal("Operator Other", audit.RestaurantNameSnapshot);
    }

    private async Task<int> SeedRestaurantAsync(string suffix)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var restaurant = new Restaurant
        {
            Name = $"Operator {suffix}",
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
        return restaurant.Id;
    }

    private async Task<string> SeedOperatorTokenAsync(int scopedRestaurantId, string uniqueSuffix)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        string identifier = $"operator-{uniqueSuffix}@test.com";
        db.OperatorPrincipals.Add(new OperatorPrincipal
        {
            Identifier = identifier,
            NormalizedIdentifier = identifier,
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
        IssuedOperatorCredential issued = await service.IssueAsync(db.OperatorPrincipals.OrderByDescending(x => x.Id).First().Id, TimeSpan.FromHours(1));
        return issued.PlaintextToken;
    }
}
