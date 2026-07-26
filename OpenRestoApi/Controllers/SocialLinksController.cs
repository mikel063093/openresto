using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/social-links")]
[EnableRateLimiting("public")]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public class SocialLinksController(SocialLinkService socialLinkService) : ControllerBase
{
    private readonly SocialLinkService _socialLinks = socialLinkService;

    [HttpGet]
    [ProducesResponseType(typeof(List<SocialLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var list = await _socialLinks.GetAllAsync();
        return Ok(list);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(SocialLinkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateSocialLinkRequest req)
    {
        var dto = await _socialLinks.CreateAsync(req);
        return CreatedAtAction(nameof(GetAll), new { }, dto);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(SocialLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSocialLinkRequest req)
    {
        var dto = await _socialLinks.UpdateAsync(id, req);
        if (dto == null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        bool deleted = await _socialLinks.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
