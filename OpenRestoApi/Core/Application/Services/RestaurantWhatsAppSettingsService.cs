using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class RestaurantWhatsAppSettingsService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<RestaurantWhatsAppSettingsDto> UpdateAsync(int restaurantId, UpdateRestaurantWhatsAppSettingsRequestDto request)
    {
        Core.Domain.Restaurant restaurant = await _db.Restaurants.FirstOrDefaultAsync(x => x.Id == restaurantId)
            ?? throw new NotFoundException("Restaurant not found.");

        restaurant.IsWhatsAppTestEnabled = request.IsWhatsAppTestEnabled;
        restaurant.HandoffWhatsAppE164 = NormalizeHandoffPhone(request.HandoffWhatsAppE164, request.IsWhatsAppTestEnabled);
        await _db.SaveChangesAsync();

        return new RestaurantWhatsAppSettingsDto
        {
            RestaurantId = restaurant.Id,
            IsWhatsAppTestEnabled = restaurant.IsWhatsAppTestEnabled,
            HandoffWhatsAppE164 = restaurant.HandoffWhatsAppE164
        };
    }

    private static string? NormalizeHandoffPhone(string? handoffWhatsAppE164, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(handoffWhatsAppE164))
        {
            if (isEnabled)
            {
                throw new ValidationException("HandoffWhatsAppE164 is required when WhatsApp test reservations are enabled.");
            }

            return null;
        }

        return WhatsAppPhoneOwnershipService.Normalize(handoffWhatsAppE164).E164;
    }
}
