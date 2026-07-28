using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Infrastructure.Auth;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/private/channels/whatsapp")]
[Authorize(AuthenticationSchemes = WhatsAppChannelAuthenticationDefaults.SchemeName)]
public sealed class WhatsAppChannelReservationsController(
    WhatsAppReservationChannelService service) : ControllerBase
{
    private readonly WhatsAppReservationChannelService _service = service;

    [HttpGet("reservations")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> ListReservations()
        => Ok(await _service.ListOwnAsync());

    [HttpGet("reservations/{id:int}")]
    public async Task<IActionResult> GetReservation(int id)
    {
        BookingDto? booking = await _service.GetOwnAsync(id);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPatch("reservations/{id:int}")]
    public async Task<ActionResult<BookingDto>> UpdateReservation(int id, [FromBody] WhatsAppReservationUpdateRequestDto request)
        => Ok(await _service.UpdateOwnAsync(id, request));

    [HttpPost("reservations/{id:int}/cancel")]
    public async Task<ActionResult<OperatorReservationActionResultDto>> CancelReservation(
        int id,
        [FromBody] WhatsAppReservationCancelRequestDto request)
        => Ok(await _service.CancelOwnAsync(id, request));

    [HttpGet("restaurants/{restaurantId:int}/occasion-catalog")]
    public async Task<ActionResult<IReadOnlyList<OccasionCatalogItemDto>>> GetOccasionCatalog(int restaurantId)
        => Ok(await _service.GetActiveOccasionCatalogAsync(restaurantId));
}
