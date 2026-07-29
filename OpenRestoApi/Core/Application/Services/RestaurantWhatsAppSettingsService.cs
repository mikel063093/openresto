using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class RestaurantWhatsAppSettingsService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<RestaurantWhatsAppSettingsDto> GetAsync(int restaurantId)
    {
        Core.Domain.Restaurant restaurant = await _db.Restaurants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == restaurantId)
            ?? throw new NotFoundException("El restaurante no existe.");

        return ToDto(restaurant);
    }

    public async Task<RestaurantWhatsAppSettingsDto> UpdateAsync(int restaurantId, UpdateRestaurantWhatsAppSettingsRequestDto request)
    {
        Core.Domain.Restaurant restaurant = await _db.Restaurants.FirstOrDefaultAsync(x => x.Id == restaurantId)
            ?? throw new NotFoundException("El restaurante no existe.");

        restaurant.IsWhatsAppTestEnabled = request.IsWhatsAppTestEnabled;
        restaurant.HandoffWhatsAppE164 = NormalizeHandoffPhone(request.HandoffWhatsAppE164, request.IsWhatsAppTestEnabled);
        await _db.SaveChangesAsync();

        return ToDto(restaurant);
    }

    private static string? NormalizeHandoffPhone(string? handoffWhatsAppE164, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(handoffWhatsAppE164))
        {
            if (isEnabled)
            {
                throw new ValidationException("El número de handoff de WhatsApp es obligatorio cuando el canal de prueba está habilitado.");
            }

            return null;
        }

        return WhatsAppPhoneOwnershipService.Normalize(handoffWhatsAppE164).E164;
    }

    private static RestaurantWhatsAppSettingsDto ToDto(Core.Domain.Restaurant restaurant) => new()
    {
        RestaurantId = restaurant.Id,
        IsWhatsAppTestEnabled = restaurant.IsWhatsAppTestEnabled,
        HandoffWhatsAppE164 = restaurant.HandoffWhatsAppE164
    };
}
