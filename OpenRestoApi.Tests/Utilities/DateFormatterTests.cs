using OpenRestoApi.Core.Application.Utilities;

namespace OpenRestoApi.Tests.Utilities;

public class DateFormatterTests
{
    [Fact]
    public void FormatLongDate_ProducesEnglishLongFormByDefault()
    {
        DateTime d = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal("Saturday, 18 April 2026", DateFormatter.FormatLongDate(d));
    }

    [Fact]
    public void FormatLongDate_ProducesSpanishLongForm()
    {
        DateTime d = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.Equal("sábado, 18 de abril de 2026", DateFormatter.FormatLongDate(d, "es-CO"));
    }

    [Fact]
    public void FormatTime_ProducesLocalizedTwelveHourForm()
    {
        DateTime morning = new DateTime(2026, 4, 18, 9, 5, 0, DateTimeKind.Unspecified);
        DateTime afternoon = new DateTime(2026, 4, 18, 15, 45, 0, DateTimeKind.Unspecified);

        Assert.Equal("9:05 AM", DateFormatter.FormatTime(morning));
        Assert.Equal("3:45 PM", DateFormatter.FormatTime(afternoon));
    }

    [Fact]
    public void FormatTime_FallsBackToEnglishWhenLocaleUnsupported()
    {
        DateTime time = new DateTime(2026, 4, 18, 15, 45, 0, DateTimeKind.Unspecified);

        Assert.Equal("3:45 PM", DateFormatter.FormatTime(time, "fr-FR"));
    }

    [Fact]
    public void FormatTimeRange_JoinsStartAndEndWithEnDash()
    {
        DateTime start = new DateTime(2026, 4, 18, 15, 45, 0, DateTimeKind.Unspecified);
        DateTime end = new DateTime(2026, 4, 18, 17, 45, 0, DateTimeKind.Unspecified);

        Assert.Equal("3:45 PM – 5:45 PM", DateFormatter.FormatTimeRange(start, end));
    }
}
