using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Infrastructure.Localization;
using OpenRestoApi.Infrastructure.OpenApi;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/media")]
[Authorize]
[EnableRateLimiting("public")]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public class MediaController(MediaService mediaService) : ControllerBase
{
    private readonly MediaService _mediaService = mediaService;

    private static readonly string[] _allowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long _maxHeroBytes = 5 * 1024 * 1024;
    private const long _maxLocationBytes = 2 * 1024 * 1024;
    private const long _maxMenuBytes = 10 * 1024 * 1024;
    private const string _menuContentType = "application/pdf";

    [HttpPost("hero")]
    [RequestSizeLimit(5 * 1024 * 1024 + 8192)]
    [ProducesResponseType(typeof(UrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadHero(IFormFile file)
    {
        if (!_allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = ApiLocalization.Localize(HttpContext, "Only JPEG, PNG, and WebP images are accepted.") });

        if (file.Length > _maxHeroBytes)
            return BadRequest(new { message = ApiLocalization.Localize(HttpContext, "Hero image must be under 5 MB.") });

        await using Stream stream = file.OpenReadStream();
        string url = await _mediaService.UploadHeroAsync(stream, file.ContentType);
        return Ok(new { url });
    }

    [HttpDelete("hero")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteHero()
    {
        await _mediaService.DeleteHeroAsync();
        return NoContent();
    }

    [HttpPost("location/{id:int}")]
    [RequestSizeLimit(2 * 1024 * 1024 + 8192)]
    [ProducesResponseType(typeof(UrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadLocation(int id, IFormFile file)
    {
        if (!_allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = ApiLocalization.Localize(HttpContext, "Only JPEG, PNG, and WebP images are accepted.") });

        if (file.Length > _maxLocationBytes)
            return BadRequest(new { message = ApiLocalization.Localize(HttpContext, "Location image must be under 2 MB.") });

        await using Stream stream = file.OpenReadStream();
        string? url = await _mediaService.UploadLocationAsync(id, stream, file.ContentType);
        if (url == null) return NotFound();
        return Ok(new { url });
    }

    [HttpDelete("location/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        bool found = await _mediaService.DeleteLocationAsync(id);
        if (!found) return NotFound();
        return NoContent();
    }

    [HttpPost("menu/{id:int}")]
    [RequestSizeLimit(10 * 1024 * 1024 + 8192)]
    [ProducesResponseType(typeof(UrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadMenu(int id, IFormFile file)
    {
        if (file.ContentType != _menuContentType)
            return BadRequest(new { message = ApiLocalization.Localize(HttpContext, "Only PDF menu files are accepted.") });

        if (file.Length > _maxMenuBytes)
            return BadRequest(new { message = ApiLocalization.Localize(HttpContext, "Menu file must be under 10 MB.") });

        await using Stream stream = file.OpenReadStream();
        string? url = await _mediaService.UploadMenuAsync(id, stream);
        if (url == null) return NotFound();
        return Ok(new { url });
    }

    [HttpDelete("menu/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMenu(int id)
    {
        bool found = await _mediaService.DeleteMenuAsync(id);
        if (!found) return NotFound();
        return NoContent();
    }
}
