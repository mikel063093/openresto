using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Interfaces;
using OpenRestoApi.Core.Application.Services;
using OpenRestoApi.Infrastructure.Localization;
using OpenRestoApi.Infrastructure.OpenApi;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/admin/auth")]
[EnableRateLimiting("auth")]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public class AuthController(
    IAuthService authService,
    ISecurityQuestionsService securityQuestions,
    IAuthCookieService cookies) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ISecurityQuestionsService _securityQuestions = securityQuestions;
    private readonly IAuthCookieService _cookies = cookies;

    [HttpPost("login")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        string? jwt = await _authService.LoginAsync(req.Email, req.Password);
        if (jwt == null)
            return Unauthorized(new { message = "Invalid email or password." });
        _cookies.SetCookie(Response, jwt);
        return Ok(new { message = "Login successful." });
    }

    [HttpPost("logout")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        _cookies.Clear(Response);
        return Ok(new { message = "Logged out." });
    }

    [HttpGet("me")]
    [Authorize]
    [EnableRateLimiting("public")]
    [ProducesResponseType(typeof(AuthIdentityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        string? email = User.FindFirst(ClaimTypes.Email)?.Value;
        string? role = User.FindFirst(ClaimTypes.Role)?.Value;
        return Ok(new { email, role });
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        // ValidationException (short password) → 400 is mapped by GlobalExceptionHandler.
        string? email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (email == null) return Unauthorized();
        bool ok = await _authService.ChangePasswordAsync(email, req.CurrentPassword, req.NewPassword);
        if (!ok)
            return Unauthorized(new { message = "Current password is incorrect." });
        return Ok(new { message = "Password changed successfully." });
    }

    [HttpPost("change-email")]
    [Authorize]
    [ProducesResponseType(typeof(EmailChangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest req)
    {
        // ValidationException (invalid email) and BusinessRuleException (same email)
        // → 400 are mapped by GlobalExceptionHandler; the BusinessRuleException's
        // message is "New email must be different from the current email.".
        string? email = User.FindFirst(ClaimTypes.Email)?.Value;
        if (email == null) return Unauthorized();
        string? jwt = await _authService.ChangeEmailAsync(email, req.CurrentPassword, req.NewEmail ?? string.Empty);
        if (jwt == null)
            return Unauthorized(new { message = "Current password is incorrect." });
        _cookies.SetCookie(Response, jwt);
        return Ok(new { message = "Email changed successfully.", email = req.NewEmail!.Trim().ToLowerInvariant() });
    }

    [HttpGet("pvq")]
    [ProducesResponseType(typeof(PvqStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPvqStatus()
    {
        return Ok(await _securityQuestions.GetStatusAsync());
    }

    [HttpPost("pvq/setup")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetupPvq([FromBody] SetupPvqRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Question) || string.IsNullOrWhiteSpace(req.Answer))
            return BadRequest(new { message = "Question and answer are required." });

        await _securityQuestions.SetupAsync(req.Question, req.Answer);
        return Ok(new { message = "Security question configured." });
    }

    [HttpPost("pvq/verify")]
    [ProducesResponseType(typeof(PvqVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyPvq([FromBody] VerifyPvqRequest req)
    {
        PvqVerifyOutcome outcome = await _securityQuestions.VerifyAsync(req.Email, req.Answer);
        return outcome.Status switch
        {
            PvqVerifyStatus.NotConfigured => BadRequest(new { message = "Security question not configured for this account." }),
            PvqVerifyStatus.WrongAnswer => Unauthorized(new { message = "Incorrect answer." }),
            _ => Ok(new { resetToken = outcome.ResetToken })
        };
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        // ValidationException (short password) → 400 is mapped by GlobalExceptionHandler.
        bool ok = await _authService.ResetPasswordAsync(req.ResetToken, req.NewPassword);
        if (!ok)
            return BadRequest(new { message = "Invalid or expired reset token." });
        return Ok(new { message = "Password reset successfully." });
    }
}
