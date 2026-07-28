using System.Text;
using OpenRestoApi.Core.Domain;

namespace OpenRestoApi.Core.Application.Services;

public sealed class WhatsAppPhoneOwnershipService
{
    public void StampVerifiedOwnership(Booking booking, string verifiedPhone)
    {
        (string e164, string normalized) = Normalize(verifiedPhone);
        booking.CustomerPhoneE164 = e164;
        booking.CustomerPhoneNormalized = normalized;
        booking.CreatedViaChannel = "whatsapp";
    }

    public static (string E164, string Normalized) Normalize(string value)
    {
        var digits = new StringBuilder();
        foreach (char ch in value)
        {
            if (char.IsDigit(ch))
            {
                digits.Append(ch);
            }
        }

        if (digits.Length < 8)
        {
            throw new InvalidOperationException("El número de WhatsApp verificado no es válido.");
        }

        string normalized = digits.ToString();
        return ($"+{normalized}", normalized);
    }
}
