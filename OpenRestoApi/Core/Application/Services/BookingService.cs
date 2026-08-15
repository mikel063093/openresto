using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Exceptions;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Mappings;
using OpenRestoApi.Core.Application.Utilities;
using OpenRestoApi.Core.Domain;
using OpenRestoApi.Infrastructure.Localization;

namespace OpenRestoApi.Core.Application.Services;

public class BookingService(
    IBookingRepository bookingRepository,
    ITableRepository tableRepository,
    ISectionRepository sectionRepository,
    IRestaurantRepository restaurantRepository,
    IHoldService holdService,
    BookingMapper mapper,
    TableAutoAssigner autoAssigner,
    IBookingConfirmationService? confirmationService = null,
    INotificationQueue? notificationQueue = null)
{
    private readonly IBookingRepository _bookingRepository = bookingRepository;
    private readonly ITableRepository _tableRepository = tableRepository;
    private readonly ISectionRepository _sectionRepository = sectionRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IHoldService _holdService = holdService;
    private readonly BookingMapper _mapper = mapper;
    private readonly TableAutoAssigner _autoAssigner = autoAssigner;
    private readonly IBookingConfirmationService? _confirmationService = confirmationService;
    private readonly INotificationQueue? _notificationQueue = notificationQueue;

    private sealed record BookingScopeValidationResult(Table Table, Section Section);

    /// <summary>
    /// Creates a booking after validating:
    /// 1. No confirmed booking exists for the same table on the same date.
    /// 2. No other user holds the table for the same date (the submitter's own hold is excluded).
    /// If a holdId is provided and valid, it is released after the booking is persisted.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the table is unavailable.</exception>
    public virtual async Task<BookingDto> CreateBookingAsync(BookingDto bookingDto, string locale = ApiLocalization.English)
    {
        locale = EmailNotificationLocalization.NormalizeLocale(locale);
        // 1. Validate restaurant-level pause first
        Restaurant? restaurant = await _restaurantRepository.GetByIdAsync(bookingDto.RestaurantId);
        if (restaurant == null)
        {
            throw new NotFoundException("Restaurant not found.");
        }

        if (restaurant.IsPaused())
        {
            throw new ConflictException("Bookings for this restaurant are currently paused. Please try again later.");
        }

        // 2. Normalize date: if Unspecified, treat as restaurant local and convert to UTC
        DateTime bookingDate = TimeZoneHelper.ConvertLocalToUtc(bookingDto.Date, restaurant.Timezone);

        // 0. Reject bookings in the past (same 5-min tolerance as booking cancellation).
        if (bookingDate < DateTime.UtcNow.AddMinutes(-Booking.CancellationGraceMinutes))
        {
            throw new ConflictException("Cannot create a booking in the past.");
        }

        // Walk-in-only locations (or walk-in-only days) never take online bookings.
        // Admin-recorded bookings use AdminService.CreateBookingAsync and are unaffected.
        if (restaurant.IsWalkInOnlyAt(bookingDate))
        {
            throw new ConflictException(restaurant.WalkInOnly
                ? "This location accepts walk-ins only and does not take online bookings."
                : "This location accepts walk-ins only on the selected day. Please choose another day or just come in.");
        }

        if (bookingDto.TableId is null || bookingDto.SectionId is null)
        {
            // Exactly one of the two is null — that's ambiguous; reject. Both null means
            // "Any section" auto-assign, which we resolve below before the rest of the
            // method (which assumes concrete ids).
            if (bookingDto.TableId is null ^ bookingDto.SectionId is null)
            {
                throw new ValidationException("Specify both TableId and SectionId, or neither for auto-assign.");
            }

            await ResolveAutoAssignAsync(bookingDto, restaurant, bookingDate);
        }

        // After auto-assign resolution (or an explicit selection) both ids are guaranteed set.
        int tableId = bookingDto.TableId!.Value;
        int sectionId = bookingDto.SectionId!.Value;
        BookingScopeValidationResult scope = await ValidateBookingScopeAsync(bookingDto.RestaurantId, tableId, sectionId);

        // 1. Check DB for an existing confirmed booking on the same table+date
        bool alreadyBooked = await _bookingRepository.IsTableBookedOnDateAsync(
            tableId, bookingDate, restaurant.DefaultBookingDurationMinutes);

        if (alreadyBooked)
        {
            throw new ConflictException("This table is already booked for that time.");
        }

        // 2. Check for an active hold by someone else
        bool heldByOther = _holdService.IsTableHeld(
            tableId, bookingDate, excludeHoldId: bookingDto.HoldId,
            durationMinutes: restaurant.DefaultBookingDurationMinutes);

        if (heldByOther)
        {
            throw new ConflictException("This table is currently being held by another user. Please try again shortly.");
        }

        // 3. Check for seat capacity
        Table? table = scope.Table;
        if (table != null && bookingDto.Seats > table.Seats)
        {
            throw new ConflictException($"This table only has {table.Seats} seats, but {bookingDto.Seats} guests were requested.");
        }

        // 3b. Reject oversized tables when the restaurant caps spare seats. Mirrors the
        // lower bound above and the AvailabilityService eligible-table filter, so a direct
        // POST (or an auto-assigned pick that slipped past the candidate filter) can't seat
        // a small party at a table the restaurant wants held for larger groups.
        if (table != null && restaurant.MaxTableOversizeSeats.HasValue
            && table.Seats - bookingDto.Seats > restaurant.MaxTableOversizeSeats.Value)
        {
            throw new ConflictException(
                $"This table has {table.Seats} seats, which is too large for a party of {bookingDto.Seats}.");
        }

        // 4. Persist the booking
        Booking booking = _mapper.ToEntity(bookingDto);
        booking.Date = bookingDate; // Use normalized date
        booking.BookingRef = BookingRefGenerator.Generate();
        booking.EndTime = bookingDate.AddMinutes(restaurant.DefaultBookingDurationMinutes);
        booking.Table = scope.Table;
        booking.Section = scope.Section;
        booking.Restaurant = restaurant;

        Booking newBooking = await _bookingRepository.AddAsync(booking);

        // 5. Release the hold now that the booking is confirmed
        if (!string.IsNullOrEmpty(bookingDto.HoldId))
        {
            _holdService.ReleaseHold(bookingDto.HoldId);
        }

        // 6. Admin push notification (fire-and-forget via background queue)
        if (_notificationQueue != null)
        {
            _notificationQueue.EnqueueBookingCreated(newBooking, restaurant.Name, locale);
            _notificationQueue.EnqueueCapacityCheck(restaurant.Id, restaurant.Name, newBooking.Date, locale);
        }

        // 7. Send booking confirmation email (best-effort, never fails the booking).
        // Delegated to BookingConfirmationService — BookingService no longer owns template
        // building or SMTP orchestration.
        if (_confirmationService != null)
        {
            await _confirmationService.SendConfirmationAsync(newBooking, restaurant, locale);
        }

        return _mapper.ToDto(newBooking);
    }

    /// <summary>
    /// Resolves a "Any section" auto-assign booking request: populates
    /// <paramref name="bookingDto"/>.TableId/SectionId (and HoldId when a fresh hold is placed)
    /// so the caller's downstream checks and persistence work unchanged. If a valid
    /// <see cref="BookingDto.HoldId"/> was provided, the held table/section are adopted
    /// directly; otherwise the candidate pool is built and a new hold is placed atomically
    /// via <see cref="IHoldService.PlaceAutoHold"/>. Throws <see cref="ConflictException"/>
    /// when no candidate is free.
    /// </summary>
    private async Task ResolveAutoAssignAsync(BookingDto bookingDto, Restaurant restaurant, DateTime bookingDate)
    {
        // 1. If the caller already holds an auto-assigned table, adopt it. This avoids a
        //    second race: the hold was placed atomically, so the held table is "ours" until
        //    the booking lands or the hold expires.
        if (!string.IsNullOrEmpty(bookingDto.HoldId))
        {
            HoldEntry? held = _holdService.GetHold(bookingDto.HoldId);
            if (held is not null && held.RestaurantId == restaurant.Id)
            {
                // The hold is on a specific table — verify it still fits and isn't double-booked
                // in the DB (the hold only guards against other in-memory holds). If something
                // changed under us, fall through to the candidate search.
                bool booked = await _bookingRepository.IsTableBookedOnDateAsync(
                    held.TableId, bookingDate, restaurant.DefaultBookingDurationMinutes);
                if (!booked)
                {
                    bookingDto.TableId = held.TableId;
                    bookingDto.SectionId = held.SectionId;
                    return;
                }
            }
        }

        // 2. No usable hold — build candidates and place a fresh hold atomically. The new
        //    hold id is stashed on the DTO so the existing "release hold after persist" step
        //    at the end of CreateBookingAsync cleans it up.
        IReadOnlyList<TableCandidate> candidates = await _autoAssigner.BuildCandidatesAsync(
            restaurant, bookingDto.Seats, bookingDate);

        if (candidates.Count == 0)
        {
            throw new ConflictException("No tables are available for the requested time and party size.");
        }

        AutoAssignResult? assigned = _holdService.PlaceAutoHold(
            restaurant.Id,
            candidates,
            bookingDate,
            currentHoldId: bookingDto.HoldId,
            restaurant.DefaultBookingDurationMinutes);

        if (assigned is null)
        {
            throw new ConflictException("All suitable tables are currently being held by other users. Please try again shortly.");
        }

        bookingDto.TableId = assigned.TableId;
        bookingDto.SectionId = assigned.SectionId;
        bookingDto.HoldId = assigned.HoldId; // ensure the release-at-end step tears it down
    }

    public virtual async Task<BookingDto?> GetBookingByIdAsync(int id)
    {
        Booking? booking = await _bookingRepository.GetByIdAsync(id);
        return booking == null ? null : _mapper.ToDto(booking);
    }

    public virtual async Task<BookingDto?> GetBookingByRefAsync(string bookingRef)
    {
        Booking? booking = await _bookingRepository.GetByRefAsync(bookingRef);
        return booking == null ? null : _mapper.ToDto(booking);
    }

    public virtual async Task<IEnumerable<BookingDto>> GetBookingsByRestaurantAsync(int restaurantId)
    {
        IEnumerable<Booking> bookings = await _bookingRepository.GetBookingsByRestaurantIdAsync(restaurantId);
        return _mapper.ToDtoList(bookings);
    }

    public virtual async Task UpdateBookingAsync(int id, BookingDto bookingDto)
    {
        _ = id; // Required by REST convention (PUT /bookings/{id}) but entity ID comes from DTO
        if (bookingDto.TableId is null ^ bookingDto.SectionId is null)
        {
            throw new ValidationException("Specify both TableId and SectionId, or neither.");
        }

        BookingScopeValidationResult? scope = null;
        if (bookingDto.TableId.HasValue && bookingDto.SectionId.HasValue)
        {
            scope = await ValidateBookingScopeAsync(bookingDto.RestaurantId, bookingDto.TableId.Value, bookingDto.SectionId.Value);
        }

        Booking booking = _mapper.ToEntity(bookingDto);
        Restaurant? restaurant = await _restaurantRepository.GetByIdAsync(booking.RestaurantId);
        if (restaurant is null)
        {
            throw new NotFoundException("Restaurant not found.");
        }

        DateTime bookingDate = TimeZoneHelper.ConvertLocalToUtc(bookingDto.Date, restaurant.Timezone);
        if (restaurant.IsPaused())
        {
            throw new ConflictException("Bookings for this restaurant are currently paused. Please try again later.");
        }

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

        // Check for seat capacity if seats are being updated
        if (bookingDto.Seats > 0)
        {
            Table? table = scope?.Table;
            if (table != null && bookingDto.Seats > table.Seats)
            {
                throw new ConflictException($"This table only has {table.Seats} seats, but {bookingDto.Seats} guests were requested.");
            }

            // Oversize check — mirrors CreateBookingAsync and the availability feed so an edit
            // can't move a small party onto a table the restaurant caps for larger groups.
            if (table != null && restaurant?.MaxTableOversizeSeats.HasValue == true
                && table.Seats - bookingDto.Seats > restaurant.MaxTableOversizeSeats.Value)
            {
                throw new ConflictException(
                    $"This table has {table.Seats} seats, which is too large for a party of {bookingDto.Seats}.");
            }
        }

        // Ensure EndTime is valid if it's being updated or if Date changed
        booking.Date = bookingDate;
        if (!booking.EndTime.HasValue || booking.EndTime.Value < booking.Date)
        {
            booking.EndTime = booking.Date.AddMinutes(restaurant.DefaultBookingDurationMinutes);
        }

        bool hasConflict = await _bookingRepository.HasConflictAsync(
            booking.TableId,
            booking.Date,
            booking.EndTime.Value,
            restaurant.DefaultBookingDurationMinutes,
            excludeBookingId: booking.Id);
        if (hasConflict)
        {
            throw new ConflictException("This table is already booked for that time.");
        }

        await _bookingRepository.UpdateAsync(booking);
    }

    private async Task<BookingScopeValidationResult> ValidateBookingScopeAsync(int restaurantId, int tableId, int sectionId)
    {
        Section? section = await _sectionRepository.GetByIdAsync(sectionId);
        if (section is null)
        {
            throw new ValidationException("Section not found.");
        }

        if (section.RestaurantId != restaurantId)
        {
            throw new ValidationException("Section does not belong to the specified restaurant.");
        }

        Table? table = await _tableRepository.GetByIdAsync(tableId);
        if (table is null)
        {
            throw new ValidationException("Table not found.");
        }

        if (table.SectionId != sectionId)
        {
            throw new ValidationException("Table does not belong to the specified section.");
        }

        return new BookingScopeValidationResult(table, section);
    }

    public virtual async Task DeleteBookingAsync(int id)
    {
        await _bookingRepository.DeleteAsync(id);
    }

    public virtual async Task<string?> GetRestaurantNameAsync(int restaurantId)
    {
        Restaurant? restaurant = await _restaurantRepository.GetByIdAsync(restaurantId);
        return restaurant?.Name;
    }

    public virtual async Task<bool> CancelBookingAsync(string bookingRef, string email, string locale = ApiLocalization.English)
    {
        locale = EmailNotificationLocalization.NormalizeLocale(locale);
        Booking? booking = await _bookingRepository.GetByRefAsync(bookingRef);
        if (booking == null)
        {
            Console.WriteLine($"[CancelBookingAsync] Booking not found for ref: {bookingRef}");
            return false;
        }

        if (!string.Equals(booking.CustomerEmail?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[CancelBookingAsync] Email mismatch for ref: {bookingRef}. DB: {booking.CustomerEmail}, Input: {email}");
            return false;
        }

        if (booking.IsCancelled)
        {
            return true;
        }

        if (!booking.CanBeCancelledAt(DateTime.UtcNow))
        {
            throw new ConflictException("Cannot cancel a booking that has already passed.");
        }

        booking.IsCancelled = true;
        booking.CancelledAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        if (_notificationQueue != null)
        {
            Restaurant? restaurant = await _restaurantRepository.GetByIdAsync(booking.RestaurantId);
            _notificationQueue.EnqueueBookingCancelled(booking, restaurant?.Name ?? "", locale);
        }

        return true;
    }
}
