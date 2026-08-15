using System.Text.Json.Serialization;

namespace OpenRestoApi.Core.Application.DTOs;

public sealed class WhatsAppReservationUpdateRequestDto
{
    public DateTime Date { get; set; }
    public int Seats { get; set; }
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int ExpectedConcurrencyToken { get; set; }

    public int? RestaurantId { get; set; }
    public int? SectionId { get; set; }
    public int? TableId { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? SpecialRequests { get; set; }

    [JsonPropertyName("occasionCatalogItemIds")]
    public List<int>? OccasionCatalogItemIds { get; set; }
}

public sealed class WhatsAppReservationCreateRequestDto
{
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    public int Seats { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;

    [JsonPropertyName("occasionCatalogItemIds")]
    public List<int>? OccasionCatalogItemIds { get; set; }
}

public sealed class WhatsAppReservationCancelRequestDto
{
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int ExpectedConcurrencyToken { get; set; }

    public string? Reason { get; set; }
}

public sealed class WhatsAppRestaurantListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class WhatsAppHandoffRequestDto
{
    public int RestaurantId { get; set; }
    public int? BookingId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class WhatsAppHandoffResultDto
{
    public int AuditId { get; set; }
    public int RestaurantId { get; set; }
    public int? BookingId { get; set; }
    public string HandoffWhatsAppE164 { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
