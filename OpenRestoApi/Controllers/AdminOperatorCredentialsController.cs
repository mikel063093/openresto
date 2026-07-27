using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/admin/operator-credentials")]
[Authorize(Policy = "SuperAdminOnly")]
[Authorize(Policy = "CurrentSuperAdminManagement")]
public sealed class AdminOperatorCredentialsController(
    OperatorCredentialManagementService credentials) : ControllerBase
{
    private readonly OperatorCredentialManagementService _credentials = credentials;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OperatorCredentialListItemDto>>> List() =>
        Ok(await _credentials.ListAsync());

    [HttpPost]
    public async Task<ActionResult<IssueOperatorCredentialResponseDto>> Issue(
        [FromBody] IssueOperatorCredentialRequestDto request)
    {
        IssueOperatorCredentialResponseDto issued = await _credentials.IssueAsync(request);
        return CreatedAtAction(nameof(List), new { credentialId = issued.CredentialId }, issued);
    }

    [HttpPost("{credentialId:int}/revoke")]
    public async Task<IActionResult> Revoke(int credentialId)
    {
        await _credentials.RevokeAsync(credentialId);
        return NoContent();
    }
}
