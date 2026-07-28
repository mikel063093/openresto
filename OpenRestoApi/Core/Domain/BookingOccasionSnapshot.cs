namespace OpenRestoApi.Core.Domain;

public sealed class BookingOccasionSnapshot
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int RestaurantOccasionCatalogItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EstimatedPriceCop { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
