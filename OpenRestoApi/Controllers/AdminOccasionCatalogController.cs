using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/admin/restaurants/{restaurantId:int}/occasion-catalog")]
[Authorize(Policy = "SuperAdminOnly")]
[Authorize(Policy = "CurrentSuperAdminManagement")]
public sealed class AdminOccasionCatalogController(OccasionCatalogService service) : ControllerBase
{
    private readonly OccasionCatalogService _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OccasionCatalogItemDto>>> List(int restaurantId)
        => Ok(await _service.ListAsync(restaurantId));

    [HttpPost]
    public async Task<ActionResult<OccasionCatalogItemDto>> Create(int restaurantId, [FromBody] UpsertOccasionCatalogItemRequest request)
    {
        OccasionCatalogItemDto created = await _service.CreateAsync(restaurantId, request);
        return CreatedAtAction(nameof(List), new { restaurantId }, created);
    }

    [HttpPut("{itemId:int}")]
    public async Task<ActionResult<OccasionCatalogItemDto>> Update(
        int restaurantId,
        int itemId,
        [FromBody] UpsertOccasionCatalogItemRequest request)
        => Ok(await _service.UpdateAsync(restaurantId, itemId, request));

    [HttpDelete("{itemId:int}")]
    public async Task<IActionResult> Delete(int restaurantId, int itemId)
    {
        await _service.DeleteAsync(restaurantId, itemId);
        return NoContent();
    }
}
