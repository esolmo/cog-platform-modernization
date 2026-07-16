using AdminService.Models;
using AdminService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "CanViewUsers")]
    public async Task<ActionResult<UserPagedResult>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await userService.GetUsersAsync(page, pageSize, search, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanViewUsers")]
    public async Task<ActionResult<UserDto>> GetUser(int id, CancellationToken ct = default)
    {
        var user = await userService.GetUserAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUserRequest request, CancellationToken ct = default)
    {
        var result = await userService.CreateUserAsync(request, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = result.Error, code = result.ErrorCode });

        return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<ActionResult<UserDto>> UpdateUser(
        int id, [FromBody] UpdateUserRequest request, CancellationToken ct = default)
    {
        var result = await userService.UpdateUserAsync(id, request, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND" ? NotFound() : Conflict(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("{id:int}/change-password")]
    public async Task<IActionResult> ChangePassword(
        int id, [FromBody] ChangePasswordRequest request, CancellationToken ct = default)
    {
        // Users can only change their own password unless they're admin
        if (id != GetCurrentUserId() && !User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
            return Forbid();

        var result = await userService.ChangePasswordAsync(id, request, ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND" ? NotFound() : BadRequest(new { error = result.Error });

        return NoContent();
    }

    [HttpPost("{id:int}/reset-password")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> ResetPassword(
        int id, [FromBody] ResetPasswordRequest request, CancellationToken ct = default)
    {
        var result = await userService.ResetPasswordAsync(id, request, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct = default)
    {
        var result = await userService.DeleteUserAsync(id, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id:int}/roles")]
    [Authorize(Policy = "CanManageRoles")]
    public async Task<IActionResult> AssignRoles(
        int id, [FromBody] AssignRoleRequest request, CancellationToken ct = default)
    {
        var result = await userService.AssignRolesAsync(id, request, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return NotFound();

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
