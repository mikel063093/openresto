using System.Globalization;

namespace OpenRestoApi.Infrastructure.Localization;

public static class EmailNotificationLocalization
{
    public static string NormalizeLocale(string? locale)
        => ApiLocalization.ResolveLocale(locale);

    public static string BookingConfirmationSubject(string locale, string restaurantName)
        => IsSpanish(locale) ? $"Reserva confirmada – {restaurantName}" : $"Booking confirmed – {restaurantName}";

    public static string BookingConfirmedLabel(string locale)
        => IsSpanish(locale) ? "Reserva confirmada" : "Booking Confirmed";

    public static string LocationLabel(string locale)
        => IsSpanish(locale) ? "Ubicacion" : "Location";

    public static string DirectionsCta(string locale)
        => IsSpanish(locale) ? "Como llegar →" : "Get directions →";

    public static string Greeting(string locale, string? customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            return IsSpanish(locale) ? "Tu reserva esta lista." : "You're all set!";
        }

        return IsSpanish(locale)
            ? $"Tu reserva esta lista, {customerName}."
            : $"You're all set, {customerName}!";
    }

    public static string LookingForward(string locale)
        => IsSpanish(locale) ? "Te esperamos pronto." : "We're looking forward to seeing you.";

    public static string ReferenceLabel(string locale)
        => IsSpanish(locale) ? "Referencia" : "Reference";

    public static string DateLabel(string locale)
        => IsSpanish(locale) ? "Fecha" : "Date";

    public static string TimeLabel(string locale)
        => IsSpanish(locale) ? "Hora" : "Time";

    public static string GuestsLabel(string locale)
        => IsSpanish(locale) ? "Comensales" : "Guests";

    public static string GuestCount(string locale, int seats)
    {
        string unit = IsSpanish(locale)
            ? (seats == 1 ? "comensal" : "comensales")
            : (seats == 1 ? "guest" : "guests");
        return $"{seats} {unit}";
    }

    public static string SectionLabel(string locale)
        => IsSpanish(locale) ? "Seccion" : "Section";

    public static string TableLabel(string locale)
        => "Table";

    public static string SpecialRequestsLabel(string locale)
        => IsSpanish(locale) ? "Solicitudes especiales" : "Special requests";

    public static string ManageBookingCta(string locale)
        => IsSpanish(locale) ? "Gestionar tu reserva" : "Manage your booking";

    public static string BookingFooterNote(string locale)
        => IsSpanish(locale)
            ? "Necesitas cambiar o cancelar tu reserva?<br>Usa el enlace de arriba o visita nuestro sitio web."
            : "Need to change or cancel your reservation?<br>Use the link above or visit our website.";

    public static string NotificationGuestFallback(string locale)
        => IsSpanish(locale) ? "Invitado" : "Guest";

    public static string NewBookingTitle(string locale, string restaurantName)
        => IsSpanish(locale) ? $"Nueva reserva - {restaurantName}" : $"New booking - {restaurantName}";

    public static string BookingCancelledTitle(string locale, string restaurantName)
        => IsSpanish(locale) ? $"Reserva cancelada - {restaurantName}" : $"Booking cancelled - {restaurantName}";

    public static string NearlyFullTitle(string locale, string restaurantName)
        => IsSpanish(locale) ? $"Casi lleno - {restaurantName}" : $"Nearly full - {restaurantName}";

    public static string BookingNotificationBody(string locale, string customerName, int seats, string localTime)
        => $"{customerName} · {GuestCount(locale, seats)} · {localTime}";

    public static string NearlyFullBody(string locale, int bookedTables, int totalTables, int percent)
        => IsSpanish(locale)
            ? $"{bookedTables} de {totalTables} mesas reservadas hoy ({percent}%)"
            : $"{bookedTables} of {totalTables} tables booked today ({percent}%)";

    public static string FormatNotificationTime(DateTime utc, string locale)
    {
        string normalized = NormalizeLocale(locale);
        CultureInfo culture = GetCulture(normalized);
        string pattern = IsSpanish(normalized) ? "ddd d MMM 'a las' h:mm tt" : "ddd d MMM 'at' h:mm tt";
        return utc.ToString(pattern, culture);
    }

    private static bool IsSpanish(string? locale)
        => string.Equals(NormalizeLocale(locale), ApiLocalization.ColombianSpanish, StringComparison.OrdinalIgnoreCase);

    private static CultureInfo GetCulture(string locale)
    {
        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo(ApiLocalization.English);
        }
    }
}
