using System.Text.RegularExpressions;

namespace OpenRestoReservationBot.Infrastructure;

public sealed partial class PiiRedactor
{
    [GeneratedRegex(@"(?<!\w)\+?\d[\d\s\-\(\)]{6,}\d(?!\w)", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    public string Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string redacted = EmailRegex().Replace(value, "[EMAIL_REDACTED]");
        redacted = PhoneRegex().Replace(redacted, "[PHONE_REDACTED]");
        return redacted;
    }
}
