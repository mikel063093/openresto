using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenRestoApi.Core.Application.DTOs;
using OpenRestoApi.Core.Application.Services;

namespace OpenRestoApi.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "SuperAdminOnly")]
public class AdminUsersController(AdminUserService users) : ControllerBase
{
    private readonly AdminUserService _users = users;

    [HttpGet]
    public async Task<ActionResult<List<AdminUserDto>>> GetAll() => Ok(await _users.GetAllAsync());

    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create(CreateAdminUserRequest request)
    {
        AdminUserDto user = await _users.CreateAsync(request);
        return CreatedAtAction(nameof(GetAll), new { user.Id }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminUserDto>> Update(int id, UpdateAdminUserRequest request) => Ok(await _users.UpdateAsync(id, request));

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _users.DeactivateAsync(id);
        return NoContent();
    }
}
