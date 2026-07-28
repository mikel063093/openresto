using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Integration;

public sealed class AdminOccasionCatalogControllerTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task SuperAdmin_Can_Create_List_Update_And_Delete_CatalogItems()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/occasion-catalog",
            new
            {
                name = "Aniversario",
                description = "Mesa decorada",
                estimatedPriceCop = 95000,
                isActive = true
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        CatalogItemResponse created = (await createResponse.Content.ReadFromJsonAsync<CatalogItemResponse>())!;
        Assert.Equal("Aniversario", created.Name);
        Assert.Equal(95000, created.EstimatedPriceCop);

        HttpResponseMessage listResponse = await client.GetAsync(
            $"/api/admin/restaurants/{restaurantId}/occasion-catalog");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        List<CatalogItemResponse> listed = (await listResponse.Content.ReadFromJsonAsync<List<CatalogItemResponse>>())!;
        CatalogItemResponse listedItem = Assert.Single(listed);
        Assert.Equal(created.Id, listedItem.Id);

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/occasion-catalog/{created.Id}",
            new
            {
                name = "Aniversario premium",
                description = "Mesa decorada con flores",
                estimatedPriceCop = 120000,
                isActive = false
            });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/admin/restaurants/{restaurantId}/occasion-catalog/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(db.RestaurantOccasionCatalogItems);
    }

    [Fact]
    public async Task BookingEditor_Cannot_Manage_OccasionCatalog()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(AdminRole.BookingEditor);
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/admin/restaurants/{restaurantId}/occasion-catalog");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_Create_ReturnsBadRequest_ForOutOfBoundsCatalogValues()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/occasion-catalog",
            new
            {
                name = new string('N', 121),
                description = new string('D', 501),
                estimatedPriceCop = 50000001,
                isActive = true
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_Update_ReturnsBadRequest_ForOutOfBoundsCatalogValues()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/occasion-catalog",
            new
            {
                name = "Aniversario",
                description = "Mesa decorada",
                estimatedPriceCop = 95000,
                isActive = true
            });
        CatalogItemResponse created = (await createResponse.Content.ReadFromJsonAsync<CatalogItemResponse>())!;

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/occasion-catalog/{created.Id}",
            new
            {
                name = new string('N', 121),
                description = new string('D', 501),
                estimatedPriceCop = 50000001,
                isActive = true
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> SeedRestaurantAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var restaurant = new Restaurant
        {
            Name = $"Catálogo {Guid.NewGuid():N}",
            OpenTime = "11:00",
            CloseTime = "22:00",
            Timezone = "UTC",
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();
        return restaurant.Id;
    }

    private sealed class CatalogItemResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int EstimatedPriceCop { get; set; }
    }
}
