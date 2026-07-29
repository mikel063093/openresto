namespace OpenRestoApi.Core.Domain;

public sealed class WhatsAppHandoffAudit
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }
    public string VerifiedPhoneE164 { get; set; } = string.Empty;
    public string VerifiedPhoneNormalized { get; set; } = string.Empty;
    public string SummarySnapshot { get; set; } = string.Empty;
    public string HandoffDestinationSnapshot { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
