using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Tests.Services;

public sealed class WhatsAppPhoneOwnershipServiceTests
{
    [Fact]
    public void StampVerifiedOwnership_NormalizesAndAssignsBookingFields()
    {
        var booking = new Booking
        {
            RestaurantId = 1,
            BookingRef = "ABC123",
            Seats = 2,
            Date = DateTime.UtcNow.AddDays(1)
        };

        var service = new WhatsAppPhoneOwnershipService();

        service.StampVerifiedOwnership(booking, " +57 300 123 4567 ");

        Assert.Equal("+573001234567", booking.CustomerPhoneE164);
        Assert.Equal("573001234567", booking.CustomerPhoneNormalized);
        Assert.Equal("whatsapp", booking.CreatedViaChannel);
    }

    [Fact]
    public void StampVerifiedOwnership_RejectsMissingDigits()
    {
        var booking = new Booking
        {
            RestaurantId = 1,
            BookingRef = "ABC123",
            Seats = 2,
            Date = DateTime.UtcNow.AddDays(1)
        };

        var service = new WhatsAppPhoneOwnershipService();

        InvalidOperationException ex;
        try
        {
            service.StampVerifiedOwnership(booking, "sin-numero");
            throw new Xunit.Sdk.XunitException("Se esperaba InvalidOperationException.");
        }
        catch (InvalidOperationException caught)
        {
            ex = caught;
        }

        Assert.Equal("El número de WhatsApp verificado no es válido.", ex.Message);
    }
}
