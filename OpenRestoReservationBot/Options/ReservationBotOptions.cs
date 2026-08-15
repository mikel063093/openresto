namespace OpenRestoReservationBot.Options;

public sealed class ReservationBotOptions
{
    public const string SectionName = "ReservationBot";

    public string InternalCredential { get; set; } = string.Empty;
    public OpenRestoOptions OpenResto { get; set; } = new();
}

public sealed class OpenRestoOptions
{
    public string AvailabilityBaseUrl { get; set; } = string.Empty;
    public string PrivateBaseUrl { get; set; } = string.Empty;
    public string PrivateApiCredential { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
}
