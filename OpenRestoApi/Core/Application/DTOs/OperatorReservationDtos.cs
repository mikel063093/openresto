namespace OpenRestoApi.Core.Application.DTOs;

public sealed class OperatorReservationCreateRequestDto
{
    public int RestaurantId { get; set; }
    public int? TableId { get; set; }
    public int? SectionId { get; set; }
    public DateTime Date { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public int Seats { get; set; }
    public string? SpecialRequests { get; set; }
    public string? HoldId { get; set; }
}

public sealed class OperatorReservationUpdateRequestDto
{
    public DateTime Date { get; set; }
    public int? TableId { get; set; }
    public int? SectionId { get; set; }
    public int Seats { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? SpecialRequests { get; set; }
}

public sealed class OperatorReservationEscalationRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public sealed record OperatorReservationActionResultDto(
    bool Success,
    string Message,
    BookingDto? Reservation = null);
