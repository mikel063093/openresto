using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Localization;

namespace OpenRestoApi.Core.Application.Interfaces;

public interface INotificationQueue
{
    void EnqueueBookingCreated(Booking booking, string restaurantName, string locale = ApiLocalization.English);
    void EnqueueBookingCancelled(Booking booking, string restaurantName, string locale = ApiLocalization.English);
    void EnqueueCapacityCheck(int restaurantId, string restaurantName, DateTime bookingDate, string locale = ApiLocalization.English);
}
