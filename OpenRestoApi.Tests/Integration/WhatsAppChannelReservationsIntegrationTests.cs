using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Integration;

public sealed class WhatsAppChannelReservationsIntegrationTests(TestWebAppFactory factory)
    : IClassFixture<TestWebAppFactory>
{
    private const string VerifiedPhoneHeader = "X-WhatsApp-Verified-Phone";

    private readonly TestWebAppFactory _factory = factory;

    [Fact]
    public async Task OwnerCanListAndGetOwnReservations_ButCrossPhoneAccessIsDenied()
    {
        int restaurantId = await SeedRestaurantAsync("Phone Ownership");
        Booking ownerBooking = await SeedBookingAsync(restaurantId, "+14155550100", "owner@example.com", "Owner");
        Booking foreignBooking = await SeedBookingAsync(restaurantId, "+14155550101", "other@example.com", "Other");

        HttpClient ownerClient = CreateWhatsAppClient("+14155550100");

        HttpResponseMessage listResponse = await ownerClient.GetAsync("/api/private/channels/whatsapp/reservations");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        List<BookingDto> listed = (await listResponse.Content.ReadFromJsonAsync<List<BookingDto>>())!;
        Assert.Contains(listed, x => x.Id == ownerBooking.Id);
        Assert.DoesNotContain(listed, x => x.Id == foreignBooking.Id);

        Assert.Equal(
            HttpStatusCode.OK,
            (await ownerClient.GetAsync($"/api/private/channels/whatsapp/reservations/{ownerBooking.Id}")).StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await ownerClient.GetAsync($"/api/private/channels/whatsapp/reservations/{foreignBooking.Id}")).StatusCode);
    }

    [Fact]
    public async Task UpdateRejectsImmutableFields_AndRequiresConfirmation()
    {
        int restaurantId = await SeedRestaurantAsync("Immutable Fields");
        Booking booking = await SeedBookingAsync(restaurantId, "+14155550110", "owner@example.com", "Owner");
        HttpClient client = CreateWhatsAppClient("+14155550110");

        HttpResponseMessage immutableResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.AddDays(1),
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey = "immutable-reject",
                customerName = "Changed Name",
                customerEmail = "changed@example.com",
                specialRequests = "Birthday",
                occasionCatalogItemIds = new[] { 1 }
            });

        Assert.Equal(HttpStatusCode.BadRequest, immutableResponse.StatusCode);

        HttpResponseMessage unconfirmedResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.AddHours(1),
                seats = booking.Seats + 1,
                confirmed = false,
                idempotencyKey = "missing-confirmation"
            });

        Assert.Equal(HttpStatusCode.BadRequest, unconfirmedResponse.StatusCode);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Booking unchanged = db.Bookings.Single(x => x.Id == booking.Id);
        Assert.Equal("Owner", unchanged.CustomerName);
        Assert.Equal("owner@example.com", unchanged.CustomerEmail);
        Assert.Equal(booking.Seats, unchanged.Seats);
        Assert.Equal(booking.Date, unchanged.Date);
    }

    [Fact]
    public async Task UpdateUsesIdempotency_AndRejectsAvailabilityConflicts()
    {
        int restaurantId = await SeedRestaurantAsync("Update Idempotency");
        Booking booking = await SeedBookingAsync(restaurantId, "+14155550120", "owner@example.com", "Owner");
        _ = await SeedBookingAsync(
            restaurantId,
            "+14155550121",
            "conflict@example.com",
            "Conflict",
            booking.Date.AddHours(2),
            tableId: booking.TableId,
            sectionId: booking.SectionId);

        HttpClient client = CreateWhatsAppClient("+14155550120");
        string idempotencyKey = "update-idempotency-key";
        DateTime newDate = booking.Date.AddHours(1);

        HttpResponseMessage firstResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = newDate,
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey
            });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        BookingDto firstResult = (await firstResponse.Content.ReadFromJsonAsync<BookingDto>())!;
        Assert.Equal(newDate, firstResult.Date);
        Assert.Equal(booking.Seats + 1, firstResult.Seats);

        HttpResponseMessage replayResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = newDate,
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey
            });
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);

        HttpResponseMessage changedFingerprintResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = newDate.AddHours(1),
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey
            });
        Assert.Equal(HttpStatusCode.Conflict, changedFingerprintResponse.StatusCode);

        HttpResponseMessage conflictResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.AddHours(2),
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey = "availability-conflict-key"
            });
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task CancelRequiresConfirmation_IsIdempotent_AndPreventsFurtherMutations()
    {
        int restaurantId = await SeedRestaurantAsync("Cancel Behavior");
        Booking booking = await SeedBookingAsync(restaurantId, "+14155550130", "owner@example.com", "Owner");
        HttpClient client = CreateWhatsAppClient("+14155550130");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(
                $"/api/private/channels/whatsapp/reservations/{booking.Id}/cancel",
                new { confirmed = false, idempotencyKey = "cancel-missing-confirmation" })).StatusCode);

        HttpResponseMessage firstCancelResponse = await client.PostAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}/cancel",
            new
            {
                confirmed = true,
                idempotencyKey = "cancel-key-1"
            });
        Assert.Equal(HttpStatusCode.OK, firstCancelResponse.StatusCode);

        OperatorReservationActionResultDto firstCancel =
            (await firstCancelResponse.Content.ReadFromJsonAsync<OperatorReservationActionResultDto>())!;
        Assert.True(firstCancel.Success);
        Assert.NotNull(firstCancel.Reservation);
        Assert.True(firstCancel.Reservation!.IsCancelled);

        HttpResponseMessage replayCancelResponse = await client.PostAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}/cancel",
            new
            {
                confirmed = true,
                idempotencyKey = "cancel-key-1"
            });
        Assert.Equal(HttpStatusCode.OK, replayCancelResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await client.PatchAsJsonAsync(
                $"/api/private/channels/whatsapp/reservations/{booking.Id}",
                new
                {
                    date = booking.Date.AddDays(1),
                    seats = booking.Seats + 1,
                    confirmed = true,
                    idempotencyKey = "update-after-cancel"
                })).StatusCode);
    }

    [Fact]
    public async Task OccasionCatalogReturnsOnlyActiveItems_ForVisibleNonArchivedRestaurant()
    {
        int visibleRestaurantId = await SeedRestaurantAsync("Catalog Visible");
        int archivedRestaurantId = await SeedRestaurantAsync("Catalog Archived", archived: true);

        await SeedCatalogItemAsync(visibleRestaurantId, "Birthday", true);
        await SeedCatalogItemAsync(visibleRestaurantId, "Inactive", false);
        await SeedCatalogItemAsync(archivedRestaurantId, "Archived Active", true);

        HttpClient client = CreateWhatsAppClient("+14155550140");

        HttpResponseMessage visibleResponse = await client.GetAsync(
            $"/api/private/channels/whatsapp/restaurants/{visibleRestaurantId}/occasion-catalog");
        Assert.Equal(HttpStatusCode.OK, visibleResponse.StatusCode);
        List<OccasionCatalogItemDto> visibleItems =
            (await visibleResponse.Content.ReadFromJsonAsync<List<OccasionCatalogItemDto>>())!;
        OccasionCatalogItemDto visibleItem = Assert.Single(visibleItems);
        Assert.Equal("Birthday", visibleItem.Name);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync(
                $"/api/private/channels/whatsapp/restaurants/{archivedRestaurantId}/occasion-catalog")).StatusCode);
    }

    private HttpClient CreateWhatsAppClient(string verifiedPhone)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestWebAppFactory.WhatsAppChannelToken);
        client.DefaultRequestHeaders.Add(VerifiedPhoneHeader, verifiedPhone);
        return client;
    }

    private async Task<int> SeedRestaurantAsync(string name, bool archived = false)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var restaurant = new Restaurant
        {
            Name = name,
            OpenTime = "11:00",
            CloseTime = "22:00",
            Timezone = "UTC",
            IsArchived = archived
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        db.Sections.Add(new Section { Name = $"{name} Main", RestaurantId = restaurant.Id });
        await db.SaveChangesAsync();
        int sectionId = db.Sections.Single(x => x.RestaurantId == restaurant.Id).Id;

        db.Tables.Add(new Table { Name = $"{name} T1", Seats = 4, SectionId = sectionId });
        db.Tables.Add(new Table { Name = $"{name} T2", Seats = 4, SectionId = sectionId });
        await db.SaveChangesAsync();

        return restaurant.Id;
    }

    private async Task<Booking> SeedBookingAsync(
        int restaurantId,
        string phone,
        string email,
        string name,
        DateTime? date = null,
        int? tableId = null,
        int? sectionId = null)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        int resolvedSectionId = sectionId ?? db.Sections.Where(x => x.RestaurantId == restaurantId).Select(x => x.Id).First();
        int resolvedTableId = tableId ?? db.Tables.Where(x => x.SectionId == resolvedSectionId).Select(x => x.Id).First();
        var booking = new Booking
        {
            RestaurantId = restaurantId,
            SectionId = resolvedSectionId,
            TableId = resolvedTableId,
            CustomerEmail = email,
            CustomerName = name,
            CustomerPhoneE164 = phone,
            CustomerPhoneNormalized = phone.TrimStart('+'),
            CreatedViaChannel = "whatsapp",
            Seats = 2,
            Date = date ?? DateTime.UtcNow.AddDays(7).Date.AddHours(12),
            EndTime = (date ?? DateTime.UtcNow.AddDays(7).Date.AddHours(12)).AddHours(1),
            BookingRef = $"WA-{Guid.NewGuid():N}".Substring(0, 12)
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private async Task SeedCatalogItemAsync(int restaurantId, string name, bool isActive)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.RestaurantOccasionCatalogItems.Add(new RestaurantOccasionCatalogItem
        {
            RestaurantId = restaurantId,
            Name = name,
            EstimatedPriceCop = 50000,
            IsActive = isActive,
            SortOrder = await db.RestaurantOccasionCatalogItems
                .Where(x => x.RestaurantId == restaurantId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
