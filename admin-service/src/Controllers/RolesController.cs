using AdminService.Models;
using AdminService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CanManageRoles")]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken ct = default)
    {
        var roles = await roleService.GetRolesAsync(ct);
        return Ok(roles);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> GetRole(int id, CancellationToken ct = default)
    {
        var role = await roleService.GetRoleAsync(id, ct);
        return role is null ? NotFound() : Ok(role);
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetPermissions(CancellationToken ct = default)
    {
        var permissions = await roleService.GetPermissionsAsync(ct);
        return Ok(permissions);
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole(
        [FromBody] CreateRoleRequest request, CancellationToken ct = default)
    {
        var result = await roleService.CreateRoleAsync(request, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return Conflict(new { error = result.Error, code = result.ErrorCode });

        return CreatedAtAction(nameof(GetRole), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleDto>> UpdateRole(
        int id, [FromBody] UpdateRoleRequest request, CancellationToken ct = default)
    {
        var result = await roleService.UpdateRoleAsync(id, request, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND" ? NotFound() : BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRole(int id, CancellationToken ct = default)
    {
        var result = await roleService.DeleteRoleAsync(id, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND" ? NotFound() : BadRequest(new { error = result.Error });

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
