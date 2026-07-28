using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Services;

public sealed class OccasionCatalogServiceTests
{
    [Fact]
    public async Task CreateSnapshotsAsync_RejectsDuplicateCatalogItemIds_InSingleRequest()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(CreateSnapshotsAsync_RejectsDuplicateCatalogItemIds_InSingleRequest));
        var restaurant = new Restaurant { Name = "Centro", Timezone = "UTC" };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        var booking = new Booking
        {
            RestaurantId = restaurant.Id,
            Restaurant = restaurant,
            BookingRef = "WHATS-DUP",
            Seats = 2,
            Date = DateTime.UtcNow.AddDays(1)
        };
        db.Bookings.Add(booking);

        var item = new RestaurantOccasionCatalogItem
        {
            RestaurantId = restaurant.Id,
            Name = "Cumpleaños",
            EstimatedPriceCop = 80000,
            SortOrder = 0,
            IsActive = true
        };
        db.RestaurantOccasionCatalogItems.Add(item);
        await db.SaveChangesAsync();

        var service = new OccasionCatalogService(db);

        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateSnapshotsAsync(booking.Id, [item.Id, item.Id]));

        Assert.Contains("duplic", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(db.BookingOccasionSnapshots);
    }

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

    [Fact]
    public async Task CreateSnapshotsAsync_ReplaysExistingMatchingSnapshots_WithoutCreatingDuplicates()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(CreateSnapshotsAsync_ReplaysExistingMatchingSnapshots_WithoutCreatingDuplicates));
        var restaurant = new Restaurant { Name = "Centro", Timezone = "UTC" };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        var booking = new Booking
        {
            RestaurantId = restaurant.Id,
            Restaurant = restaurant,
            BookingRef = "WHATS-REPLAY",
            Seats = 2,
            Date = DateTime.UtcNow.AddDays(1)
        };
        db.Bookings.Add(booking);

        var item = new RestaurantOccasionCatalogItem
        {
            RestaurantId = restaurant.Id,
            Name = "Aniversario",
            Description = "Flores",
            EstimatedPriceCop = 120000,
            SortOrder = 0,
            IsActive = true
        };
        db.RestaurantOccasionCatalogItems.Add(item);
        await db.SaveChangesAsync();

        var service = new OccasionCatalogService(db);

        IReadOnlyList<BookingOccasionSnapshot> first = await service.CreateSnapshotsAsync(booking.Id, [item.Id]);
        IReadOnlyList<BookingOccasionSnapshot> replay = await service.CreateSnapshotsAsync(booking.Id, [item.Id]);

        Assert.Single(first);
        Assert.Single(replay);
        Assert.Equal(first[0].Id, replay[0].Id);
        Assert.Equal(1, await db.BookingOccasionSnapshots.CountAsync());
    }

    [Fact]
    public async Task CreateSnapshotsAsync_RejectsMismatchedReplay_WhenBookingAlreadyHasDifferentSnapshotSet()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(CreateSnapshotsAsync_RejectsMismatchedReplay_WhenBookingAlreadyHasDifferentSnapshotSet));
        var restaurant = new Restaurant { Name = "Centro", Timezone = "UTC" };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        var booking = new Booking
        {
            RestaurantId = restaurant.Id,
            Restaurant = restaurant,
            BookingRef = "WHATS-CONFLICT",
            Seats = 2,
            Date = DateTime.UtcNow.AddDays(1)
        };
        db.Bookings.Add(booking);

        var itemOne = new RestaurantOccasionCatalogItem
        {
            RestaurantId = restaurant.Id,
            Name = "Aniversario",
            EstimatedPriceCop = 90000,
            SortOrder = 0,
            IsActive = true
        };
        var itemTwo = new RestaurantOccasionCatalogItem
        {
            RestaurantId = restaurant.Id,
            Name = "Cumpleaños",
            EstimatedPriceCop = 110000,
            SortOrder = 1,
            IsActive = true
        };
        db.RestaurantOccasionCatalogItems.AddRange(itemOne, itemTwo);
        await db.SaveChangesAsync();

        var service = new OccasionCatalogService(db);
        await service.CreateSnapshotsAsync(booking.Id, [itemOne.Id]);

        ConflictException ex = await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateSnapshotsAsync(booking.Id, [itemTwo.Id]));

        Assert.Contains("snapshot", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.BookingOccasionSnapshots.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsValuesBeyondCatalogBounds()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(CreateAsync_RejectsValuesBeyondCatalogBounds));
        db.Restaurants.Add(new Restaurant { Name = "Centro", Timezone = "UTC" });
        await db.SaveChangesAsync();

        var service = new OccasionCatalogService(db);
        UpsertOccasionCatalogItemRequest request = new()
        {
            Name = new string('N', 121),
            Description = new string('D', 501),
            EstimatedPriceCop = 50000001,
            IsActive = true
        };

        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(1, request));

        Assert.Contains("nombre", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
