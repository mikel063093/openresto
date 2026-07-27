using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class OperatorAvailabilityService(
    IAvailabilityService availabilityService,
    OperatorIdentityAccessor operatorIdentityAccessor,
    AppDbContext db)
{
    private readonly IAvailabilityService _availabilityService = availabilityService;
    private readonly OperatorIdentityAccessor _operatorIdentityAccessor = operatorIdentityAccessor;
    private readonly AppDbContext _db = db;

    public async Task<AvailabilityResponseDto> GetAvailabilityAsync(int restaurantId, DateTime bookingDate, int seats)
    {
        OperatorIdentityContext identity = _operatorIdentityAccessor.GetCurrent();
        if (!identity.RestaurantIds.Contains(restaurantId))
        {
            await WriteAuditAsync(identity, restaurantId, null, "availability.read", "denied", "restaurant_out_of_scope");
            throw new NotFoundException("Restaurant not found.");
        }

        return await _availabilityService.GetAvailabilityAsync(restaurantId, bookingDate, seats);
    }

    private async Task WriteAuditAsync(
        OperatorIdentityContext identity,
        int restaurantId,
        int? bookingId,
        string action,
        string outcome,
        string? reason)
    {
        _db.OperatorActionAudits.Add(new Core.Domain.OperatorActionAudit
        {
            OperatorPrincipalId = identity.OperatorId,
            OperatorAgentCredentialId = identity.CredentialId,
            RestaurantId = restaurantId,
            BookingId = bookingId,
            Action = action,
            Outcome = outcome,
            Reason = reason,
            CorrelationId = identity.CorrelationId,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
    }
}
