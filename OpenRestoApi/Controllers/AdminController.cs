using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Infrastructure.Localization;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController(AdminService adminService) : ControllerBase
{
    public enum bookingStatus { active, cancelled, all, past, upcoming }
    private readonly AdminService _adminService = adminService;

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
        => Ok(await _adminService.GetOverviewAsync());

    [Authorize(Policy = "BookingsRead")]
    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(
        [FromQuery] int? restaurantId,
        [FromQuery] DateTime? date,
        [FromQuery] bookingStatus status = bookingStatus.active,
        [FromQuery] bool cancelled = false,
        [FromQuery] string? email = null,
        [FromQuery] string? bookingRef = null)
    {
        //this is business logic that should likely belong in the service but its OK for now
        string effectiveStatus = cancelled ? nameof(bookingStatus.cancelled) : status.ToString();
        return Ok(await _adminService.GetBookingsAsync(restaurantId, date, effectiveStatus, email, bookingRef));
    }

    [Authorize(Policy = "BookingsRead")]
    [HttpGet("bookings/{id}")]
    public async Task<IActionResult> GetBooking(int id)
    {
        BookingDetailDto? result = await _adminService.GetBookingAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [Authorize(Policy = "BookingsWrite")]
    [HttpPost("bookings")]
    public async Task<IActionResult> CreateBooking([FromBody] AdminCreateBookingRequest req)
    {
        // ValidationException (bad table/section) → 400, ConflictException (overlap/seats) → 409
        // are mapped by GlobalExceptionHandler; the controller just orchestrates.
        BookingDetailDto result = await _adminService.CreateBookingAsync(req);
        return CreatedAtAction(nameof(GetBooking), new { id = result.Id }, result);
    }

    [Authorize(Policy = "BookingsWrite")]
    [HttpPost("bookings/{id}/extend")]
    public async Task<IActionResult> ExtendBooking(int id, [FromBody] ExtendBookingRequest req)
    {
        DateTime? endTime = await _adminService.ExtendBookingAsync(id, req.Minutes);
        return endTime == null ? NotFound() : Ok(new { endTime });
    }

    [Authorize(Policy = "BookingsWrite")]
    [HttpPost("bookings/{id}/cancel")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        // ConflictException (past booking) → 409 is mapped by GlobalExceptionHandler.
        return await _adminService.CancelBookingAsync(id) ? NoContent() : NotFound();
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpDelete("bookings/{id}")]
    public async Task<IActionResult> PurgeBooking(int id)
        => await _adminService.PurgeBookingAsync(id) ? NoContent() : NotFound();

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPost("restaurants")]
    public async Task<IActionResult> CreateRestaurant([FromBody] CreateRestaurantRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, "Name is required.") });
        }

        RestaurantDto result = await _adminService.CreateRestaurantAsync(req.Name, req.Address);
        return CreatedAtAction(nameof(Overview), new { }, result);
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPatch("restaurants/{id}")]
    public async Task<IActionResult> PatchRestaurant(int id, [FromBody] AdminRestaurantPatchRequest req)
    {
        if (req.IsArchived.HasValue)
        {
            bool success = await _adminService.SetArchivedAsync(id, req.IsArchived.Value);
            if (!success) return NotFound();
        }
        return NoContent();
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpDelete("restaurants/{id}")]
    public async Task<IActionResult> DeleteRestaurant(int id)
        => await _adminService.DeleteRestaurantAsync(id) ? NoContent() : NotFound();

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPost("restaurants/{id}/pause")]
    public async Task<IActionResult> PauseBookings(int id, [FromBody] PauseRestaurantRequest req)
    {
        bool success = await _adminService.PauseRestaurantBookingsAsync(id, req.Minutes);
        return success
            ? Ok(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, "Bookings paused successfully.") })
            : NotFound();
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPost("restaurants/{id}/unpause")]
    public async Task<IActionResult> UnpauseBookings(int id)
    {
        bool success = await _adminService.UnpauseRestaurantBookingsAsync(id);
        return success
            ? Ok(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, "Bookings unpaused successfully.") })
            : NotFound();
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPost("restaurants/{id}/extend")]
    public async Task<IActionResult> ExtendBookings(int id, [FromBody] ExtendRestaurantRequest req)
    {
        List<BookingDetailDto>? extendedBookings = await _adminService.ExtendAllActiveBookingsAsync(id, req.Minutes);
        return extendedBookings != null
            ? Ok(new
            {
                Message = ApiLocalization.Localize(HttpContext, "Bookings extended successfully."),
                ExtendedBookings = extendedBookings
            })
            : NotFound();
    }

    [Authorize(Policy = "BookingsRead")]
    [HttpGet("restaurants")]
    public async Task<IActionResult> GetRestaurants()
    {
        List<LookupDto> restaurants = await _adminService.GetRestaurantsAsync();
        return Ok(restaurants);
    }

    [Authorize(Policy = "BookingsRead")]
    [HttpGet("restaurants/{restaurantId}/sections")]
    public async Task<IActionResult> GetSections(int restaurantId)
    {
        List<LookupDto> sections = await _adminService.GetSectionsAsync(restaurantId);
        return Ok(sections);
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPatch("restaurants/{id}/sections/reorder")]
    public async Task<IActionResult> ReorderSections(int id, [FromBody] ReorderSectionsRequest req)
    {
        bool? result = await _adminService.ReorderSectionsAsync(id, req.SectionIds);
        return result switch
        {
            null => NotFound(),
            false => BadRequest(new MessageResponse
            {
                Message = ApiLocalization.Localize(HttpContext, "sectionIds must include exactly the restaurant's current sections, with no duplicates.")
            }),
            true => NoContent(),
        };
    }

    [Authorize(Policy = "BookingsRead")]
    [HttpGet("restaurants/{restaurantId}/tables")]
    public async Task<IActionResult> GetTables(int restaurantId)
    {
        List<SectionDto>? result = await _adminService.GetTablesAsync(restaurantId);
        return result == null
            ? NotFound(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, "Restaurant not found or has no sections.") })
            : Ok(result);
    }

    [Authorize(Policy = "SuperAdminOnly")]
    [HttpPost("bookings/{id}/email")]
    public async Task<IActionResult> SendEmail(int id, [FromBody] SendBookingEmailRequest req)
    {
        // Intentionally keeps its catch: SMTP/transport failures (SmtpException,
        // InfrastructureException, etc.) surface here from the email stack and are
        // wrapped as a user-facing 400 "Failed to send: ..." rather than a 500 —
        // this is a deliberate UX choice, not something GlobalExceptionHandler should
        // own (those failures are not domain exceptions).
        try
        {
            SendBookingEmailResult result = await _adminService.SendBookingEmailAsync(id, req);
            return result.Status switch
            {
                SendBookingEmailStatus.NotFound => NotFound(),
                SendBookingEmailStatus.MissingFields => BadRequest(new MessageResponse
                {
                    Message = ApiLocalization.Localize(HttpContext, "Subject and body are required.")
                }),
                SendBookingEmailStatus.NoCustomerEmail => BadRequest(new MessageResponse
                {
                    Message = ApiLocalization.Localize(HttpContext, "Customer email is not available.")
                }),
                _ => Ok(new MessageResponse
                {
                    Message = ApiLocalization.Localize(HttpContext, $"Email sent to {result.Recipient}.")
                })
            };
        }
        catch (Exception ex)
        {
            return BadRequest(new MessageResponse
            {
                Message = ApiLocalization.Localize(HttpContext, $"Failed to send: {ex.Message}")
            });
        }
    }

    [Authorize(Policy = "BookingsWrite")]
    [HttpPost("bookings/{id}/restore")]
    public async Task<IActionResult> RestoreBooking(int id)
    {
        // BusinessRuleException (booking already active) → 400 is mapped by GlobalExceptionHandler.
        BookingDetailDto? result = await _adminService.RestoreBookingAsync(id);
        return result == null
            ? NotFound()
            : Ok(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, "Booking restored successfully.") });
    }

    [Authorize(Policy = "BookingsWrite")]
    [HttpPut("bookings/{id}")]
    public async Task<IActionResult> AdminUpdateBooking(int id, [FromBody] AdminUpdateBookingRequest req)
    {
        // ValidationException (bad restaurant/table) and BusinessRuleException
        // (update-conflict / seats) → 400 are mapped by GlobalExceptionHandler.
        BookingDetailDto? result = await _adminService.AdminUpdateBookingAsync(id, req);
        return result == null ? NotFound() : Ok(result);
    }
}
