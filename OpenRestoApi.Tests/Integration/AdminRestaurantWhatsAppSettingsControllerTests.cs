using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Integration;

public sealed class AdminRestaurantWhatsAppSettingsControllerTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task SuperAdmin_Can_Get_And_Update_WhatsAppSettings()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage getResponse = await client.GetAsync(
            $"/api/admin/restaurants/{restaurantId}/whatsapp-settings");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        RestaurantWhatsAppSettingsResponse initial = (await getResponse.Content.ReadFromJsonAsync<RestaurantWhatsAppSettingsResponse>())!;
        Assert.False(initial.IsWhatsAppTestEnabled);
        Assert.Null(initial.HandoffWhatsAppE164);

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/whatsapp-settings",
            new
            {
                isWhatsAppTestEnabled = true,
                handoffWhatsAppE164 = "300 111 22 33"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        RestaurantWhatsAppSettingsResponse updated = (await updateResponse.Content.ReadFromJsonAsync<RestaurantWhatsAppSettingsResponse>())!;
        Assert.True(updated.IsWhatsAppTestEnabled);
        Assert.Equal("+3001112233", updated.HandoffWhatsAppE164);
    }

    [Fact]
    public async Task BookingEditor_Cannot_Manage_WhatsAppSettings()
    {
        HttpClient client = _factory.CreateAuthenticatedClient(AdminRole.BookingEditor);
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/admin/restaurants/{restaurantId}/whatsapp-settings");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuperAdmin_Update_ReturnsBadRequest_WhenEnabledWithoutHandoffNumber()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        int restaurantId = await SeedRestaurantAsync();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/whatsapp-settings",
            new
            {
                isWhatsAppTestEnabled = true,
                handoffWhatsAppE164 = ""
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("handoff", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaleSuperAdminJwt_IsDenied_For_WhatsAppSettings_Management()
    {
        HttpClient client = _factory.CreateAuthenticatedClient();
        int restaurantId = await SeedRestaurantAsync();

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        AdminCredential admin = db.AdminCredentials.Single(x => x.Email == TestWebAppFactory.AdminEmail);
        admin.IsActive = false;
        await db.SaveChangesAsync();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/admin/restaurants/{restaurantId}/whatsapp-settings");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> SeedRestaurantAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var restaurant = new Restaurant
        {
            Name = $"WhatsApp {Guid.NewGuid():N}",
            OpenTime = "11:00",
            CloseTime = "22:00",
            Timezone = "UTC",
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();
        return restaurant.Id;
    }

    private sealed class RestaurantWhatsAppSettingsResponse
    {
        public bool IsWhatsAppTestEnabled { get; set; }
        public string? HandoffWhatsAppE164 { get; set; }
    }
}
