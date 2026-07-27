using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Infrastructure.Auth;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/internal/operators")]
[Authorize(AuthenticationSchemes = OperatorAuthenticationDefaults.SchemeName)]
[EnableRateLimiting("operatorMcp")]
public sealed class OperatorReservationsController(
    OperatorAvailabilityService operatorAvailabilityService,
    OperatorReservationService operatorReservationService) : ControllerBase
{
    private readonly OperatorAvailabilityService _operatorAvailabilityService = operatorAvailabilityService;
    private readonly OperatorReservationService _operatorReservationService = operatorReservationService;

    [HttpGet("restaurants/{restaurantId}/availability")]
    public async Task<IActionResult> GetAvailability(int restaurantId, [FromQuery] DateTime date, [FromQuery] int seats)
        => Ok(await _operatorAvailabilityService.GetAvailabilityAsync(restaurantId, date, seats));

    [HttpPost("restaurants/{restaurantId}/reservations")]
    public async Task<IActionResult> CreateReservation(int restaurantId, [FromBody] OperatorReservationCreateRequestDto bookingDto)
    {
        BookingDto created = await _operatorReservationService.CreateAsync(restaurantId, bookingDto);
        return CreatedAtAction(nameof(GetReservation), new { id = created.Id }, created);
    }

    [HttpGet("reservations")]
    public async Task<IActionResult> ListReservations()
        => Ok(await _operatorReservationService.ListOwnAsync());

    [HttpGet("reservations/{id}")]
    public async Task<IActionResult> GetReservation(int id)
    {
        BookingDto? booking = await _operatorReservationService.GetOwnAsync(id);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPut("reservations/{id}")]
    public async Task<IActionResult> UpdateReservation(int id, [FromBody] OperatorReservationUpdateRequestDto request)
        => Ok(await _operatorReservationService.UpdateOwnAsync(id, request));

    [HttpPost("reservations/{id}/cancel")]
    public async Task<IActionResult> CancelReservation(int id)
    {
        await _operatorReservationService.CancelOwnAsync(id);
        return NoContent();
    }

    [HttpPost("reservations/{id}/escalate")]
    public async Task<IActionResult> EscalateReservation(int id, [FromBody] OperatorReservationEscalationRequestDto request)
        => Ok(await _operatorReservationService.EscalateOwnAsync(id, request));
}
