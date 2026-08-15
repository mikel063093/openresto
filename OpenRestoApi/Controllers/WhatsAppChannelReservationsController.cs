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

    [HttpGet("restaurants")]
    [ProducesResponseType(typeof(IReadOnlyList<WhatsAppRestaurantListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WhatsAppRestaurantListItemDto>>> ListRestaurants()
        => Ok(await _service.ListAvailableRestaurantsAsync());

    [HttpGet("reservations")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> ListReservations()
        => Ok(await _service.ListOwnAsync());

    [HttpGet("reservations/{id:int}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReservation(int id)
    {
        BookingDto? booking = await _service.GetOwnAsync(id);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPatch("reservations/{id:int}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> UpdateReservation(int id, [FromBody] WhatsAppReservationUpdateRequestDto request)
        => Ok(await _service.UpdateOwnAsync(id, request));

    [HttpPost("reservations")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> CreateReservation([FromBody] WhatsAppReservationCreateRequestDto request)
    {
        ChannelMutationExecutionResult<BookingDto> result = await _service.CreateOwnAsync(request);
        return StatusCode(StatusCodes.Status201Created, result.Result);
    }

    [HttpPost("reservations/{id:int}/cancel")]
    [ProducesResponseType(typeof(OperatorReservationActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OperatorReservationActionResultDto>> CancelReservation(
        int id,
        [FromBody] WhatsAppReservationCancelRequestDto request)
        => Ok(await _service.CancelOwnAsync(id, request));

    [HttpGet("restaurants/{restaurantId:int}/occasion-catalog")]
    [ProducesResponseType(typeof(IReadOnlyList<OccasionCatalogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<OccasionCatalogItemDto>>> GetOccasionCatalog(int restaurantId)
        => Ok(await _service.GetActiveOccasionCatalogAsync(restaurantId));

    [HttpPost("handoffs")]
    [ProducesResponseType(typeof(WhatsAppHandoffResultDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WhatsAppHandoffResultDto>> CreateHandoff([FromBody] WhatsAppHandoffRequestDto request)
    {
        ChannelMutationExecutionResult<WhatsAppHandoffResultDto> result = await _service.CreateHandoffAsync(request);
        return Accepted(result.Result);
    }
}
