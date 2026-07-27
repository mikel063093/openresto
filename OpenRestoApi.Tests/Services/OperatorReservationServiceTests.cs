using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Mappings;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Auth;
using OpenRestoApi.Infrastructure.Persistence;
using OpenRestoApi.Infrastructure.Persistence.Repositories;

namespace OpenRestoApi.Tests.Services;

public sealed class OperatorReservationServiceTests
{
    private static BookingService CreateBookingService(AppDbContext db)
    {
        return new BookingService(
            new BookingRepository(db),
            new TableRepository(db),
            new SectionRepository(db),
            new RestaurantRepository(db),
            new OpenRestoApi.Infrastructure.Holds.HoldService(new UtcClock()),
            new BookingMapper(),
            new TableAutoAssigner(new BookingRepository(db)));
    }

    private static OperatorReservationService CreateService(
        AppDbContext db,
        INotificationQueue queue,
        int operatorId = 99,
        int credentialId = 199,
        params int[] restaurantIds)
    {
        restaurantIds = restaurantIds.Length == 0 ? [1] : restaurantIds;
        List<Claim> claims =
        [
            new Claim(OperatorAuthenticationDefaults.OperatorIdClaim, operatorId.ToString()),
            new Claim(OperatorAuthenticationDefaults.OperatorIdentifierClaim, "operator@test.com"),
            new Claim(OperatorAuthenticationDefaults.CredentialIdClaim, credentialId.ToString())
        ];
        claims.AddRange(restaurantIds.Select(id => new Claim(OperatorAuthenticationDefaults.RestaurantIdClaim, id.ToString())));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, OperatorAuthenticationDefaults.SchemeName));

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal,
                TraceIdentifier = "trace-operator-tests"
            }
        };

        return new OperatorReservationService(
            CreateBookingService(db),
            new BookingRepository(db),
            queue,
            new BookingMapper(),
            new OperatorIdentityAccessor(httpContextAccessor),
            db);
    }

    private sealed class UtcClock : ISystemClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    [Fact]
    public async Task EscalateOwnAsync_RejectsOversizedReason_BeforeAuditOrNotification()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(EscalateOwnAsync_RejectsOversizedReason_BeforeAuditOrNotification));
        SeedOperatorBooking(db);
        Mock<INotificationQueue> queue = new();
        OperatorReservationService service = CreateService(db, queue.Object);

        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.EscalateOwnAsync(1, new OperatorReservationEscalationRequestDto
            {
                Reason = new string('x', 1025)
            }));

        Assert.Contains("1024", ex.Message);
        Assert.Empty(db.OperatorActionAudits.ToList());
        Assert.Empty(db.AdminNotifications.ToList());
        queue.Verify(x => x.EnqueueOperatorEscalation(It.IsAny<Booking>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task EscalateOwnAsync_PersistsNotificationIntent_AndSurfacesQueueFailure()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(EscalateOwnAsync_PersistsNotificationIntent_AndSurfacesQueueFailure));
        SeedOperatorBooking(db);
        Mock<INotificationQueue> queue = new();
        queue.Setup(x => x.EnqueueOperatorEscalation(It.IsAny<Booking>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
            .Throws(new InvalidOperationException("queue unavailable"));

        OperatorReservationService service = CreateService(db, queue.Object);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EscalateOwnAsync(1, new OperatorReservationEscalationRequestDto
            {
                Reason = "Needs manager follow-up"
            }));

        Assert.Equal("queue unavailable", ex.Message);

        AdminNotification notification = await db.AdminNotifications.SingleAsync();
        Assert.Equal(NotificationType.OperatorEscalation, notification.Type);
        Assert.Equal(1, notification.BookingId);
        Assert.Equal("OPENResto", notification.RestaurantName);
        Assert.Equal("Notification queue unavailable.", notification.PushError);

        Assert.Contains(db.OperatorActionAudits.ToList(), x =>
            x.BookingId == 1 &&
            x.Action == "reservation.escalate" &&
            x.Outcome == "failed" &&
            x.Reason == "queue_enqueue_failed");
    }

    [Fact]
    public async Task ListOwnAsync_WritesSuccessAuditWithoutReason()
    {
        using AppDbContext db = TestDbFactory.Create(nameof(ListOwnAsync_WritesSuccessAuditWithoutReason));
        SeedOperatorBooking(db);
        Mock<INotificationQueue> queue = new();
        OperatorReservationService service = CreateService(db, queue.Object);

        List<BookingDto> result = await service.ListOwnAsync();

        Assert.Single(result);
        Assert.Contains(db.OperatorActionAudits.ToList(), x =>
            x.Action == "reservation.list" &&
            x.Outcome == "success" &&
            x.Reason == null &&
            x.BookingId == null);
    }

    private static void SeedOperatorBooking(AppDbContext db)
    {
        var restaurant = new Restaurant
        {
            Id = 1,
            Name = "OPENResto",
            OpenTime = "11:00",
            CloseTime = "13:00",
            Timezone = "UTC"
        };
        db.Restaurants.Add(restaurant);
        db.Sections.Add(new Section { Id = 1, Name = "Main", RestaurantId = 1 });
        db.Tables.Add(new Table { Id = 1, Name = "T1", Seats = 4, SectionId = 1 });
        db.Bookings.Add(new Booking
        {
            Id = 1,
            BookingRef = "BOOK-1",
            RestaurantId = 1,
            SectionId = 1,
            TableId = 1,
            CustomerName = "Guest",
            CustomerEmail = "guest@example.com",
            Seats = 2,
            Date = DateTime.UtcNow.AddDays(7),
            EndTime = DateTime.UtcNow.AddDays(7).AddHours(1),
            CreatedByOperatorId = 99,
            CreatedViaChannel = "operator_mcp"
        });
        db.SaveChanges();
    }
}
