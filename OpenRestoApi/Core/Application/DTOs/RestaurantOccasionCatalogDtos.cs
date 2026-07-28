namespace OpenRestoApi.Core.Application.DTOs;

public sealed class OccasionCatalogItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EstimatedPriceCop { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public sealed class UpsertOccasionCatalogItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int EstimatedPriceCop { get; set; }
    public bool IsActive { get; set; } = true;
}
