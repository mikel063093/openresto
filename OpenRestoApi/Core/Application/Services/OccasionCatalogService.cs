using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class OccasionCatalogService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<OccasionCatalogItemDto>> ListAsync(int restaurantId)
    {
        await EnsureRestaurantExistsAsync(restaurantId);

        return await _db.RestaurantOccasionCatalogItems
            .Where(x => x.RestaurantId == restaurantId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<OccasionCatalogItemDto> CreateAsync(int restaurantId, UpsertOccasionCatalogItemRequest request)
    {
        await EnsureRestaurantExistsAsync(restaurantId);
        ValidateRequest(request);

        int nextSortOrder = await _db.RestaurantOccasionCatalogItems
            .Where(x => x.RestaurantId == restaurantId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync() ?? -1;

        var entity = new RestaurantOccasionCatalogItem
        {
            RestaurantId = restaurantId,
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            EstimatedPriceCop = request.EstimatedPriceCop,
            IsActive = request.IsActive,
            SortOrder = nextSortOrder + 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.RestaurantOccasionCatalogItems.Add(entity);
        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<OccasionCatalogItemDto> UpdateAsync(int restaurantId, int itemId, UpsertOccasionCatalogItemRequest request)
    {
        ValidateRequest(request);

        RestaurantOccasionCatalogItem entity = await _db.RestaurantOccasionCatalogItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.RestaurantId == restaurantId)
            ?? throw new NotFoundException("El ítem del catálogo no existe.");

        entity.Name = request.Name.Trim();
        entity.Description = NormalizeDescription(request.Description);
        entity.EstimatedPriceCop = request.EstimatedPriceCop;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task DeleteAsync(int restaurantId, int itemId)
    {
        RestaurantOccasionCatalogItem entity = await _db.RestaurantOccasionCatalogItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.RestaurantId == restaurantId)
            ?? throw new NotFoundException("El ítem del catálogo no existe.");

        _db.RestaurantOccasionCatalogItems.Remove(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<BookingOccasionSnapshot>> CreateSnapshotsAsync(int bookingId, IReadOnlyCollection<int> catalogItemIds)
    {
        Booking booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId)
            ?? throw new NotFoundException("La reserva no existe.");

        List<RestaurantOccasionCatalogItem> items = await _db.RestaurantOccasionCatalogItems
            .Where(x => x.RestaurantId == booking.RestaurantId && catalogItemIds.Contains(x.Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        if (items.Count != catalogItemIds.Count)
        {
            throw new ValidationException("Uno o más ítems del catálogo no existen para este restaurante.");
        }

        var snapshots = items.Select(item => new BookingOccasionSnapshot
        {
            BookingId = booking.Id,
            RestaurantOccasionCatalogItemId = item.Id,
            Name = item.Name,
            Description = item.Description,
            EstimatedPriceCop = item.EstimatedPriceCop,
            CreatedAtUtc = DateTime.UtcNow
        }).ToList();

        _db.BookingOccasionSnapshots.AddRange(snapshots);
        await _db.SaveChangesAsync();
        return snapshots;
    }

    private async Task EnsureRestaurantExistsAsync(int restaurantId)
    {
        bool exists = await _db.Restaurants.AnyAsync(x => x.Id == restaurantId);
        if (!exists)
        {
            throw new NotFoundException("El restaurante no existe.");
        }
    }

    private static void ValidateRequest(UpsertOccasionCatalogItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("El nombre del ítem es obligatorio.");
        }

        if (request.EstimatedPriceCop < 0)
        {
            throw new ValidationException("El precio estimado en COP no puede ser negativo.");
        }
    }

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static OccasionCatalogItemDto ToDto(RestaurantOccasionCatalogItem entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        EstimatedPriceCop = entity.EstimatedPriceCop,
        IsActive = entity.IsActive,
        SortOrder = entity.SortOrder
    };
}
