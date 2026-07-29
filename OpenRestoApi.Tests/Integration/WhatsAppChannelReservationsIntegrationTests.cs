using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
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
        int restaurantId = await SeedRestaurantAsync("Phone Ownership", whatsappEnabled: true);
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

        HttpClient previousKeyClient = CreateWhatsAppClient(
            "+14155550105",
            assertion: TestWebAppFactory.GenerateWhatsAppAssertion(
                "+14155550105",
                kid: TestWebAppFactory.WhatsAppChannelAssertionPreviousKid,
                signingKey: TestWebAppFactory.WhatsAppChannelAssertionPreviousSigningKey));
        Assert.Equal(
            HttpStatusCode.OK,
            (await previousKeyClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);

        HttpClient wrongKidClient = CreateWhatsAppClient(
            "+14155550105",
            assertion: TestWebAppFactory.GenerateWhatsAppAssertion(
                "+14155550105",
                kid: "kid-unknown"));
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await wrongKidClient.GetAsync("/api/private/channels/whatsapp/reservations")).StatusCode);
    }

    [Fact]
    public async Task ReplayProtection_PersistsThroughValidatedJwtExpiry_AndExpiredMarkersAreCleanedUp()
    {
        int restaurantId = await SeedRestaurantAsync("Assertion Lifetime", whatsappEnabled: true);
        _ = await SeedBookingAsync(restaurantId, "+14155550106", "owner@example.com", "Owner");

        string jwtId = Guid.NewGuid().ToString("N");
        DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(9);
        string longLivedAssertion = TestWebAppFactory.GenerateWhatsAppAssertion(
            "+14155550106",
            expiresAtUtc: expiresAtUtc,
            jwtId: jwtId);

        HttpClient client = CreateWhatsAppClient("+14155550106", assertion: longLivedAssertion);

        HttpResponseMessage firstResponse = await client.GetAsync("/api/private/channels/whatsapp/reservations");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ChannelMutationIdempotencyRecord marker = db.ChannelMutationIdempotencyRecords
                .Single(x => x.Channel == "whatsapp_assertion" && x.ReplayKey == jwtId);
            Assert.NotNull(marker.ExpiresAtUtc);
            Assert.True(marker.ExpiresAtUtc.Value > DateTime.UtcNow.AddMinutes(5));
            Assert.True(marker.ExpiresAtUtc.Value <= expiresAtUtc);
        }

        HttpResponseMessage replayResponse = await client.GetAsync("/api/private/channels/whatsapp/reservations");
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);

        using (IServiceScope expireScope = _factory.Services.CreateScope())
        {
            AppDbContext db = expireScope.ServiceProvider.GetRequiredService<AppDbContext>();
            ChannelMutationIdempotencyRecord marker = db.ChannelMutationIdempotencyRecords
                .Single(x => x.Channel == "whatsapp_assertion" && x.ReplayKey == jwtId);
            marker.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        string refreshedAssertion = TestWebAppFactory.GenerateWhatsAppAssertion(
            "+14155550106",
            expiresAtUtc: DateTime.UtcNow.AddMinutes(4),
            jwtId: jwtId);
        HttpClient refreshedClient = CreateWhatsAppClient("+14155550106", assertion: refreshedAssertion);

        HttpResponseMessage refreshedResponse = await refreshedClient.GetAsync("/api/private/channels/whatsapp/reservations");
        Assert.Equal(HttpStatusCode.OK, refreshedResponse.StatusCode);

        using IServiceScope cleanupScope = _factory.Services.CreateScope();
        AppDbContext cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        ChannelMutationIdempotencyRecord renewedMarker = cleanupDb.ChannelMutationIdempotencyRecords
            .Single(x => x.Channel == "whatsapp_assertion" && x.ReplayKey == jwtId);
        Assert.True(renewedMarker.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateRequiresEmailAndConfirmation_AndStampsTrustedOwnershipAndSnapshots()
    {
        int restaurantId = await SeedRestaurantAsync("Create Validation", whatsappEnabled: true);
        int catalogItemId = await SeedCatalogItemAsync(restaurantId, "Cumpleaños", true, 85000);
        HttpClient client = CreateWhatsAppClient("+57 300 123 4567", action: "reservations.mutate");
        DateTime bookingDate = DateTime.UtcNow.AddDays(7).Date.AddHours(18);

        HttpResponseMessage missingEmailResponse = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/reservations",
            new
            {
                restaurantId,
                date = bookingDate,
                seats = 2,
                customerName = "Valentina",
                customerEmail = "",
                specialRequests = "Mesa tranquila",
                occasionCatalogItemIds = new[] { catalogItemId },
                confirmed = true,
                idempotencyKey = "wa-create-missing-email"
            });
        Assert.Equal(HttpStatusCode.BadRequest, missingEmailResponse.StatusCode);

        HttpResponseMessage missingConfirmationResponse = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/reservations",
            new
            {
                restaurantId,
                date = bookingDate,
                seats = 2,
                customerName = "Valentina",
                customerEmail = "vale@example.com",
                occasionCatalogItemIds = new[] { catalogItemId },
                confirmed = false,
                idempotencyKey = "wa-create-missing-confirmation"
            });
        Assert.Equal(HttpStatusCode.BadRequest, missingConfirmationResponse.StatusCode);

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/reservations",
            new
            {
                restaurantId,
                date = bookingDate,
                seats = 2,
                customerName = "Valentina",
                customerEmail = "vale@example.com",
                specialRequests = "Mesa tranquila",
                occasionCatalogItemIds = new[] { catalogItemId },
                confirmed = true,
                idempotencyKey = "wa-create-success"
            });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        BookingDto created = (await createResponse.Content.ReadFromJsonAsync<BookingDto>())!;
        Assert.Equal("vale@example.com", created.CustomerEmail);
        Assert.Equal("Valentina", created.CustomerName);
        Assert.Equal(2, created.Seats);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Booking persisted = db.Bookings.Single(x => x.Id == created.Id);
        Assert.Equal("+573001234567", persisted.CustomerPhoneE164);
        Assert.Equal("573001234567", persisted.CustomerPhoneNormalized);
        Assert.Equal("whatsapp", persisted.CreatedViaChannel);

        BookingOccasionSnapshot snapshot = db.BookingOccasionSnapshots.Single(x => x.BookingId == created.Id);
        Assert.Equal(catalogItemId, snapshot.RestaurantOccasionCatalogItemId);
        Assert.Equal("Cumpleaños", snapshot.Name);
        Assert.Equal(85000, snapshot.EstimatedPriceCop);
    }

    [Fact]
    public async Task CreateUsesIdempotency_ReplaysStoredResult_RejectsFingerprintReuse_AndRejectsCrossRestaurantExtras()
    {
        int restaurantId = await SeedRestaurantAsync("Create Idempotency", whatsappEnabled: true);
        int otherRestaurantId = await SeedRestaurantAsync("Create Idempotency Other", whatsappEnabled: true);
        int catalogItemId = await SeedCatalogItemAsync(restaurantId, "Aniversario", true, 120000);
        int foreignCatalogItemId = await SeedCatalogItemAsync(otherRestaurantId, "Ajeno", true, 99000);
        HttpClient client = CreateWhatsAppClient("+14155550190", action: "reservations.mutate");
        DateTime bookingDate = DateTime.UtcNow.AddDays(8).Date.AddHours(19);
        const string idempotencyKey = "wa-create-idempotency";

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/reservations",
            new
            {
                restaurantId,
                date = bookingDate,
                seats = 2,
                customerName = "Camilo",
                customerEmail = "camilo@example.com",
                occasionCatalogItemIds = new[] { catalogItemId },
                confirmed = true,
                idempotencyKey
            });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        BookingDto first = (await firstResponse.Content.ReadFromJsonAsync<BookingDto>())!;

        await MutateBookingDirectlyAsync(first.Id, bookingDate.AddHours(2), 4);

        HttpResponseMessage replayResponse = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/reservations",
            new
            {
                restaurantId,
                date = bookingDate,
                seats = 2,
                customerName = "Camilo",
                customerEmail = "camilo@example.com",
                occasionCatalogItemIds = new[] { catalogItemId },
                confirmed = true,
                idempotencyKey
            });
        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        BookingDto replayed = (await replayResponse.Content.ReadFromJsonAsync<BookingDto>())!;
        Assert.Equal(first.Id, replayed.Id);
        Assert.Equal(first.Date, replayed.Date);
        Assert.Equal(first.Seats, replayed.Seats);

        HttpResponseMessage changedFingerprintResponse = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/reservations",
            new
            {
                restaurantId,
                date = bookingDate,
                seats = 3,
                customerName = "Camilo",
                customerEmail = "camilo@example.com",
                occasionCatalogItemIds = new[] { catalogItemId },
                confirmed = true,
                idempotencyKey
            });
        Assert.Equal(HttpStatusCode.Conflict, changedFingerprintResponse.StatusCode);

        HttpResponseMessage crossRestaurantExtrasResponse = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/reservations",
            new
            {
                restaurantId,
                date = bookingDate.AddDays(1),
                seats = 2,
                customerName = "Camilo",
                customerEmail = "camilo@example.com",
                occasionCatalogItemIds = new[] { foreignCatalogItemId },
                confirmed = true,
                idempotencyKey = "wa-create-foreign-extra"
            });
        Assert.Equal(HttpStatusCode.BadRequest, crossRestaurantExtrasResponse.StatusCode);
    }

    [Fact]
    public async Task CreateDeniesArchivedDisabledAndUnavailableRestaurants()
    {
        int archivedRestaurantId = await SeedRestaurantAsync("Archived Create", archived: true, whatsappEnabled: true, tableCount: 1);
        int disabledRestaurantId = await SeedRestaurantAsync("Disabled Create", whatsappEnabled: false, tableCount: 1);
        int unavailableRestaurantId = await SeedRestaurantAsync("Unavailable Create", whatsappEnabled: true, tableCount: 1);
        DateTime bookingDate = DateTime.UtcNow.AddDays(9).Date.AddHours(18);
        _ = await SeedBookingAsync(unavailableRestaurantId, "+14155550191", "busy@example.com", "Busy", bookingDate);

        HttpClient client = CreateWhatsAppClient("+14155550192", action: "reservations.mutate");

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.PostAsJsonAsync(
                "/api/private/channels/whatsapp/reservations",
                new
                {
                    restaurantId = archivedRestaurantId,
                    date = bookingDate,
                    seats = 2,
                    customerName = "Laura",
                    customerEmail = "laura@example.com",
                    confirmed = true,
                    idempotencyKey = "wa-create-archived"
                })).StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync(
                "/api/private/channels/whatsapp/reservations",
                new
                {
                    restaurantId = disabledRestaurantId,
                    date = bookingDate,
                    seats = 2,
                    customerName = "Laura",
                    customerEmail = "laura@example.com",
                    confirmed = true,
                    idempotencyKey = "wa-create-disabled"
                })).StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync(
                "/api/private/channels/whatsapp/reservations",
                new
                {
                    restaurantId = unavailableRestaurantId,
                    date = bookingDate,
                    seats = 2,
                    customerName = "Laura",
                    customerEmail = "laura@example.com",
                    confirmed = true,
                    idempotencyKey = "wa-create-unavailable"
                })).StatusCode);
    }

    [Fact]
    public async Task CreateRejectsTimesOutsideRestaurantOpeningHours()
    {
        int restaurantId = await SeedRestaurantAsync("Closed Hours Create", whatsappEnabled: true);
        HttpClient client = CreateWhatsAppClient("+14155550194", action: "reservations.mutate");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/reservations",
            new
            {
                restaurantId,
                date = DateTime.UtcNow.AddDays(9).Date.AddHours(3),
                seats = 2,
                customerName = "Laura",
                customerEmail = "laura@example.com",
                confirmed = true,
                idempotencyKey = "wa-create-closed-hours"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRejectsImmutableFields_RequiresConfirmation_AndRejectsStaleWriter()
    {
        int restaurantId = await SeedRestaurantAsync("Immutable Fields", whatsappEnabled: true);
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
        int restaurantId = await SeedRestaurantAsync("Update Idempotency", whatsappEnabled: true);
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
        int currentConcurrencyToken = await GetBookingConcurrencyTokenAsync(booking.Id);

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
                expectedConcurrencyToken = currentConcurrencyToken
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
                expectedConcurrencyToken = currentConcurrencyToken
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
        int restaurantId = await SeedRestaurantAsync("Cancel Behavior", whatsappEnabled: true);
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
    public async Task DisabledOrArchivedRestaurant_DeniesExistingWhatsAppOwnedReservations_AndListStaysSafe()
    {
        int disabledRestaurantId = await SeedRestaurantAsync("Disabled Existing WA", whatsappEnabled: true);
        int archivedRestaurantId = await SeedRestaurantAsync("Archived Existing WA", whatsappEnabled: true);
        Booking disabledBooking = await SeedBookingAsync(disabledRestaurantId, "+14155550195", "disabled@example.com", "Disabled Owner");
        Booking archivedBooking = await SeedBookingAsync(archivedRestaurantId, "+14155550196", "archived@example.com", "Archived Owner");

        await UpdateRestaurantWhatsAppStateAsync(disabledRestaurantId, whatsappEnabled: false, archived: false);
        await UpdateRestaurantWhatsAppStateAsync(archivedRestaurantId, whatsappEnabled: true, archived: true);

        HttpClient disabledReadClient = CreateWhatsAppClient("+14155550195");
        HttpClient archivedReadClient = CreateWhatsAppClient("+14155550196");
        HttpClient disabledMutateClient = CreateWhatsAppClient("+14155550195", action: "reservations.mutate");
        HttpClient archivedMutateClient = CreateWhatsAppClient("+14155550196", action: "reservations.mutate");

        List<BookingDto> disabledList =
            (await (await disabledReadClient.GetAsync("/api/private/channels/whatsapp/reservations"))
                .Content.ReadFromJsonAsync<List<BookingDto>>())!;
        Assert.DoesNotContain(disabledList, x => x.Id == disabledBooking.Id);

        List<BookingDto> archivedList =
            (await (await archivedReadClient.GetAsync("/api/private/channels/whatsapp/reservations"))
                .Content.ReadFromJsonAsync<List<BookingDto>>())!;
        Assert.DoesNotContain(archivedList, x => x.Id == archivedBooking.Id);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await disabledReadClient.GetAsync($"/api/private/channels/whatsapp/reservations/{disabledBooking.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await archivedReadClient.GetAsync($"/api/private/channels/whatsapp/reservations/{archivedBooking.Id}")).StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await disabledMutateClient.PatchAsJsonAsync(
                $"/api/private/channels/whatsapp/reservations/{disabledBooking.Id}",
                new
                {
                    date = disabledBooking.Date.AddHours(1),
                    seats = disabledBooking.Seats + 1,
                    confirmed = true,
                    idempotencyKey = "disabled-update",
                    expectedConcurrencyToken = disabledBooking.ConcurrencyToken
                })).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await archivedMutateClient.PatchAsJsonAsync(
                $"/api/private/channels/whatsapp/reservations/{archivedBooking.Id}",
                new
                {
                    date = archivedBooking.Date.AddHours(1),
                    seats = archivedBooking.Seats + 1,
                    confirmed = true,
                    idempotencyKey = "archived-update",
                    expectedConcurrencyToken = archivedBooking.ConcurrencyToken
                })).StatusCode);

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await disabledMutateClient.PostAsJsonAsync(
                $"/api/private/channels/whatsapp/reservations/{disabledBooking.Id}/cancel",
                new
                {
                    confirmed = true,
                    idempotencyKey = "disabled-cancel",
                    expectedConcurrencyToken = disabledBooking.ConcurrencyToken
                })).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await archivedMutateClient.PostAsJsonAsync(
                $"/api/private/channels/whatsapp/reservations/{archivedBooking.Id}/cancel",
                new
                {
                    confirmed = true,
                    idempotencyKey = "archived-cancel",
                    expectedConcurrencyToken = archivedBooking.ConcurrencyToken
                })).StatusCode);
    }

    [Fact]
    public async Task GlobalKillSwitch_DisablesWhatsAppAuthenticationEvenWithValidCredentialAndAssertion()
    {
        using var disabledFactory = new WhatsAppDisabledTestWebAppFactory();
        HttpClient client = disabledFactory.CreateDefaultClient(
            new FreshWhatsAppAssertionHandler("+14155550197", "reservations.read", null));

        HttpResponseMessage response = await client.GetAsync("/api/private/channels/whatsapp/restaurants");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StaleWhatsAppWriterIsRejectedAfterAdminUpdateAdvancesConcurrencyToken()
    {
        int restaurantId = await SeedRestaurantAsync("Admin Concurrency", whatsappEnabled: true);
        Booking booking = await SeedBookingAsync(restaurantId, "+14155550198", "admin-concurrency@example.com", "Admin Owner");

        HttpClient adminClient = _factory.CreateAuthenticatedClient(AdminRole.BookingEditor);
        HttpResponseMessage adminUpdateResponse = await adminClient.PutAsJsonAsync(
            $"/api/admin/bookings/{booking.Id}",
            new
            {
                id = booking.Id,
                restaurantId = booking.RestaurantId,
                sectionId = booking.SectionId,
                tableId = booking.TableId,
                date = booking.Date.AddHours(2),
                customerEmail = booking.CustomerEmail,
                customerName = booking.CustomerName,
                seats = booking.Seats + 1,
                specialRequests = booking.SpecialRequests,
                bookingRef = booking.BookingRef,
                endTime = booking.EndTime,
                isCancelled = booking.IsCancelled,
                cancelledAt = booking.CancelledAt
            });
        Assert.Equal(HttpStatusCode.OK, adminUpdateResponse.StatusCode);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Booking updated = db.Bookings.Single(x => x.Id == booking.Id);
            Assert.Equal(booking.ConcurrencyToken + 1, updated.ConcurrencyToken);
        }

        HttpClient whatsappClient = CreateWhatsAppClient("+14155550198", action: "reservations.mutate");
        HttpResponseMessage staleWriterResponse = await whatsappClient.PatchAsJsonAsync(
            $"/api/private/channels/whatsapp/reservations/{booking.Id}",
            new
            {
                date = booking.Date.AddHours(3),
                seats = booking.Seats + 2,
                confirmed = true,
                idempotencyKey = "stale-after-admin-update",
                expectedConcurrencyToken = booking.ConcurrencyToken
            });

        Assert.Equal(HttpStatusCode.Conflict, staleWriterResponse.StatusCode);
    }

    [Fact]
    public async Task OccasionCatalogReturnsOnlyActiveItems_ForVisibleNonArchivedRestaurant()
    {
        int visibleRestaurantId = await SeedRestaurantAsync("Catalog Visible", whatsappEnabled: true);
        int archivedRestaurantId = await SeedRestaurantAsync("Catalog Archived", archived: true, whatsappEnabled: true);
        int disabledRestaurantId = await SeedRestaurantAsync("Catalog Disabled", whatsappEnabled: false);

        await SeedCatalogItemAsync(visibleRestaurantId, "Birthday", true);
        await SeedCatalogItemAsync(visibleRestaurantId, "Inactive", false);
        await SeedCatalogItemAsync(archivedRestaurantId, "Archived Active", true);
        await SeedCatalogItemAsync(disabledRestaurantId, "Disabled Active", true);

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

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await client.GetAsync(
                $"/api/private/channels/whatsapp/restaurants/{disabledRestaurantId}/occasion-catalog")).StatusCode);
    }

    [Fact]
    public async Task RestaurantsEndpoint_ReturnsOnlyEnabledVisibleRestaurants()
    {
        int visibleRestaurantId = await SeedRestaurantAsync("Visible WA", whatsappEnabled: true);
        _ = await SeedRestaurantAsync("Disabled WA", whatsappEnabled: false);
        _ = await SeedRestaurantAsync("Archived WA", archived: true, whatsappEnabled: true);

        HttpClient client = CreateWhatsAppClient("+14155550141");
        HttpResponseMessage response = await client.GetAsync("/api/private/channels/whatsapp/restaurants");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<WhatsAppRestaurantListItemResponse> restaurants =
            (await response.Content.ReadFromJsonAsync<List<WhatsAppRestaurantListItemResponse>>())!;

        WhatsAppRestaurantListItemResponse item = Assert.Single(restaurants.Where(x => x.Id == visibleRestaurantId));
        Assert.Equal(visibleRestaurantId, item.Id);
        Assert.Equal("Visible WA", item.Name);
    }

    [Fact]
    public async Task HandoffAudit_PersistsDurableRecord()
    {
        int restaurantId = await SeedRestaurantAsync("Handoff", whatsappEnabled: true);
        Booking booking = await SeedBookingAsync(restaurantId, "+14155550193", "handoff@example.com", "Handoff");
        HttpClient client = CreateWhatsAppClient("+14155550193", action: "reservations.mutate");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/handoffs",
            new
            {
                restaurantId,
                bookingId = booking.Id,
                summary = "Cliente pide ayuda humana por cambio no soportado.",
                confirmed = true,
                idempotencyKey = "wa-handoff-1"
            });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        WhatsAppHandoffAudit audit = db.WhatsAppHandoffAudits.Single(x => x.RestaurantId == restaurantId && x.BookingId == booking.Id);
        Assert.Equal(restaurantId, audit.RestaurantId);
        Assert.Equal(booking.Id, audit.BookingId);
        Assert.Equal("+14155550193", audit.VerifiedPhoneE164);
        Assert.Equal("14155550193", audit.VerifiedPhoneNormalized);
        Assert.Equal("Cliente pide ayuda humana por cambio no soportado.", audit.SummarySnapshot);
    }

    [Fact]
    public async Task HandoffAudit_SanitizesAndBoundsPersistedSummaryAndDestination()
    {
        int restaurantId = await SeedRestaurantAsync("Handoff Bounds", whatsappEnabled: true);
        await SetRestaurantHandoffDestinationAsync(restaurantId, "  +57 300 222 3333  ");
        HttpClient client = CreateWhatsAppClient("+14155550199", action: "reservations.mutate");

        string longSummary = $"  {new string('A', 600)}   {new string('B', 600)}  ";
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/private/channels/whatsapp/handoffs",
            new
            {
                restaurantId,
                summary = longSummary,
                confirmed = true,
                idempotencyKey = "wa-handoff-bounded"
            });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        WhatsAppHandoffAudit audit = db.WhatsAppHandoffAudits.Single(x => x.RestaurantId == restaurantId);
        Assert.True(audit.SummarySnapshot.Length <= 1024);
        Assert.DoesNotContain("  ", audit.SummarySnapshot);
        Assert.Equal("+573002223333", audit.HandoffDestinationSnapshot);
    }

    [Fact]
    public async Task SuperAdmin_CanManageRestaurantWhatsAppSettings()
    {
        int restaurantId = await SeedRestaurantAsync("Settings Admin", whatsappEnabled: false);
        HttpClient superAdmin = _factory.CreateAuthenticatedClient();
        HttpClient bookingEditor = _factory.CreateAuthenticatedClient(AdminRole.BookingEditor);

        HttpResponseMessage forbiddenResponse = await bookingEditor.PutAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/whatsapp-settings",
            new
            {
                isWhatsAppTestEnabled = true,
                handoffWhatsAppE164 = "+573001111111"
            });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        HttpResponseMessage response = await superAdmin.PutAsJsonAsync(
            $"/api/admin/restaurants/{restaurantId}/whatsapp-settings",
            new
            {
                isWhatsAppTestEnabled = true,
                handoffWhatsAppE164 = "+573001111111"
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        RestaurantWhatsAppSettingsResponse settings =
            (await response.Content.ReadFromJsonAsync<RestaurantWhatsAppSettingsResponse>())!;
        Assert.True(settings.IsWhatsAppTestEnabled);
        Assert.Equal("+573001111111", settings.HandoffWhatsAppE164);

        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Restaurant restaurant = db.Restaurants.Single(x => x.Id == restaurantId);
        Assert.True(restaurant.IsWhatsAppTestEnabled);
        Assert.Equal("+573001111111", restaurant.HandoffWhatsAppE164);
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

    private async Task UpdateRestaurantWhatsAppStateAsync(int restaurantId, bool whatsappEnabled, bool archived)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Restaurant restaurant = db.Restaurants.Single(x => x.Id == restaurantId);
        restaurant.IsWhatsAppTestEnabled = whatsappEnabled;
        restaurant.IsArchived = archived;
        await db.SaveChangesAsync();
    }

    private async Task<int> GetBookingConcurrencyTokenAsync(int bookingId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.Bookings.Single(x => x.Id == bookingId).ConcurrencyToken;
    }

    private async Task SetRestaurantHandoffDestinationAsync(int restaurantId, string? handoffE164)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Restaurant restaurant = db.Restaurants.Single(x => x.Id == restaurantId);
        restaurant.HandoffWhatsAppE164 = handoffE164;
        await db.SaveChangesAsync();
    }

    private async Task<int> SeedRestaurantAsync(
        string name,
        bool archived = false,
        bool whatsappEnabled = false,
        int tableCount = 2)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var restaurant = new Restaurant
        {
            Name = name,
            OpenTime = "11:00",
            CloseTime = "22:00",
            Timezone = "UTC",
            IsArchived = archived,
            IsWhatsAppTestEnabled = whatsappEnabled,
            HandoffWhatsAppE164 = whatsappEnabled ? "+573000000000" : null
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        db.Sections.Add(new Section { Name = $"{name} Main", RestaurantId = restaurant.Id });
        await db.SaveChangesAsync();
        int sectionId = db.Sections.Single(x => x.RestaurantId == restaurant.Id).Id;

        for (int i = 1; i <= tableCount; i += 1)
        {
            db.Tables.Add(new Table { Name = $"{name} T{i}", Seats = 4, SectionId = sectionId });
        }
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

    private async Task<int> SeedCatalogItemAsync(int restaurantId, string name, bool isActive, int estimatedPriceCop = 50000)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = new RestaurantOccasionCatalogItem
        {
            RestaurantId = restaurantId,
            Name = name,
            EstimatedPriceCop = estimatedPriceCop,
            IsActive = isActive,
            SortOrder = await db.RestaurantOccasionCatalogItems
                .Where(x => x.RestaurantId == restaurantId)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        db.RestaurantOccasionCatalogItems.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
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

    private sealed class WhatsAppRestaurantListItemResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class RestaurantWhatsAppSettingsResponse
    {
        public bool IsWhatsAppTestEnabled { get; set; }
        public string? HandoffWhatsAppE164 { get; set; }
    }

    private sealed class WhatsAppDisabledTestWebAppFactory : TestWebAppFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("WhatsAppChannel:Enabled", "false");
        }
    }
}
