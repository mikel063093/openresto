using System.Text.Json.Serialization;

namespace OpenRestoReservationBot.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoTimeSlotDto
{
    public string Time { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public List<int> AvailableTableIds { get; set; } = [];
    public string Category { get; set; } = string.Empty;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoAvailabilityDto
{
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    public List<OpenRestoTimeSlotDto> Slots { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoReservationDto
{
    public int Id { get; set; }
    public int? TableId { get; set; }
    public int? SectionId { get; set; }
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public int Seats { get; set; }
    public bool IsHeld { get; set; }
    public string? SpecialRequests { get; set; }
    public string? BookingRef { get; set; }
    public DateTime? EndTime { get; set; }
    public string? TableName { get; set; }
    public string? SectionName { get; set; }
    public int? TableSeats { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int ConcurrencyToken { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoReservationActionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public OpenRestoReservationDto? Reservation { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoOccasionCatalogItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EstimatedPriceCop { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoHandoffResultDto
{
    public int AuditId { get; set; }
    public int RestaurantId { get; set; }
    public int? BookingId { get; set; }
    public string HandoffWhatsAppE164 { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoWhatsAppCreateReservationRequestDto
{
    public int RestaurantId { get; set; }
    public DateTime Date { get; set; }
    public int Seats { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<int>? OccasionCatalogItemIds { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoWhatsAppUpdateReservationRequestDto
{
    public DateTime Date { get; set; }
    public int Seats { get; set; }
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int ExpectedConcurrencyToken { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoWhatsAppCancelReservationRequestDto
{
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int ExpectedConcurrencyToken { get; set; }
    public string? Reason { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OpenRestoWhatsAppHandoffRequestDto
{
    public int RestaurantId { get; set; }
    public int? BookingId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public bool Confirmed { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
