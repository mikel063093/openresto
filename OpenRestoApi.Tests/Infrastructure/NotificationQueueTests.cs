using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Notifications;

namespace OpenRestoApi.Tests.Infrastructure;

public class NotificationQueueTests
{
    private static Booking CreateBooking() => new()
    {
        RestaurantId = 1,
        BookingRef = "REF1",
        CustomerName = "Jane",
        CustomerEmail = "jane@test.com",
        Seats = 2,
        Date = DateTime.UtcNow,
    };

    [Fact]
    public void EnqueueBookingCreated_WritesBookingCreatedWork()
    {
        var queue = new NotificationQueue();
        Booking booking = CreateBooking();

        queue.EnqueueBookingCreated(booking, "Test Resto");

        Assert.True(queue.TryReadForTests(out NotificationWorkItem? item));
        var work = Assert.IsType<BookingCreatedWork>(item);
        Assert.Same(booking, work.Booking);
        Assert.Equal("Test Resto", work.RestaurantName);
        Assert.Equal("en", work.Locale);
    }

    [Fact]
    public void EnqueueBookingCancelled_WritesBookingCancelledWork()
    {
        var queue = new NotificationQueue();
        Booking booking = CreateBooking();

        queue.EnqueueBookingCancelled(booking, "Test Resto", "es-CO");

        Assert.True(queue.TryReadForTests(out NotificationWorkItem? item));
        var work = Assert.IsType<BookingCancelledWork>(item);
        Assert.Same(booking, work.Booking);
        Assert.Equal("Test Resto", work.RestaurantName);
        Assert.Equal("es-CO", work.Locale);
    }

    [Fact]
    public void EnqueueCapacityCheck_WritesCapacityCheckWork()
    {
        var queue = new NotificationQueue();
        DateTime date = DateTime.UtcNow;

        queue.EnqueueCapacityCheck(42, "Test Resto", date, "es-CO");

        Assert.True(queue.TryReadForTests(out NotificationWorkItem? item));
        var work = Assert.IsType<CapacityCheckWork>(item);
        Assert.Equal(42, work.RestaurantId);
        Assert.Equal("Test Resto", work.RestaurantName);
        Assert.Equal(date, work.BookingDate);
        Assert.Equal("es-CO", work.Locale);
    }

    [Fact]
    public void ImplementsINotificationQueue()
    {
        var queue = new NotificationQueue();
        Assert.IsAssignableFrom<OpenRestoApi.Core.Application.Interfaces.INotificationQueue>(queue);
    }

    [Fact]
    public void EnqueueOperatorEscalation_WritesOperatorEscalationWork()
    {
        var queue = new NotificationQueue();
        Booking booking = CreateBooking();

        bool enqueued = queue.EnqueueOperatorEscalation(booking, "Test Resto", "operator@test.com", "Needs manager review");

        Assert.True(enqueued);
        Assert.True(queue.TryReadForTests(out NotificationWorkItem? item));
        var work = Assert.IsType<OperatorEscalationWork>(item);
        Assert.Same(booking, work.Booking);
        Assert.Equal("operator@test.com", work.OperatorIdentifier);
        Assert.Equal("Needs manager review", work.Reason);
    }
}
