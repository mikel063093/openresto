using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Mappings;
using OpenRestoApi.Core.Application.Utilities;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;
using System.Globalization;

namespace OpenRestoApi.Core.Application.Services;

public sealed class WhatsAppReservationChannelService(
    BookingMapper mapper,
    ChannelIdempotencyService channelIdempotencyService,
    WhatsAppChannelIdentityAccessor identityAccessor,
    AppDbContext db)
{
    private const string ChannelName = "whatsapp_private_api";

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
        DateTime updatedDate = await ValidateRequestedUpdateBeforeIdempotencyAsync(booking, request);

        string fingerprint = ComputeFingerprint(
            "update",
            booking.Id.ToString(CultureInfo.InvariantCulture),
            updatedDate.ToString("O"),
            request.Seats.ToString(CultureInfo.InvariantCulture),
            request.ExpectedConcurrencyToken.ToString(CultureInfo.InvariantCulture),
            identity.VerifiedPhoneNormalized);

        ChannelMutationExecutionResult<BookingDto> execution = await _channelIdempotencyService.ExecuteAsync(
            ChannelName,
            $"reservation:update:{booking.Id}",
            request.IdempotencyKey.Trim(),
            fingerprint,
            async () =>
            {
                Booking tracked = await GetOwnedBookingOrThrowAsync(id);
                if (tracked.IsCancelled)
                {
                    throw new ConflictException("Cancelled reservations cannot be changed.");
                }

                ValidateOwnedBookingVersion(tracked, request.ExpectedConcurrencyToken);
                DateTime bookingDate = await ApplyUpdateValidationAsync(tracked, request);

                tracked.Date = bookingDate;
                tracked.Seats = request.Seats;
                tracked.EndTime = bookingDate.AddMinutes(tracked.Restaurant.DefaultBookingDurationMinutes);
                tracked.ConcurrencyToken += 1;

                _phoneOwnershipService.StampVerifiedOwnership(tracked, identity.VerifiedPhoneE164);
                await _db.SaveChangesAsync();

                return _mapper.ToDto(tracked);
            });

        return execution.Result;
    }

    public async Task<OperatorReservationActionResultDto> CancelOwnAsync(int id, WhatsAppReservationCancelRequestDto request)
    {
        ValidateCancelRequest(request);
        _ = await GetOwnedBookingOrThrowAsync(id);

        string fingerprint = ComputeFingerprint(
            "cancel",
            id.ToString(CultureInfo.InvariantCulture),
            request.ExpectedConcurrencyToken.ToString(CultureInfo.InvariantCulture),
            _identityAccessor.GetCurrent().VerifiedPhoneNormalized);

        ChannelMutationExecutionResult<OperatorReservationActionResultDto> execution = await _channelIdempotencyService.ExecuteAsync(
            ChannelName,
            $"reservation:cancel:{id}",
            request.IdempotencyKey.Trim(),
            fingerprint,
            async () =>
            {
                Booking tracked = await GetOwnedBookingOrThrowAsync(id);
                ValidateOwnedBookingVersion(tracked, request.ExpectedConcurrencyToken);

                if (!tracked.IsCancelled && !tracked.CanBeCancelledAt(DateTime.UtcNow))
                {
                    throw new ConflictException("Cannot cancel a booking that has already passed.");
                }

                if (!tracked.IsCancelled)
                {
                    tracked.IsCancelled = true;
                    tracked.CancelledAt = DateTime.UtcNow;
                    tracked.ConcurrencyToken += 1;
                    await _db.SaveChangesAsync();
                }

                return new OperatorReservationActionResultDto(true, "Reservation cancelled.", _mapper.ToDto(tracked));
            });

        return execution.Result;
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

        if (request.ExpectedConcurrencyToken < 0)
        {
            throw new ValidationException("ExpectedConcurrencyToken is required.");
        }

        if (request.Seats <= 0)
        {
            throw new ValidationException("Seats must be greater than zero.");
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

        if (request.ExpectedConcurrencyToken < 0)
        {
            throw new ValidationException("ExpectedConcurrencyToken is required.");
        }

        if (request.Reason is not null)
        {
            throw new ValidationException("Cancel requests do not accept mutable free-form fields.");
        }
    }

    private static void ValidateOwnedBookingVersion(Booking booking, int expectedConcurrencyToken)
    {
        if (booking.ConcurrencyToken != expectedConcurrencyToken)
        {
            throw new ConflictException("The reservation was changed by another writer. Refresh and retry.");
        }
    }

    private Task<DateTime> ValidateRequestedUpdateBeforeIdempotencyAsync(Booking booking, WhatsAppReservationUpdateRequestDto request)
        => ValidateRequestedUpdateAsync(booking, request);

    private Task<DateTime> ApplyUpdateValidationAsync(Booking booking, WhatsAppReservationUpdateRequestDto request)
        => ValidateRequestedUpdateAsync(booking, request);

    private async Task<DateTime> ValidateRequestedUpdateAsync(Booking booking, WhatsAppReservationUpdateRequestDto request)
    {
        Restaurant restaurant = booking.Restaurant;
        if (restaurant.IsPaused())
        {
            throw new ConflictException("Bookings for this restaurant are currently paused. Please try again later.");
        }

        DateTime bookingDate = TimeZoneHelper.ConvertLocalToUtc(request.Date, restaurant.Timezone);
        if (bookingDate < DateTime.UtcNow.AddMinutes(-Booking.CancellationGraceMinutes))
        {
            throw new ConflictException("Cannot create a booking in the past.");
        }

        if (restaurant.IsWalkInOnlyAt(bookingDate))
        {
            throw new ConflictException(restaurant.WalkInOnly
                ? "This location accepts walk-ins only and does not take online bookings."
                : "This location accepts walk-ins only on the selected day. Please choose another day or just come in.");
        }

        if (!restaurant.IsOpenAt(bookingDate))
        {
            throw new ValidationException("The restaurant is closed at the requested time.");
        }

        if (booking.Table is not null && request.Seats > booking.Table.Seats)
        {
            throw new ConflictException(
                $"This table only has {booking.Table.Seats} seats, but {request.Seats} guests were requested.");
        }

        if (booking.Table is not null &&
            restaurant.MaxTableOversizeSeats.HasValue &&
            booking.Table.Seats - request.Seats > restaurant.MaxTableOversizeSeats.Value)
        {
            throw new ConflictException(
                $"This table has {booking.Table.Seats} seats, which is too large for a party of {request.Seats}.");
        }

        DateTime bookingEnd = bookingDate.AddMinutes(restaurant.DefaultBookingDurationMinutes);
        bool hasConflict = booking.TableId.HasValue && await _db.Bookings.AnyAsync(b =>
            b.TableId == booking.TableId &&
            !b.IsCancelled &&
            b.Id != booking.Id &&
            b.Date < bookingEnd &&
            (b.EndTime != null
                ? b.EndTime > bookingDate
                : b.Date.AddMinutes(restaurant.DefaultBookingDurationMinutes) > bookingDate));
        if (hasConflict)
        {
            throw new ConflictException("This table is already booked for that time.");
        }

        return bookingDate;
    }

    private static string ComputeFingerprint(params string[] parts)
    {
        string payload = string.Join("|", parts);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
