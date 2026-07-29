using System.Text.Json.Serialization;

namespace OpenRestoReservationBot.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class BotOperationResponse
{
    public ReservationBotOperation Operation { get; set; }
    public AvailabilityLookupResponse? Availability { get; set; }
    public ReservationRecordResponse? Reservation { get; set; }
    public ReservationListResponse? Reservations { get; set; }
    public ReservationActionResponse? Cancellation { get; set; }
    public OccasionCatalogResponse? OccasionCatalog { get; set; }
    public HandoffRecordResponse? Handoff { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AvailabilityLookupResponse
{
    public OpenRestoAvailabilityDto Result { get; set; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationRecordResponse
{
    public OpenRestoReservationDto Result { get; set; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationListResponse
{
    public IReadOnlyList<OpenRestoReservationDto> Result { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationActionResponse
{
    public OpenRestoReservationActionResultDto Result { get; set; } = new();
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OccasionCatalogResponse
{
    public IReadOnlyList<OpenRestoOccasionCatalogItemDto> Result { get; set; } = [];
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HandoffRecordResponse
{
    public OpenRestoHandoffResultDto Result { get; set; } = new();
}
