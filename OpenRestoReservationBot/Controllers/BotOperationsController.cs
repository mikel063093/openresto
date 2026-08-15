using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoReservationBot.Contracts;
using OpenRestoReservationBot.Security;
using OpenRestoReservationBot.Services;

namespace OpenRestoReservationBot.Controllers;

[ApiController]
[Route("api/internal/reservation-bot")]
[Authorize(AuthenticationSchemes = BotInternalAuthenticationDefaults.SchemeName)]
public sealed class BotOperationsController(IBotOperationRouter router) : ControllerBase
{
    private readonly IBotOperationRouter _router = router;

    [HttpPost("operations")]
    [ProducesResponseType(typeof(BotOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BotOperationResponse>> Execute([FromBody] BotOperationRequest request, CancellationToken cancellationToken)
        => Ok(await _router.ExecuteAsync(request, cancellationToken));
}
