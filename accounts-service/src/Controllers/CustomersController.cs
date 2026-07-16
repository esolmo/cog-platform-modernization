using AccountsService.Models.Requests;
using AccountsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountsService.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    // ─── Core CRUD ────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await customerService.CreateCustomerAsync(request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "DUPLICATE_LOGIN"                => Conflict(new { result.Error, result.ErrorCode }),
                "AGENT_NOT_FOUND"                => NotFound(new { result.Error, result.ErrorCode }),
                "CREDIT_LIMIT_EXCEEDS_AGENT_MAX" => BadRequest(new { result.Error, result.ErrorCode }),
                _                                => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return CreatedAtAction(nameof(GetCustomer), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCustomer(int id, CancellationToken ct)
    {
        var result = await customerService.GetCustomerByIdAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("by-login/{loginName}")]
    public async Task<IActionResult> GetCustomerByLogin(string loginName, CancellationToken ct)
    {
        var result = await customerService.GetCustomerByLoginAsync(loginName, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("by-agent/{agentId:int}")]
    public async Task<IActionResult> GetCustomersByAgent(
        int agentId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct     = default)
    {
        var result = await customerService.GetCustomersByAgentAsync(agentId, page, pageSize, ct);
        return Ok(result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(
        int id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await customerService.UpdateCustomerAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPut("{id:int}/credit-limits")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> UpdateCreditLimits(
        int id, [FromBody] UpdateCreditLimitRequest request, CancellationToken ct)
    {
        var result = await customerService.UpdateCreditLimitsAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"                      => NotFound(new { result.Error, result.ErrorCode }),
                "CREDIT_LIMIT_EXCEEDS_AGENT_MAX" => BadRequest(new { result.Error, result.ErrorCode }),
                _                                => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return Ok(result.Value);
    }

    [HttpPost("{id:int}/suspend")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> SuspendCustomer(int id, [FromQuery] string updatedBy, CancellationToken ct)
    {
        var result = await customerService.SuspendCustomerAsync(id, updatedBy, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return NoContent();
    }

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> ActivateCustomer(int id, [FromQuery] string updatedBy, CancellationToken ct)
    {
        var result = await customerService.ActivateCustomerAsync(id, updatedBy, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return NoContent();
    }

    // ─── Permissions (replaces BitPermission bitmask) ─────────────────────────

    [HttpGet("{id:int}/permissions")]
    public async Task<IActionResult> GetPermissions(int id, CancellationToken ct)
    {
        var result = await customerService.GetPermissionsAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPut("{id:int}/permissions")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> UpdatePermissions(
        int id, [FromBody] UpdateCustomerPermissionsRequest request, CancellationToken ct)
    {
        var result = await customerService.UpdatePermissionsAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    // ─── Wager limits ─────────────────────────────────────────────────────────

    [HttpPut("{id:int}/wager-limits")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> UpdateWagerLimits(
        int id, [FromBody] UpdateCustomerWagerLimitsRequest request, CancellationToken ct)
    {
        var result = await customerService.UpdateWagerLimitsAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    // ─── Casino limits ────────────────────────────────────────────────────────

    [HttpPut("{id:int}/casino-limits")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> UpdateCasinoLimits(
        int id, [FromBody] UpdateCustomerCasinoLimitsRequest request, CancellationToken ct)
    {
        var result = await customerService.UpdateCasinoLimitsAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    // ─── Settle figure ────────────────────────────────────────────────────────

    [HttpPut("{id:int}/settle-figure")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> SetSettleFigure(
        int id, [FromBody] UpdateCustomerSettleFigureRequest request, CancellationToken ct)
    {
        var result = await customerService.SetSettleFigureAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return NoContent();
    }

    // ─── Agent reassignment ───────────────────────────────────────────────────

    [HttpPost("{id:int}/agent")]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> ReassignAgent(
        int id, [FromBody] ReassignCustomerAgentRequest request, CancellationToken ct)
    {
        var result = await customerService.ReassignAgentAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"                      => NotFound(new { result.Error, result.ErrorCode }),
                "AGENT_NOT_FOUND"                => NotFound(new { result.Error, result.ErrorCode }),
                "CREDIT_LIMIT_EXCEEDS_AGENT_MAX" => BadRequest(new { result.Error, result.ErrorCode }),
                _                                => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return Ok(result.Value);
    }

    // ─── Free play ────────────────────────────────────────────────────────────

    [HttpPost("{id:int}/free-play")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> AwardFreePlay(
        int id, [FromBody] AwardFreePlayRequest request, CancellationToken ct)
    {
        var result = await customerService.AwardFreePlayAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return CreatedAtAction(nameof(GetFreePlayHistory), new { id }, result.Value);
    }

    [HttpGet("{id:int}/free-play")]
    public async Task<IActionResult> GetFreePlayHistory(int id, CancellationToken ct)
    {
        var result = await customerService.GetFreePlayHistoryAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    // ─── Comments ─────────────────────────────────────────────────────────────

    [HttpPost("{id:int}/comments")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> AddComment(
        int id, [FromBody] AddCustomerCommentRequest request, CancellationToken ct)
    {
        var result = await customerService.AddCommentAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return CreatedAtAction(nameof(GetComments), new { id }, result.Value);
    }

    [HttpGet("{id:int}/comments")]
    public async Task<IActionResult> GetComments(
        int id,
        [FromQuery] bool includeCustomerVisible = false,
        CancellationToken ct = default)
    {
        var result = await customerService.GetCommentsAsync(id, includeCustomerVisible, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    // ─── Batch transactions ───────────────────────────────────────────────────

    [HttpPost("batch-transactions")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> BatchTransactions(
        [FromBody] BatchTransactionRequest request, CancellationToken ct)
    {
        var result = await customerService.BatchTransactionAsync(request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "EMPTY_BATCH"           => BadRequest(new { result.Error, result.ErrorCode }),
                "CUSTOMERS_NOT_FOUND"   => NotFound(new { result.Error, result.ErrorCode }),
                "INVALID_TYPE"          => BadRequest(new { result.Error, result.ErrorCode }),
                _                       => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return Ok(new { processed = result.Value });
    }
}
