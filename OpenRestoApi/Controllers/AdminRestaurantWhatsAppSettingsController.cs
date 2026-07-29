using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/admin/restaurants/{restaurantId:int}/whatsapp-settings")]
[Authorize(Policy = "SuperAdminOnly")]
[Authorize(Policy = "CurrentSuperAdminManagement")]
public sealed class AdminRestaurantWhatsAppSettingsController(
    RestaurantWhatsAppSettingsService service) : ControllerBase
{
    private readonly RestaurantWhatsAppSettingsService _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(RestaurantWhatsAppSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantWhatsAppSettingsDto>> Get(int restaurantId)
        => Ok(await _service.GetAsync(restaurantId));

    [HttpPut]
    [ProducesResponseType(typeof(RestaurantWhatsAppSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RestaurantWhatsAppSettingsDto>> Update(
        int restaurantId,
        [FromBody] UpdateRestaurantWhatsAppSettingsRequestDto request)
        => Ok(await _service.UpdateAsync(restaurantId, request));
}
