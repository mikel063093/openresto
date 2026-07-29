using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Mappings;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class OperatorReservationService(
    BookingService bookingService,
    IBookingRepository bookingRepository,
    INotificationQueue notificationQueue,
    BookingMapper mapper,
    OperatorIdentityAccessor operatorIdentityAccessor,
    AppDbContext db)
{
    private const string OperatorChannel = "operator_mcp";
    private const int MaxEscalationReasonLength = 1024;

    private readonly BookingService _bookingService = bookingService;
    private readonly IBookingRepository _bookingRepository = bookingRepository;
    private readonly INotificationQueue _notificationQueue = notificationQueue;
    private readonly BookingMapper _mapper = mapper;
    private readonly OperatorIdentityAccessor _operatorIdentityAccessor = operatorIdentityAccessor;
    private readonly AppDbContext _db = db;

    public async Task<BookingDto> CreateAsync(int restaurantId, OperatorReservationCreateRequestDto request)
    {
        OperatorIdentityContext identity = _operatorIdentityAccessor.GetCurrent();
        EnsureRestaurantScope(identity, restaurantId);

        if (request.RestaurantId != restaurantId)
        {
            await WriteAuditAsync(identity, restaurantId, null, "reservation.create", "denied", "route_body_restaurant_mismatch");
            throw new ValidationException("RestaurantId must match the route.");
        }

        BookingDto created = await _bookingService.CreateBookingAsync(new BookingDto
        {
            RestaurantId = request.RestaurantId,
            SectionId = request.SectionId,
            TableId = request.TableId,
            Date = request.Date,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            Seats = request.Seats,
            SpecialRequests = request.SpecialRequests,
            HoldId = request.HoldId,
        });

        Booking? persisted = await _bookingRepository.FindByIdAsync(created.Id);
        if (persisted is null)
        {
            throw new NotFoundException("Booking not found.");
        }

        persisted.CreatedByOperatorId = identity.OperatorId;
        persisted.CreatedViaChannel = OperatorChannel;
        await _bookingRepository.SaveChangesAsync();

        Booking hydrated = await GetOwnedScopedBookingOrThrowAsync(created.Id, identity, "reservation.create");
        await WriteAuditAsync(identity, restaurantId, hydrated.Id, "reservation.create", "success", null);
        return _mapper.ToDto(hydrated);
    }

    public async Task<List<BookingDto>> ListOwnAsync()
    {
        OperatorIdentityContext identity = _operatorIdentityAccessor.GetCurrent();
        List<Booking> bookings = await _bookingRepository.GetByOperatorAsync(identity.OperatorId);
        List<BookingDto> result = _mapper.ToDtoList(bookings.Where(b => identity.RestaurantIds.Contains(b.RestaurantId))).ToList();
        await WriteAuditAsync(identity, identity.RestaurantIds.FirstOrDefault(), null, "reservation.list", "success", null);
        return result;
    }

    public async Task<BookingDto?> GetOwnAsync(int id)
    {
        OperatorIdentityContext identity = _operatorIdentityAccessor.GetCurrent();
        Booking? booking = await TryGetOwnedScopedBookingAsync(id, identity, "reservation.read");
        if (booking is null)
        {
            return null;
        }

        await WriteAuditAsync(identity, booking.RestaurantId, booking.Id, "reservation.read", "success", null);
        return _mapper.ToDto(booking);
    }

    public async Task<BookingDto> GetOwnOrThrowAsync(int id)
    {
        OperatorIdentityContext identity = _operatorIdentityAccessor.GetCurrent();
        Booking booking = await GetOwnedScopedBookingOrThrowAsync(id, identity, "reservation.read");
        await WriteAuditAsync(identity, booking.RestaurantId, booking.Id, "reservation.read", "success", null);
        return _mapper.ToDto(booking);
    }

    public async Task<BookingDto> UpdateOwnAsync(int id, OperatorReservationUpdateRequestDto request)
    {
        OperatorIdentityContext identity = _operatorIdentityAccessor.GetCurrent();
        Booking existing = await GetOwnedScopedBookingOrThrowAsync(id, identity, "reservation.update");
        _db.Entry(existing).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var updateDto = new BookingDto
        {
            Id = existing.Id,
            RestaurantId = existing.RestaurantId,
            SectionId = request.SectionId,
            TableId = request.TableId,
            Date = request.Date,
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            Seats = request.Seats,
            SpecialRequests = request.SpecialRequests,
            BookingRef = existing.BookingRef,
            EndTime = existing.EndTime,
            IsCancelled = existing.IsCancelled,
            CancelledAt = existing.CancelledAt,
            ConcurrencyToken = existing.ConcurrencyToken,
        };

        await _bookingService.UpdateBookingAsync(id, updateDto);
        Booking? restamped = await _bookingRepository.FindByIdAsync(id);
        if (restamped is null)
        {
            throw new NotFoundException("Reservation not found.");
        }

        restamped.CreatedByOperatorId = identity.OperatorId;
        restamped.CreatedViaChannel = OperatorChannel;
        await _bookingRepository.SaveChangesAsync();

        Booking updated = await GetOwnedScopedBookingOrThrowAsync(id, identity, "reservation.update");
        await WriteAuditAsync(identity, updated.RestaurantId, updated.Id, "reservation.update", "success", null);
        return _mapper.ToDto(updated);
    }

    public async Task<OperatorReservationActionResultDto> CancelOwnAsync(int id)
    {
        OperatorIdentityContext identity = _operatorIdentityAccessor.GetCurrent();
        Booking booking = await GetOwnedScopedBookingOrThrowAsync(id, identity, "reservation.cancel");

        if (!booking.IsCancelled && !booking.CanBeCancelledAt(DateTime.UtcNow))
        {
            await WriteAuditAsync(identity, booking.RestaurantId, booking.Id, "reservation.cancel", "denied", "booking_in_past");
            throw new ConflictException("Cannot cancel a booking that has already passed.");
        }

        if (!booking.IsCancelled)
        {
            booking.IsCancelled = true;
            booking.CancelledAt = DateTime.UtcNow;
            await _bookingRepository.SaveChangesAsync();
            _notificationQueue.EnqueueBookingCancelled(booking, booking.Restaurant?.Name ?? string.Empty);
        }

        await WriteAuditAsync(identity, booking.RestaurantId, booking.Id, "reservation.cancel", "success", null);
        return new OperatorReservationActionResultDto(true, "Reservation cancelled.", _mapper.ToDto(booking));
    }

    public async Task<OperatorReservationActionResultDto> EscalateOwnAsync(int id, OperatorReservationEscalationRequestDto request)
    {
        OperatorIdentityContext identity = _operatorIdentityAccessor.GetCurrent();
        Booking booking = await GetOwnedScopedBookingOrThrowAsync(id, identity, "reservation.escalate");

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            await WriteAuditAsync(identity, booking.RestaurantId, booking.Id, "reservation.escalate", "denied", "missing_reason");
            throw new ValidationException("Escalation reason is required.");
        }

        string reason = request.Reason.Trim();
        if (reason.Length > MaxEscalationReasonLength)
        {
            throw new ValidationException($"Escalation reason must be {MaxEscalationReasonLength} characters or fewer.");
        }

        string restaurantName = booking.Restaurant?.Name ?? string.Empty;
        var notification = new AdminNotification
        {
            RestaurantId = booking.RestaurantId,
            BookingId = booking.Id,
            BookingRef = booking.BookingRef,
            Type = NotificationType.OperatorEscalation,
            CustomerName = booking.CustomerName ?? "Guest",
            BookingDate = booking.Date,
            Seats = booking.Seats,
            RestaurantName = restaurantName,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
        };
        _db.AdminNotifications.Add(notification);
        await _db.SaveChangesAsync();

        try
        {
            bool enqueued = _notificationQueue.EnqueueOperatorEscalation(
                booking,
                restaurantName,
                identity.OperatorIdentifier,
                reason,
                notification.Id);

            if (!enqueued)
            {
                notification.PushError = "Notification queue unavailable.";
                await _db.SaveChangesAsync();
                throw new InvalidOperationException("Failed to enqueue escalation notification.");
            }
        }
        catch
        {
            notification.PushError ??= "Notification queue unavailable.";
            await _db.SaveChangesAsync();
            await WriteAuditAsync(identity, booking.RestaurantId, booking.Id, "reservation.escalate", "failed", "queue_enqueue_failed");
            throw;
        }

        await WriteAuditAsync(identity, booking.RestaurantId, booking.Id, "reservation.escalate", "success", null);
        return new OperatorReservationActionResultDto(true, "Reservation escalated.", _mapper.ToDto(booking));
    }

    private async Task<Booking?> TryGetOwnedScopedBookingAsync(int id, OperatorIdentityContext identity, string action)
    {
        Booking? booking = await _bookingRepository.GetByIdForOperatorAsync(id, identity.OperatorId);
        if (booking is null)
        {
            await WriteAuditAsync(identity, 0, id, action, "denied", "booking_not_owned_or_not_found");
            return null;
        }

        if (!identity.RestaurantIds.Contains(booking.RestaurantId))
        {
            await WriteAuditAsync(identity, booking.RestaurantId, booking.Id, action, "denied", "restaurant_out_of_scope");
            return null;
        }

        return booking;
    }

    private async Task<Booking> GetOwnedScopedBookingOrThrowAsync(int id, OperatorIdentityContext identity, string action)
    {
        Booking? booking = await TryGetOwnedScopedBookingAsync(id, identity, action);
        if (booking is null)
        {
            throw new NotFoundException("Reservation not found.");
        }

        return booking;
    }

    private async Task WriteAuditAsync(
        OperatorIdentityContext identity,
        int restaurantId,
        int? bookingId,
        string action,
        string outcome,
        string? reason)
    {
        _db.OperatorActionAudits.Add(new OperatorActionAudit
        {
            OperatorPrincipalId = identity.OperatorId,
            OperatorPrincipalIdSnapshot = identity.OperatorId,
            OperatorAgentCredentialId = identity.CredentialId,
            RestaurantId = restaurantId == 0 ? identity.RestaurantIds.FirstOrDefault() : restaurantId,
            RestaurantIdSnapshot = restaurantId == 0 ? identity.RestaurantIds.FirstOrDefault() : restaurantId,
            RestaurantNameSnapshot = await ResolveRestaurantNameSnapshotAsync(restaurantId, bookingId),
            BookingId = bookingId,
            Action = action,
            Outcome = outcome,
            Reason = reason,
            CorrelationId = identity.CorrelationId,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
    }

    private static void EnsureRestaurantScope(OperatorIdentityContext identity, int restaurantId)
    {
        if (!identity.RestaurantIds.Contains(restaurantId))
        {
            throw new NotFoundException("Restaurant not found.");
        }
    }

    private async Task<string> ResolveRestaurantNameSnapshotAsync(int restaurantId, int? bookingId)
    {
        if (bookingId.HasValue)
        {
            Booking? booking = await _bookingRepository.GetByIdAsync(bookingId.Value);
            if (!string.IsNullOrWhiteSpace(booking?.Restaurant?.Name))
            {
                return booking.Restaurant.Name;
            }
        }

        if (restaurantId == 0)
        {
            return string.Empty;
        }

        Restaurant? restaurant = await _db.Restaurants.FindAsync(restaurantId);
        return restaurant?.Name ?? string.Empty;
    }
}
