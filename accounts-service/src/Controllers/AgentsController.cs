using AccountsService.Models.Requests;
using AccountsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountsService.Controllers;

[ApiController]
[Route("api/agents")]
[Authorize]
public class AgentsController(IAgentService agentService) : ControllerBase
{
    // ─── Read ─────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> ListAgents(CancellationToken ct)
    {
        var result = await agentService.ListAllAsync(ct);
        return Ok(result.Value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAgent(int id, CancellationToken ct)
    {
        var result = await agentService.GetAgentByIdAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:int}/sub-agents")]
    public async Task<IActionResult> GetSubAgents(int id, CancellationToken ct)
    {
        var result = await agentService.GetSubAgentsAsync(id, ct);
        return Ok(result.Value);
    }

    /// <summary>
    /// Returns the full agent hierarchy tree starting from the given agent.
    /// Replaces the legacy fn_GetSubAgentHierarchyByID recursive SQL function.
    /// </summary>
    [HttpGet("{id:int}/hierarchy")]
    public async Task<IActionResult> GetHierarchy(int id, CancellationToken ct)
    {
        var result = await agentService.GetHierarchyAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:int}/all-sub-agent-ids")]
    public async Task<IActionResult> GetAllSubAgentIds(int id, CancellationToken ct)
    {
        var result = await agentService.GetAllSubAgentIdsAsync(id, ct);
        return Ok(result.Value);
    }

    // ─── Write ────────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> CreateAgent(
        [FromBody] CreateAgentRequest request, CancellationToken ct)
    {
        var result = await agentService.CreateAgentAsync(request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "DUPLICATE_LOGIN"          => Conflict(new { result.Error, result.ErrorCode }),
                "PARENT_NOT_FOUND"         => NotFound(new { result.Error, result.ErrorCode }),
                "CREDIT_LIMIT_EXCEEDS_PARENT" => BadRequest(new { result.Error, result.ErrorCode }),
                _                          => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return CreatedAtAction(nameof(GetAgent), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> UpdateAgent(
        int id, [FromBody] UpdateAgentRequest request, CancellationToken ct)
    {
        var result = await agentService.UpdateAgentAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPut("{id:int}/credit-limits")]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> UpdateCreditLimits(
        int id, [FromBody] UpdateAgentCreditLimitsRequest request, CancellationToken ct)
    {
        var result = await agentService.UpdateCreditLimitsAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"                   => NotFound(new { result.Error, result.ErrorCode }),
                "CREDIT_LIMIT_EXCEEDS_PARENT" => BadRequest(new { result.Error, result.ErrorCode }),
                _                             => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return Ok(result.Value);
    }

    [HttpPost("{id:int}/move")]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> MoveAgent(
        int id, [FromBody] MoveAgentRequest request, CancellationToken ct)
    {
        var result = await agentService.MoveAgentAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"          => NotFound(new { result.Error, result.ErrorCode }),
                "PARENT_NOT_FOUND"   => NotFound(new { result.Error, result.ErrorCode }),
                "CIRCULAR_HIERARCHY" => BadRequest(new { result.Error, result.ErrorCode }),
                _                    => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return Ok(result.Value);
    }

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> DeactivateAgent(
        int id, [FromQuery] string updatedBy, CancellationToken ct)
    {
        var result = await agentService.DeactivateAgentAsync(id, updatedBy, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"             => NotFound(new { result.Error, result.ErrorCode }),
                "HAS_ACTIVE_CUSTOMERS"  => Conflict(new { result.Error, result.ErrorCode }),
                "HAS_ACTIVE_SUBAGENTS"  => Conflict(new { result.Error, result.ErrorCode }),
                _                       => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return NoContent();
    }

    // ─── Position & figures ───────────────────────────────────────────────────

    [HttpGet("position-summary")]
    public async Task<IActionResult> GetPositionSummary(CancellationToken ct)
    {
        var result = await agentService.GetPositionSummaryAsync(ct);
        return Ok(result.Value);
    }

    [HttpGet("{id:int}/position")]
    public async Task<IActionResult> GetPosition(int id, CancellationToken ct)
    {
        var result = await agentService.GetPositionAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });
        return Ok(result.Value);
    }

    [HttpGet("{id:int}/figures")]
    public async Task<IActionResult> GetFigures(
        int id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        // Default to Monday–Sunday of the current week
        var today    = DateTime.UtcNow.Date;
        var dayOfWk  = (int)today.DayOfWeek;
        var monday   = today.AddDays(-(dayOfWk == 0 ? 6 : dayOfWk - 1));
        var fromDate = from?.Date ?? monday;
        var toDate   = to?.Date   ?? today;

        var result = await agentService.GetFiguresAsync(id, fromDate, toDate, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });
        return Ok(result.Value);
    }

    // ─── Settlement ───────────────────────────────────────────────────────────

    [HttpGet("{id:int}/makeup")]
    public async Task<IActionResult> GetMakeup(int id, CancellationToken ct)
    {
        var result = await agentService.GetMakeupAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:int}/distribution/{weekEnding:datetime}")]
    public async Task<IActionResult> GetDistribution(int id, DateTime weekEnding, CancellationToken ct)
    {
        var result = await agentService.GetDistributionAsync(id, weekEnding, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("{id:int}/distribution")]
    public async Task<IActionResult> GetDistributionHistory(
        int id,
        [FromQuery] int weeksBack = 12,
        CancellationToken ct = default)
    {
        var result = await agentService.GetDistributionHistoryAsync(id, weeksBack, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost("{id:int}/distribution/{weekEnding:datetime}/calculate")]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> CalculateDistribution(
        int id, DateTime weekEnding,
        [FromQuery] string calculatedBy,
        CancellationToken ct)
    {
        var result = await agentService.CalculateDistributionAsync(id, weekEnding, calculatedBy, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"        => NotFound(new { result.Error, result.ErrorCode }),
                "ALREADY_CONFIRMED" => Conflict(new { result.Error, result.ErrorCode }),
                _                  => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return Ok(result.Value);
    }

    [HttpPost("{id:int}/distribution/{weekEnding:datetime}/confirm")]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> ConfirmDistribution(
        int id, DateTime weekEnding,
        [FromQuery] string confirmedBy,
        CancellationToken ct)
    {
        var result = await agentService.ConfirmDistributionAsync(id, weekEnding, confirmedBy, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"         => NotFound(new { result.Error, result.ErrorCode }),
                "NOT_CALCULATED"    => BadRequest(new { result.Error, result.ErrorCode }),
                "ALREADY_CONFIRMED" => Conflict(new { result.Error, result.ErrorCode }),
                _                   => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return Ok(result.Value);
    }
}
