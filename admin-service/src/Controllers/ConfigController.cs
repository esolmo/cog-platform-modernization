using AdminService.Models;
using AdminService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfigController(ISystemConfigService configService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "CanViewConfig")]
    public async Task<ActionResult<IReadOnlyList<SystemConfigDto>>> GetAll(
        [FromQuery] string? category = null, CancellationToken ct = default)
    {
        var configs = await configService.GetAllAsync(category, ct);
        return Ok(configs);
    }

    [HttpGet("{key}")]
    [Authorize(Policy = "CanViewConfig")]
    public async Task<ActionResult<SystemConfigDto>> Get(string key, CancellationToken ct = default)
    {
        var config = await configService.GetAsync(key, ct);
        return config is null ? NotFound() : Ok(config);
    }

    [HttpPut]
    [Authorize(Policy = "CanEditConfig")]
    public async Task<ActionResult<SystemConfigDto>> Upsert(
        [FromBody] UpsertConfigRequest request, CancellationToken ct = default)
    {
        var result = await configService.UpsertAsync(request, GetCurrentUserId(), ct);
        return Ok(result.Value);
    }

    [HttpDelete("{key}")]
    [Authorize(Policy = "CanEditConfig")]
    public async Task<IActionResult> Delete(string key, CancellationToken ct = default)
    {
        var result = await configService.DeleteAsync(key, GetCurrentUserId(), ct);
        if (!result.IsSuccess)
            return NotFound();

        return NoContent();
    }

    [HttpGet("audit")]
    [Authorize(Policy = "CanViewAuditLogs")]
    public async Task<ActionResult<AuditLogPagedResult>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] int? userId = null,
        CancellationToken ct = default)
    {
        var result = await configService.GetAuditLogsAsync(page, pageSize, action, userId, ct);
        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
