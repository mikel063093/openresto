using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Localization;

namespace OpenRestoApi.Core.Application.Interfaces;

/// <summary>
/// Background-only notification surface: persists <see cref="AdminNotification"/> rows for
/// booking events and capacity thresholds, then dispatches Web Push deliveries. Consumed by
/// <c>NotificationWorker</c> (the background-service reader of <c>INotificationQueue</c>);
/// never called from request-scoped controllers.
/// </summary>
public interface IBookingNotificationService
{
    Task NotifyBookingCreatedAsync(Booking booking, string restaurantName, string locale = ApiLocalization.English);
    Task NotifyBookingCancelledAsync(Booking booking, string restaurantName, string locale = ApiLocalization.English);
    Task CheckAndNotifyCapacityAsync(int restaurantId, string restaurantName, DateTime bookingDate, string locale = ApiLocalization.English);
    Task NotifyOperatorEscalationAsync(Booking booking, string restaurantName, string operatorIdentifier, string reason, int? notificationId = null);
}
