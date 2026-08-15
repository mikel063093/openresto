using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Services;

public sealed class RestaurantWhatsAppSettingsServiceTests
{
    [Fact]
    public async Task GetAsync_Returns_CurrentPersistedSettings()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(GetAsync_Returns_CurrentPersistedSettings));
        db.Restaurants.Add(new Restaurant
        {
            Name = "Centro",
            Timezone = "UTC",
            IsWhatsAppTestEnabled = true,
            HandoffWhatsAppE164 = "+573001112233"
        });
        await db.SaveChangesAsync();

        var service = new RestaurantWhatsAppSettingsService(db);

        RestaurantWhatsAppSettingsDto result = await service.GetAsync(1);

        Assert.True(result.IsWhatsAppTestEnabled);
        Assert.Equal("+573001112233", result.HandoffWhatsAppE164);
    }

    [Fact]
    public async Task UpdateAsync_RequiresHandoffNumber_WhenWhatsAppTestIsEnabled()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(UpdateAsync_RequiresHandoffNumber_WhenWhatsAppTestIsEnabled));
        db.Restaurants.Add(new Restaurant { Name = "Centro", Timezone = "UTC" });
        await db.SaveChangesAsync();

        var service = new RestaurantWhatsAppSettingsService(db);

        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateAsync(1, new UpdateRestaurantWhatsAppSettingsRequestDto
            {
                IsWhatsAppTestEnabled = true,
                HandoffWhatsAppE164 = " "
            }));

        Assert.Contains("handoff", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_NormalizesConfiguredHandoffNumber()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(UpdateAsync_NormalizesConfiguredHandoffNumber));
        db.Restaurants.Add(new Restaurant { Name = "Centro", Timezone = "UTC" });
        await db.SaveChangesAsync();

        var service = new RestaurantWhatsAppSettingsService(db);

        RestaurantWhatsAppSettingsDto result = await service.UpdateAsync(1, new UpdateRestaurantWhatsAppSettingsRequestDto
        {
            IsWhatsAppTestEnabled = true,
            HandoffWhatsAppE164 = " 300 111 22 33 "
        });

        Assert.True(result.IsWhatsAppTestEnabled);
        Assert.Equal("+3001112233", result.HandoffWhatsAppE164);
        Assert.Equal("+3001112233", await db.Restaurants.Where(x => x.Id == 1).Select(x => x.HandoffWhatsAppE164).SingleAsync());
    }
}
