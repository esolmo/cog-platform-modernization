using AuthService.Models.Requests;
using AuthService.Models.Responses;
using AuthService.Services;
using Cog.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,MasterAgent")]
    [ProducesResponseType(typeof(PagedResult<UserInfoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await userService.GetUsersAsync(page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int id, CancellationToken ct)
    {
        var result = await userService.GetUserByIdAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,MasterAgent")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        request.CreatedBy = User.FindFirstValue("login_name");
        var result = await userService.CreateUserAsync(request, ct);

        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error });

        return CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("{id:int}/roles/{roleId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(int id, int roleId, CancellationToken ct)
    {
        var assignedBy = User.FindFirstValue("login_name") ?? "system";
        var result     = await userService.AssignRoleAsync(id, roleId, assignedBy, ct);

        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND" || result.ErrorCode == "ROLE_NOT_FOUND"
                ? NotFound()
                : BadRequest(new ProblemDetails { Title = result.Error });

        return NoContent();
    }

    [HttpDelete("{id:int}/roles/{roleId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveRole(int id, int roleId, CancellationToken ct)
    {
        await userService.RemoveRoleAsync(id, roleId, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(int id, CancellationToken ct)
    {
        var result = await userService.DeactivateUserAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        return NoContent();
    }

    [HttpPut("{id:int}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        int id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        // Users can only change their own password unless they are Admin
        var callerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User.FindFirstValue("sub");
        if (!int.TryParse(callerIdClaim, out var callerId)) return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        if (callerId != id && !isAdmin)
            return Forbid();

        var result = await userService.ChangePasswordAsync(id, request, ct);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error, Detail = result.ErrorCode });

        return NoContent();
    }
}
