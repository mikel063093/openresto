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
        string token = await SeedOperatorTokenAsync(scopedRestaurantId);

        HttpClient operatorClient = _factory.CreateClient();
        operatorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        string date = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        HttpResponseMessage okResponse = await operatorClient.GetAsync(
            $"/api/internal/operators/restaurants/{scopedRestaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        AvailabilityResponseDto? body = await okResponse.Content.ReadFromJsonAsync<AvailabilityResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(scopedRestaurantId, body!.RestaurantId);

        HttpResponseMessage outOfScopeResponse = await operatorClient.GetAsync(
            $"/api/internal/operators/restaurants/{otherRestaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.NotFound, outOfScopeResponse.StatusCode);

        HttpClient adminClient = _factory.CreateAuthenticatedClient();
        HttpResponseMessage adminResponse = await adminClient.GetAsync(
            $"/api/internal/operators/restaurants/{scopedRestaurantId}/availability?date={date}&seats=2");
        Assert.Equal(HttpStatusCode.Unauthorized, adminResponse.StatusCode);
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

    private async Task<string> SeedOperatorTokenAsync(int scopedRestaurantId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.OperatorPrincipals.Add(new OperatorPrincipal
        {
            Identifier = "operator@test.com",
            NormalizedIdentifier = "operator@test.com",
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
