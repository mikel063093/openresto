using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Mappings;
using OpenRestoApi.Core.Application.Utilities;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Persistence;

namespace OpenRestoApi.Core.Application.Services;

public sealed class WhatsAppReservationChannelService(
    BookingMapper mapper,
    BookingService bookingService,
    ChannelIdempotencyService channelIdempotencyService,
    WhatsAppChannelIdentityAccessor identityAccessor,
    OccasionCatalogService occasionCatalogService,
    AppDbContext db)
{
    private const string ChannelName = "whatsapp_private_api";
    private const int HandoffSummaryMaxLength = AppDbContext.WhatsAppHandoffSummaryMaxLength;

    private readonly BookingMapper _mapper = mapper;
    private readonly BookingService _bookingService = bookingService;
    private readonly ChannelIdempotencyService _channelIdempotencyService = channelIdempotencyService;
    private readonly WhatsAppChannelIdentityAccessor _identityAccessor = identityAccessor;
    private readonly OccasionCatalogService _occasionCatalogService = occasionCatalogService;
    private readonly AppDbContext _db = db;
    private readonly WhatsAppPhoneOwnershipService _phoneOwnershipService = new();

    public async Task<IReadOnlyList<WhatsAppRestaurantListItemDto>> ListAvailableRestaurantsAsync()
    {
        return await _db.Restaurants
            .Where(x => !x.IsArchived && x.IsWhatsAppTestEnabled)
            .OrderBy(x => x.Name)
            .Select(x => new WhatsAppRestaurantListItemDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync();
    }

    public async Task<List<BookingDto>> ListOwnAsync()
    {
        WhatsAppChannelIdentityContext identity = _identityAccessor.GetCurrent();
        List<Booking> bookings = await _db.Bookings
            .Include(x => x.Table)
            .Include(x => x.Section)
            .Include(x => x.Restaurant)
            .Where(x =>
                x.CustomerPhoneNormalized == identity.VerifiedPhoneNormalized &&
                !x.Restaurant.IsArchived &&
                x.Restaurant.IsWhatsAppTestEnabled)
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return bookings.Select(_mapper.ToDto).ToList();
    }

    public async Task<BookingDto?> GetOwnAsync(int id)
    {
        Booking? booking = await TryGetOwnedBookingAsync(id);
        return booking is null ? null : _mapper.ToDto(booking);
    }

    public async Task<ChannelMutationExecutionResult<BookingDto>> CreateOwnAsync(WhatsAppReservationCreateRequestDto request)
    {
        ValidateCreateRequest(request);

        WhatsAppChannelIdentityContext identity = _identityAccessor.GetCurrent();
        Restaurant restaurant = await GetVisibleEnabledRestaurantOrThrowAsync(request.RestaurantId);
        ValidateRestaurantOpenAt(restaurant, request.Date);
        IReadOnlyList<int> requestedCatalogItemIds = (request.OccasionCatalogItemIds ?? []).OrderBy(x => x).ToList();
        _ = await _occasionCatalogService.GetActiveItemsForCreateAsync(restaurant.Id, requestedCatalogItemIds);

        string fingerprint = ComputeFingerprint(
            "create",
            restaurant.Id.ToString(CultureInfo.InvariantCulture),
            TimeZoneHelper.ConvertLocalToUtc(request.Date, restaurant.Timezone).ToString("O"),
            request.Seats.ToString(CultureInfo.InvariantCulture),
            request.CustomerEmail.Trim(),
            request.CustomerName.Trim(),
            request.SpecialRequests?.Trim() ?? string.Empty,
            string.Join(",", requestedCatalogItemIds),
            identity.VerifiedPhoneNormalized);

        return await _channelIdempotencyService.ExecuteAsync(
            ChannelName,
            $"reservation:create:{restaurant.Id}:{identity.VerifiedPhoneNormalized}",
            request.IdempotencyKey.Trim(),
            fingerprint,
            async () =>
            {
                Restaurant trackedRestaurant = await GetVisibleEnabledRestaurantOrThrowAsync(request.RestaurantId);
                ValidateRestaurantOpenAt(trackedRestaurant, request.Date);
                IReadOnlyList<RestaurantOccasionCatalogItem> activeItems = await _occasionCatalogService.GetActiveItemsForCreateAsync(
                    trackedRestaurant.Id,
                    requestedCatalogItemIds);

                BookingDto created = await _bookingService.CreateBookingAsync(new BookingDto
                {
                    RestaurantId = trackedRestaurant.Id,
                    Date = request.Date,
                    Seats = request.Seats,
                    CustomerEmail = request.CustomerEmail.Trim(),
                    CustomerName = request.CustomerName.Trim(),
                    SpecialRequests = string.IsNullOrWhiteSpace(request.SpecialRequests) ? null : request.SpecialRequests.Trim()
                });

                Booking booking = await _db.Bookings
                    .Include(x => x.Table)
                    .Include(x => x.Section)
                    .Include(x => x.Restaurant)
                    .FirstAsync(x => x.Id == created.Id);

                _phoneOwnershipService.StampVerifiedOwnership(booking, identity.VerifiedPhoneE164);
                await _db.SaveChangesAsync();

                if (activeItems.Count > 0)
                {
                    await _occasionCatalogService.CreateSnapshotsAsync(booking.Id, activeItems.Select(x => x.Id).ToList());
                }

                return _mapper.ToDto(booking);
            });
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
                    await _db.SaveChangesAsync();
                }

                return new OperatorReservationActionResultDto(true, "Reservation cancelled.", _mapper.ToDto(tracked));
            });

        return execution.Result;
    }

    public async Task<IReadOnlyList<OccasionCatalogItemDto>> GetActiveOccasionCatalogAsync(int restaurantId)
    {
        _ = await GetVisibleEnabledRestaurantOrThrowAsync(restaurantId);

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

    public async Task<ChannelMutationExecutionResult<WhatsAppHandoffResultDto>> CreateHandoffAsync(WhatsAppHandoffRequestDto request)
    {
        ValidateHandoffRequest(request);
        WhatsAppChannelIdentityContext identity = _identityAccessor.GetCurrent();
        Restaurant restaurant = await GetVisibleEnabledRestaurantOrThrowAsync(request.RestaurantId);

        string fingerprint = ComputeFingerprint(
            "handoff",
            restaurant.Id.ToString(CultureInfo.InvariantCulture),
            request.BookingId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            request.Summary.Trim(),
            identity.VerifiedPhoneNormalized);

        return await _channelIdempotencyService.ExecuteAsync(
            ChannelName,
            $"reservation:handoff:{restaurant.Id}:{identity.VerifiedPhoneNormalized}",
            request.IdempotencyKey.Trim(),
            fingerprint,
            async () =>
            {
                Restaurant trackedRestaurant = await GetVisibleEnabledRestaurantOrThrowAsync(request.RestaurantId);
                Booking? booking = null;
                if (request.BookingId.HasValue)
                {
                    booking = await GetOwnedBookingOrThrowAsync(request.BookingId.Value);
                    if (booking.RestaurantId != trackedRestaurant.Id)
                    {
                        throw new ValidationException("The reservation does not belong to the selected restaurant.");
                    }
                }

                string destination = trackedRestaurant.HandoffWhatsAppE164
                    ?? throw new ConflictException("WhatsApp handoff is not configured for this restaurant.");
                string normalizedDestination = WhatsAppPhoneOwnershipService.Normalize(destination).E164;
                string summarySnapshot = SanitizeSummary(request.Summary);

                var audit = new WhatsAppHandoffAudit
                {
                    RestaurantId = trackedRestaurant.Id,
                    BookingId = booking?.Id,
                    VerifiedPhoneE164 = identity.VerifiedPhoneE164,
                    VerifiedPhoneNormalized = identity.VerifiedPhoneNormalized,
                    SummarySnapshot = summarySnapshot,
                    HandoffDestinationSnapshot = normalizedDestination,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _db.WhatsAppHandoffAudits.Add(audit);
                await _db.SaveChangesAsync();

                return new WhatsAppHandoffResultDto
                {
                    AuditId = audit.Id,
                    RestaurantId = audit.RestaurantId,
                    BookingId = audit.BookingId,
                    HandoffWhatsAppE164 = audit.HandoffDestinationSnapshot,
                    CreatedAtUtc = audit.CreatedAtUtc
                };
            });
    }

    private async Task<Booking?> TryGetOwnedBookingAsync(int id)
    {
        WhatsAppChannelIdentityContext identity = _identityAccessor.GetCurrent();
        return await _db.Bookings
            .Include(x => x.Table)
            .Include(x => x.Section)
            .Include(x => x.Restaurant)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.CustomerPhoneNormalized == identity.VerifiedPhoneNormalized &&
                !x.Restaurant.IsArchived &&
                x.Restaurant.IsWhatsAppTestEnabled);
    }

    private async Task<Booking> GetOwnedBookingOrThrowAsync(int id)
        => await TryGetOwnedBookingAsync(id) ?? throw new NotFoundException("Reservation not found.");

    private async Task<Restaurant> GetVisibleEnabledRestaurantOrThrowAsync(int restaurantId)
    {
        Restaurant restaurant = await _db.Restaurants.FirstOrDefaultAsync(x => x.Id == restaurantId)
            ?? throw new NotFoundException("Restaurant not found.");

        if (restaurant.IsArchived)
        {
            throw new NotFoundException("Restaurant not found.");
        }

        if (!restaurant.IsWhatsAppTestEnabled)
        {
            throw new ConflictException("WhatsApp reservations are disabled for this restaurant.");
        }

        return restaurant;
    }

    private static void ValidateCreateRequest(WhatsAppReservationCreateRequestDto request)
    {
        if (!request.Confirmed)
        {
            throw new ValidationException("Confirmation is required before creating a reservation.");
        }

        if (request.RestaurantId <= 0)
        {
            throw new ValidationException("RestaurantId is required.");
        }

        if (request.Seats <= 0)
        {
            throw new ValidationException("Seats must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ValidationException("IdempotencyKey is required.");
        }

        if (!EmailValidator.IsValid(request.CustomerEmail))
        {
            throw new ValidationException("CustomerEmail is required and must be valid.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new ValidationException("CustomerName is required.");
        }
    }

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

    private static void ValidateHandoffRequest(WhatsAppHandoffRequestDto request)
    {
        if (!request.Confirmed)
        {
            throw new ValidationException("Confirmation is required before creating a handoff.");
        }

        if (request.RestaurantId <= 0)
        {
            throw new ValidationException("RestaurantId is required.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ValidationException("IdempotencyKey is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            throw new ValidationException("Summary is required.");
        }
    }

    private static void ValidateRestaurantOpenAt(Restaurant restaurant, DateTime requestedDate)
    {
        DateTime bookingDate = TimeZoneHelper.ConvertLocalToUtc(requestedDate, restaurant.Timezone);
        if (!restaurant.IsOpenAt(bookingDate))
        {
            throw new ValidationException("The restaurant is closed at the requested time.");
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
        string payload = string.Join("|", parts.Select(x => x.Trim()));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private static string SanitizeSummary(string summary)
    {
        string minimized = string.Join(
            " ",
            summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (minimized.Length <= HandoffSummaryMaxLength)
        {
            return minimized;
        }

        return minimized[..HandoffSummaryMaxLength];
    }
}
