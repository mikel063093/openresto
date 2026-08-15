using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenRestoReservationBot.Contracts;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class BotOperationRequest : IValidatableObject
{
    public ReservationBotOperation Operation { get; set; }
    public AvailabilityLookupRequest? Availability { get; set; }
    public ReservationCreateRequest? Create { get; set; }
    public ReservationListRequest? List { get; set; }
    public ReservationDetailRequest? Detail { get; set; }
    public ReservationUpdateRequest? Update { get; set; }
    public ReservationCancelRequest? Cancel { get; set; }
    public OccasionCatalogLookupRequest? OccasionCatalog { get; set; }
    public ReservationHandoffRequest? Handoff { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        int configuredPayloads = CountConfiguredPayloads();
        if (configuredPayloads != 1)
        {
            yield return new ValidationResult(
                "La solicitud debe incluir exactamente un cuerpo tipado para la operación indicada.",
                [nameof(Availability), nameof(Create), nameof(List), nameof(Detail), nameof(Update), nameof(Cancel), nameof(OccasionCatalog), nameof(Handoff)]);
            yield break;
        }

        if (!IsMatchingPayloadConfigured())
        {
            yield return new ValidationResult(
                "La operación solicitada no coincide con el cuerpo enviado.",
                [nameof(Operation)]);
        }
    }

    private int CountConfiguredPayloads()
    {
        int count = 0;
        count += Availability is null ? 0 : 1;
        count += Create is null ? 0 : 1;
        count += List is null ? 0 : 1;
        count += Detail is null ? 0 : 1;
        count += Update is null ? 0 : 1;
        count += Cancel is null ? 0 : 1;
        count += OccasionCatalog is null ? 0 : 1;
        count += Handoff is null ? 0 : 1;
        return count;
    }

    private bool IsMatchingPayloadConfigured() => Operation switch
    {
        ReservationBotOperation.Availability => Availability is not null,
        ReservationBotOperation.Create => Create is not null,
        ReservationBotOperation.List => List is not null,
        ReservationBotOperation.Detail => Detail is not null,
        ReservationBotOperation.Update => Update is not null,
        ReservationBotOperation.Cancel => Cancel is not null,
        ReservationBotOperation.OccasionCatalog => OccasionCatalog is not null,
        ReservationBotOperation.Handoff => Handoff is not null,
        _ => false
    };
}

[JsonConverter(typeof(JsonStringEnumConverter<ReservationBotOperation>))]
public enum ReservationBotOperation
{
    [JsonStringEnumMemberName("availability")]
    Availability,
    [JsonStringEnumMemberName("create")]
    Create,
    [JsonStringEnumMemberName("list")]
    List,
    [JsonStringEnumMemberName("detail")]
    Detail,
    [JsonStringEnumMemberName("update")]
    Update,
    [JsonStringEnumMemberName("cancel")]
    Cancel,
    [JsonStringEnumMemberName("occasionCatalog")]
    OccasionCatalog,
    [JsonStringEnumMemberName("handoff")]
    Handoff
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AvailabilityLookupRequest
{
    [Range(1, int.MaxValue)]
    public int RestaurantId { get; set; }

    public DateTime Date { get; set; }

    [Range(1, int.MaxValue)]
    public int Seats { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationCreateRequest
{
    [Range(1, int.MaxValue)]
    public int RestaurantId { get; set; }

    public DateTime Date { get; set; }

    [Range(1, int.MaxValue)]
    public int Seats { get; set; }

    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    public string? SpecialRequests { get; set; }
    public bool Confirmed { get; set; }

    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;

    public List<int>? OccasionCatalogItemIds { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationListRequest
{
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationDetailRequest
{
    [Range(1, int.MaxValue)]
    public int ReservationId { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationUpdateRequest
{
    [Range(1, int.MaxValue)]
    public int ReservationId { get; set; }

    public DateTime Date { get; set; }

    [Range(1, int.MaxValue)]
    public int Seats { get; set; }

    public bool Confirmed { get; set; }

    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int ExpectedConcurrencyToken { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationCancelRequest
{
    [Range(1, int.MaxValue)]
    public int ReservationId { get; set; }

    public bool Confirmed { get; set; }

    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int ExpectedConcurrencyToken { get; set; }

    public string? Reason { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class OccasionCatalogLookupRequest
{
    [Range(1, int.MaxValue)]
    public int RestaurantId { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReservationHandoffRequest
{
    [Range(1, int.MaxValue)]
    public int RestaurantId { get; set; }

    public int? BookingId { get; set; }

    [Required]
    public string Summary { get; set; } = string.Empty;

    public bool Confirmed { get; set; }

    [Required]
    public string IdempotencyKey { get; set; } = string.Empty;
}
