using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Interfaces;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("public")]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public class AvailabilityController(IAvailabilityService availabilityService) : ControllerBase
{
    private readonly IAvailabilityService _availabilityService = availabilityService;

    [HttpGet("/api/restaurants/{restaurantId}/availability")]
    [ProducesResponseType(typeof(AvailabilityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int restaurantId, [FromQuery] DateTime date, [FromQuery] int seats)
    {
        // NotFoundException (restaurant not found) → 404 and any unexpected exception
        // → 500 are mapped by GlobalExceptionHandler with a { message } body.
        AvailabilityResponseDto result = await _availabilityService.GetAvailabilityAsync(restaurantId, date, seats);
        return Ok(result);
    }
}
