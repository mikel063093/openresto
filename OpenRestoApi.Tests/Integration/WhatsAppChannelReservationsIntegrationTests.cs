using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Tests.Integration;

public sealed class WhatsAppChannelReservationsIntegrationTests(TestWebAppFactory factory)
    : IClassFixture<TestWebAppFactory>
{
    private const string AssertionHeader = "X-OpenResto-Channel-Assertion";

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
    public async Task RejectsMissingForgedExpiredWrongIssuerAudienceWrongActionAssertions_AndReplay()
    {
        int restaurantId = await SeedRestaurantAsync("Assertion Security");
        Booking booking = await SeedBookingAsync(restaurantId, "+14155550105", "owner@example.com", "Owner");

        HttpClient missingAssertionClient = CreateInternalCallerOnlyClient();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await missingAssertionClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);

        HttpClient forgedClient = CreateWhatsAppClient(
            "+14155550105",
            assertion: TestWebAppFactory.GenerateWhatsAppAssertion(
                "+14155550105",
                signingKey: "forged-whatsapp-assertion-signing-key-minimum-32-chars!!"));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await forgedClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);

        HttpClient expiredClient = CreateWhatsAppClient(
            "+14155550105",
            assertion: TestWebAppFactory.GenerateWhatsAppAssertion(
                "+14155550105",
                expiresAtUtc: DateTime.UtcNow.AddMinutes(-1)));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await expiredClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);

        HttpClient wrongIssuerClient = CreateWhatsAppClient(
            "+14155550105",
            assertion: TestWebAppFactory.GenerateWhatsAppAssertion("+14155550105", issuer: "wrong-issuer"));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await wrongIssuerClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);

        HttpClient wrongAudienceClient = CreateWhatsAppClient(
            "+14155550105",
            assertion: TestWebAppFactory.GenerateWhatsAppAssertion("+14155550105", audience: "wrong-audience"));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await wrongAudienceClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);

        HttpClient wrongActionClient = CreateWhatsAppClient("+14155550105", action: "reservations.mutate");
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await wrongActionClient.GetAsync($"/api/private/channels/whatsapp/reservations/{booking.Id}")).StatusCode);

        string replayedAssertion = TestWebAppFactory.GenerateWhatsAppAssertion(
            "+14155550105",
            jwtId: Guid.NewGuid().ToString("N"));
        HttpClient replayClient = CreateWhatsAppClient("+14155550105", assertion: replayedAssertion);

        Assert.Equal(
            HttpStatusCode.OK,
            (await replayClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await replayClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);
    }

    [Fact]
    public async Task UpdateRejectsImmutableFields_RequiresConfirmation_AndRejectsStaleWriter()
    {
        int restaurantId = await SeedRestaurantAsync("Immutable Fields");
        Booking booking = await SeedBookingAsync(restaurantId, "+14155550110", "owner@example.com", "Owner");
        HttpClient client = CreateWhatsAppClient("+14155550110", action: "reservations.mutate");

        HttpResponseMessage immutableResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.AddDays(1),
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey = "immutable-reject",
                expectedConcurrencyToken = booking.ConcurrencyToken,
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
                idempotencyKey = "missing-confirmation",
                expectedConcurrencyToken = booking.ConcurrencyToken
            });

        Assert.Equal(HttpStatusCode.BadRequest, unconfirmedResponse.StatusCode);

        HttpResponseMessage firstUpdateResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.AddHours(1),
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey = "fresh-writer",
                expectedConcurrencyToken = booking.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.OK, firstUpdateResponse.StatusCode);

        HttpResponseMessage staleWriterResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.AddHours(2),
                seats = booking.Seats + 2,
                confirmed = true,
                idempotencyKey = "stale-writer",
                expectedConcurrencyToken = booking.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.Conflict, staleWriterResponse.StatusCode);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Booking unchanged = db.Bookings.Single(x => x.Id == booking.Id);
        Assert.Equal("Owner", unchanged.CustomerName);
        Assert.Equal("owner@example.com", unchanged.CustomerEmail);
        Assert.Equal(booking.Seats + 1, unchanged.Seats);
        Assert.Equal(booking.Date.AddHours(1), unchanged.Date);
    }

    [Fact]
    public async Task UpdateUsesIdempotency_ReplaysStoredResult_RejectsFingerprintReuse_AndBusinessFailuresDoNotConsumeKey()
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

        HttpClient client = CreateWhatsAppClient("+14155550120", action: "reservations.mutate");
        string idempotencyKey = "update-idempotency-key";
        DateTime newDate = booking.Date.AddHours(1);

        HttpResponseMessage firstResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = newDate,
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey,
                expectedConcurrencyToken = booking.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        BookingDto firstResult = (await firstResponse.Content.ReadFromJsonAsync<BookingDto>())!;
        Assert.Equal(newDate, firstResult.Date);
        Assert.Equal(booking.Seats + 1, firstResult.Seats);

        await MutateBookingDirectlyAsync(booking.Id, booking.Date.AddHours(5), booking.Seats + 3);

        HttpResponseMessage replayResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = newDate,
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey,
                expectedConcurrencyToken = booking.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        BookingDto replayed = (await replayResponse.Content.ReadFromJsonAsync<BookingDto>())!;
        Assert.Equal(firstResult.Date, replayed.Date);
        Assert.Equal(firstResult.Seats, replayed.Seats);

        HttpResponseMessage changedFingerprintResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = newDate.AddHours(1),
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey,
                expectedConcurrencyToken = booking.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.Conflict, changedFingerprintResponse.StatusCode);

        string reusableKey = "validation-before-idempotency";
        HttpResponseMessage invalidSeatsResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = newDate.AddHours(1),
                seats = 0,
                confirmed = true,
                idempotencyKey = reusableKey,
                expectedConcurrencyToken = replayed.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidSeatsResponse.StatusCode);

        HttpResponseMessage validRetryAfterInvalidSeats = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = newDate.AddHours(2),
                seats = booking.Seats + 2,
                confirmed = true,
                idempotencyKey = reusableKey,
                expectedConcurrencyToken = replayed.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.OK, validRetryAfterInvalidSeats.StatusCode);

        BookingDto successfulRetry = (await validRetryAfterInvalidSeats.Content.ReadFromJsonAsync<BookingDto>())!;

        string closedHoursKey = "closed-hours-before-idempotency";
        HttpResponseMessage conflictResponse = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.Date.AddHours(3),
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey = closedHoursKey,
                expectedConcurrencyToken = successfulRetry.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.BadRequest, conflictResponse.StatusCode);

        HttpResponseMessage validRetryAfterClosedHours = await client.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.AddHours(4),
                seats = booking.Seats + 1,
                confirmed = true,
                idempotencyKey = closedHoursKey,
                expectedConcurrencyToken = successfulRetry.ConcurrencyToken
            });
        Assert.Equal(HttpStatusCode.OK, validRetryAfterClosedHours.StatusCode);
    }

    [Fact]
    public async Task CancelRequiresConfirmation_IsIdempotent_PreventsFurtherMutations_AndRejectsStaleWriter()
    {
        int restaurantId = await SeedRestaurantAsync("Cancel Behavior");
        Booking booking = await SeedBookingAsync(restaurantId, "+14155550130", "owner@example.com", "Owner");
        HttpClient client = CreateWhatsAppClient("+14155550130", action: "reservations.mutate");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync(
                $"/api/private/channels/whatsapp/reservations/{booking.Id}/cancel",
                new
                {
                    confirmed = false,
                    idempotencyKey = "cancel-missing-confirmation",
                    expectedConcurrencyToken = booking.ConcurrencyToken
                })).StatusCode);

        HttpResponseMessage staleWriterResponse = await client.PostAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}/cancel",
            new
            {
                confirmed = true,
                idempotencyKey = "cancel-stale-writer",
                expectedConcurrencyToken = booking.ConcurrencyToken + 1
            });
        Assert.Equal(HttpStatusCode.Conflict, staleWriterResponse.StatusCode);

        HttpResponseMessage firstCancelResponse = await client.PostAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}/cancel",
            new
            {
                confirmed = true,
                idempotencyKey = "cancel-key-1",
                expectedConcurrencyToken = booking.ConcurrencyToken
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
                idempotencyKey = "cancel-key-1",
                expectedConcurrencyToken = booking.ConcurrencyToken
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
                    idempotencyKey = "update-after-cancel",
                    expectedConcurrencyToken = firstCancel.Reservation!.ConcurrencyToken
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

    private HttpClient CreateInternalCallerOnlyClient()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestWebAppFactory.WhatsAppChannelInternalCallerCredential);
        return client;
    }

    private HttpClient CreateWhatsAppClient(string verifiedPhone, string action = "reservations.read", string? assertion = null)
    {
        return _factory.CreateDefaultClient(new FreshWhatsAppAssertionHandler(verifiedPhone, action, assertion));
    }

    private async Task MutateBookingDirectlyAsync(int bookingId, DateTime newDate, int newSeats)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Booking booking = db.Bookings.Single(x => x.Id == bookingId);
        booking.Date = newDate;
        booking.Seats = newSeats;
        await db.SaveChangesAsync();
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

    private sealed class FreshWhatsAppAssertionHandler(string verifiedPhone, string action, string? fixedAssertion)
        : DelegatingHandler
    {
        private readonly string _verifiedPhone = verifiedPhone;
        private readonly string _action = action;
        private readonly string? _fixedAssertion = fixedAssertion;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                TestWebAppFactory.WhatsAppChannelInternalCallerCredential);
            request.Headers.Remove(AssertionHeader);
            request.Headers.Add(
                AssertionHeader,
                _fixedAssertion ?? TestWebAppFactory.GenerateWhatsAppAssertion(_verifiedPhone, action: _action));
            return base.SendAsync(request, cancellationToken);
        }
    }
}
