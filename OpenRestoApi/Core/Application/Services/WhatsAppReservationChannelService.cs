using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Mappings;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;
using System.Globalization;

namespace OpenRestoApi.Core.Application.Services;

public sealed class WhatsAppReservationChannelService(
    BookingService bookingService,
    BookingMapper mapper,
    ChannelIdempotencyService channelIdempotencyService,
    WhatsAppChannelIdentityAccessor identityAccessor,
    AppDbContext db)
{
    private const string ChannelName = "whatsapp_private_api";

    private readonly BookingService _bookingService = bookingService;
    private readonly BookingMapper _mapper = mapper;
    private readonly ChannelIdempotencyService _channelIdempotencyService = channelIdempotencyService;
    private readonly WhatsAppChannelIdentityAccessor _identityAccessor = identityAccessor;
    private readonly AppDbContext _db = db;
    private readonly WhatsAppPhoneOwnershipService _phoneOwnershipService = new();

    public async Task<List<BookingDto>> ListOwnAsync()
    {
        WhatsAppChannelIdentityContext identity = _identityAccessor.GetCurrent();
        List<Booking> bookings = await _db.Bookings
            .Include(x => x.Table)
            .Include(x => x.Section)
            .Include(x => x.Restaurant)
            .Where(x => x.CustomerPhoneNormalized == identity.VerifiedPhoneNormalized)
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return bookings.Select(_mapper.ToDto).ToList();
    }

    public async Task<BookingDto?> GetOwnAsync(int id)
    {
        Booking? booking = await TryGetOwnedBookingAsync(id);
        return booking is null ? null : _mapper.ToDto(booking);
    }

    public async Task<BookingDto> UpdateOwnAsync(int id, WhatsAppReservationUpdateRequestDto request)
    {
        ValidateUpdateRequest(request);
        WhatsAppChannelIdentityContext identity = _identityAccessor.GetCurrent();
        Booking booking = await GetOwnedBookingOrThrowAsync(id);
        if (booking.IsCancelled)
        {
            throw new ConflictException("Cancelled reservations cannot be changed.");
        }

        string fingerprint = ComputeFingerprint(
            "update",
            booking.Id.ToString(CultureInfo.InvariantCulture),
            request.Date.ToUniversalTime().ToString("O"),
            request.Seats.ToString(CultureInfo.InvariantCulture),
            identity.VerifiedPhoneNormalized);

        ChannelMutationRegistrationResult registration = await _channelIdempotencyService.RegisterMutationAsync(
            ChannelName,
            $"reservation:update:{booking.Id}",
            request.IdempotencyKey.Trim(),
            fingerprint);

        if (registration.Outcome == ChannelMutationRegistrationOutcome.Replayed)
        {
            Booking replayed = await GetOwnedBookingOrThrowAsync(id);
            return _mapper.ToDto(replayed);
        }

        _db.Entry(booking).State = EntityState.Detached;

        var updateDto = new BookingDto
        {
            Id = booking.Id,
            RestaurantId = booking.RestaurantId,
            SectionId = booking.SectionId,
            TableId = booking.TableId,
            Date = request.Date,
            CustomerEmail = booking.CustomerEmail,
            CustomerName = booking.CustomerName,
            Seats = request.Seats,
            SpecialRequests = booking.SpecialRequests,
            BookingRef = booking.BookingRef,
            EndTime = booking.EndTime,
            IsCancelled = booking.IsCancelled,
            CancelledAt = booking.CancelledAt
        };

        await _bookingService.UpdateBookingAsync(id, updateDto);
        Booking? restamped = await _db.Bookings.FindAsync(id);
        if (restamped is null)
        {
            throw new NotFoundException("Reservation not found.");
        }

        _phoneOwnershipService.StampVerifiedOwnership(restamped, identity.VerifiedPhoneE164);
        await _db.SaveChangesAsync();

        Booking updated = await GetOwnedBookingOrThrowAsync(id);
        return _mapper.ToDto(updated);
    }

    public async Task<OperatorReservationActionResultDto> CancelOwnAsync(int id, WhatsAppReservationCancelRequestDto request)
    {
        ValidateCancelRequest(request);
        Booking booking = await GetOwnedBookingOrThrowAsync(id);

        string fingerprint = ComputeFingerprint(
            "cancel",
            booking.Id.ToString(CultureInfo.InvariantCulture),
            _identityAccessor.GetCurrent().VerifiedPhoneNormalized);

        ChannelMutationRegistrationResult registration = await _channelIdempotencyService.RegisterMutationAsync(
            ChannelName,
            $"reservation:cancel:{booking.Id}",
            request.IdempotencyKey.Trim(),
            fingerprint);

        if (!booking.IsCancelled && !booking.CanBeCancelledAt(DateTime.UtcNow))
        {
            throw new ConflictException("Cannot cancel a booking that has already passed.");
        }

        if (registration.Outcome == ChannelMutationRegistrationOutcome.Registered && !booking.IsCancelled)
        {
            booking.IsCancelled = true;
            booking.CancelledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        Booking cancelled = await GetOwnedBookingOrThrowAsync(id);
        return new OperatorReservationActionResultDto(true, "Reservation cancelled.", _mapper.ToDto(cancelled));
    }

    public async Task<IReadOnlyList<OccasionCatalogItemDto>> GetActiveOccasionCatalogAsync(int restaurantId)
    {
        bool exists = await _db.Restaurants.AnyAsync(x => x.Id == restaurantId && !x.IsArchived);
        if (!exists)
        {
            throw new NotFoundException("Restaurant not found.");
        }

        return await _db.RestaurantOccasionCatalogItems
            .Where(x => x.RestaurantId == restaurantId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new OccasionCatalogItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                EstimatedPriceCop = x.EstimatedPriceCop,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToListAsync();
    }

    private async Task<Booking?> TryGetOwnedBookingAsync(int id)
    {
        WhatsAppChannelIdentityContext identity = _identityAccessor.GetCurrent();
        return await _db.Bookings
            .Include(x => x.Table)
            .Include(x => x.Section)
            .Include(x => x.Restaurant)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerPhoneNormalized == identity.VerifiedPhoneNormalized);
    }

    private async Task<Booking> GetOwnedBookingOrThrowAsync(int id)
        => await TryGetOwnedBookingAsync(id) ?? throw new NotFoundException("Reservation not found.");

    private static void ValidateUpdateRequest(WhatsAppReservationUpdateRequestDto request)
    {
        if (!request.Confirmed)
        {
            throw new ValidationException("Confirmation is required before changing a reservation.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ValidationException("IdempotencyKey is required.");
        }

        if (request.RestaurantId.HasValue || request.SectionId.HasValue || request.TableId.HasValue ||
            request.CustomerEmail is not null || request.CustomerName is not null ||
            request.SpecialRequests is not null || request.OccasionCatalogItemIds is not null)
        {
            throw new ValidationException(
                "Only date, time, and seats can be changed through the WhatsApp reservation channel.");
        }
    }

    private static void ValidateCancelRequest(WhatsAppReservationCancelRequestDto request)
    {
        if (!request.Confirmed)
        {
            throw new ValidationException("Confirmation is required before cancelling a reservation.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ValidationException("IdempotencyKey is required.");
        }

        if (request.Reason is not null)
        {
            throw new ValidationException("Cancel requests do not accept mutable free-form fields.");
        }
    }

    private static string ComputeFingerprint(params string[] parts)
    {
        string payload = string.Join("|", parts);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
