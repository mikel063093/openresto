using System.Text.Json.Serialization;

namespace OpenRestoApi.Core.Application.DTOs;

public sealed class WhatsAppReservationUpdateRequestDto
{
    public DateTime Date { get; set; }
    public int Seats { get; set; }
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;

    public int? RestaurantId { get; set; }
    public int? SectionId { get; set; }
    public int? TableId { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? SpecialRequests { get; set; }

    [JsonPropertyName("occasionCatalogItemIds")]
    public List<int>? OccasionCatalogItemIds { get; set; }
}

public sealed class WhatsAppReservationCancelRequestDto
{
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;

    public string? Reason { get; set; }
}
