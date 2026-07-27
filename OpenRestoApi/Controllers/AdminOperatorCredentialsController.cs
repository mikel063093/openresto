using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/admin/operator-credentials")]
[Authorize(Policy = "SuperAdminOnly")]
[Authorize(Policy = "CurrentSuperAdminManagement")]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status500InternalServerError)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class AdminOperatorCredentialsController(
    OperatorCredentialManagementService credentials) : ControllerBase
{
    private readonly OperatorCredentialManagementService _credentials = credentials;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OperatorCredentialListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OperatorCredentialListItemDto>>> List() =>
        Ok(await _credentials.ListAsync());

    [HttpPost]
    [ProducesResponseType(typeof(IssueOperatorCredentialResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IssueOperatorCredentialResponseDto>> Issue(
        [FromBody] IssueOperatorCredentialRequestDto request)
    {
        IssueOperatorCredentialResponseDto issued = await _credentials.IssueAsync(request);
        return CreatedAtAction(nameof(List), new { credentialId = issued.CredentialId }, issued);
    }

    [HttpPost("{credentialId:int}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(int credentialId)
    {
        await _credentials.RevokeAsync(credentialId);
        return NoContent();
    }
}
