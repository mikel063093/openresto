using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Infrastructure.Localization;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("public")]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public class HoldsController(
    IHoldService holdService,
    IHoldPolicyService holdPolicyService,
    TableAutoAssigner autoAssigner) : ControllerBase
{
    private readonly IHoldService _holdService = holdService;
    private readonly IHoldPolicyService _holdPolicyService = holdPolicyService;
    private readonly TableAutoAssigner _autoAssigner = autoAssigner;

    /// <summary>
    /// Places a temporary hold on a table for a given date.
    /// Returns 409 Conflict if the table is already held by someone else.
    /// When <see cref="PlaceHoldRequest.TableId"/>/<see cref="PlaceHoldRequest.SectionId"/>
    /// are omitted, the server auto-assigns the best available table across all sections.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(HoldResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PlaceHold([FromBody] PlaceHoldRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        bool autoAssign = request.TableId is null && request.SectionId is null;
        if (request.TableId is null ^ request.SectionId is null)
        {
            return BadRequest(new MessageResponse
            {
                Message = ApiLocalization.Localize(HttpContext, "Specify both TableId and SectionId, or omit both for auto-assign.")
            });
        }

        HoldPolicyResult policy = autoAssign
            ? await _holdPolicyService.ValidateAnyTableAsync(request.RestaurantId, request.Date)
            : await _holdPolicyService.ValidateAsync(request.RestaurantId, request.TableId!.Value, request.Date);

        return policy.Status switch
        {
            HoldPolicyStatus.NotFound => NotFound(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, "Restaurant not found.") }),
            HoldPolicyStatus.Rejected => BadRequest(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, policy.FailureMessage!) }),
            HoldPolicyStatus.Booked => Conflict(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, policy.FailureMessage!) }),
            _ => autoAssign
                ? await PlaceAutoAssignedHold(request, policy)
                : PlaceEligibleHold(request, policy)
        };
    }

    private IActionResult PlaceEligibleHold(PlaceHoldRequest request, HoldPolicyResult policy)
    {
        HoldResult? result = _holdService.PlaceHold(
            request.RestaurantId,
            request.TableId!.Value,
            request.SectionId!.Value,
            policy.BookingDate,
            request.CurrentHoldId,
            policy.Restaurant!.DefaultBookingDurationMinutes);

        if (result == null)
        {
            return Conflict(new MessageResponse { Message = ApiLocalization.Localize(HttpContext, "This table is already held by another user. Please select a different table or try again shortly.") });
        }

        return Ok(new HoldResponse
        {
            HoldId = result.HoldId,
            ExpiresAt = result.ExpiresAt
        });
    }

    private async Task<IActionResult> PlaceAutoAssignedHold(PlaceHoldRequest request, HoldPolicyResult policy)
    {
        if (request.Seats <= 0)
        {
            return BadRequest(new MessageResponse
            {
                Message = ApiLocalization.Localize(HttpContext, "Seats is required for auto-assign so the server can pick a table that fits your party.")
            });
        }

        IReadOnlyList<TableCandidate> candidates = await _autoAssigner.BuildCandidatesAsync(
            policy.Restaurant!, request.Seats, policy.BookingDate);

        if (candidates.Count == 0)
        {
            return Conflict(new MessageResponse
            {
                Message = ApiLocalization.Localize(HttpContext, "No tables are available for the requested time and party size.")
            });
        }

        AutoAssignResult? result = _holdService.PlaceAutoHold(
            request.RestaurantId,
            candidates,
            policy.BookingDate,
            request.CurrentHoldId,
            policy.Restaurant!.DefaultBookingDurationMinutes);

        if (result == null)
        {
            return Conflict(new MessageResponse
            {
                Message = ApiLocalization.Localize(HttpContext, "All suitable tables are currently being held by other users. Please try again shortly.")
            });
        }

        return Ok(new HoldResponse
        {
            HoldId = result.HoldId,
            ExpiresAt = result.ExpiresAt,
            TableId = result.TableId,
            SectionId = result.SectionId
        });
    }

    /// <summary>
    /// Releases a hold early (e.g., when the user navigates away).
    /// Safe to call even if the hold has already expired.
    /// </summary>
    [HttpDelete("{holdId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ReleaseHold(string holdId)
    {
        _holdService.ReleaseHold(holdId);
        return NoContent();
    }
}
