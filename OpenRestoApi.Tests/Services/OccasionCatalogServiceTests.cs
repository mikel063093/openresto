using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Services;

public sealed class OccasionCatalogServiceTests
{
    [Fact]
    public async Task CreateSnapshotsAsync_PreservesCatalogValues_AfterCatalogEdit()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(CreateSnapshotsAsync_PreservesCatalogValues_AfterCatalogEdit));
        var restaurant = new Restaurant { Name = "Centro", Timezone = "UTC" };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        var booking = new Booking
        {
            RestaurantId = restaurant.Id,
            Restaurant = restaurant,
            BookingRef = "WHATS1",
            Seats = 2,
            Date = DateTime.UtcNow.AddDays(1)
        };
        db.Bookings.Add(booking);

        var item = new RestaurantOccasionCatalogItem
        {
            RestaurantId = restaurant.Id,
            Name = "Cumpleaños",
            Description = "Decoración sencilla",
            EstimatedPriceCop = 80000,
            SortOrder = 0,
            IsActive = true
        };
        db.RestaurantOccasionCatalogItems.Add(item);
        await db.SaveChangesAsync();

        var service = new OccasionCatalogService(db);

        IReadOnlyList<BookingOccasionSnapshot> snapshots = await service.CreateSnapshotsAsync(booking.Id, [item.Id]);

        item.Name = "Cumpleaños premium";
        item.EstimatedPriceCop = 120000;
        await db.SaveChangesAsync();

        BookingOccasionSnapshot persisted = await db.BookingOccasionSnapshots.SingleAsync();
        Assert.Single(snapshots);
        Assert.Equal("Cumpleaños", persisted.Name);
        Assert.Equal(80000, persisted.EstimatedPriceCop);
        Assert.Equal(item.Id, persisted.RestaurantOccasionCatalogItemId);
    }
}
