using BettingService.Models.Requests;
using BettingService.Models.Responses;
using BettingService.Services;
using Cog.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WagersController(IWagerService wagerService, ILogger<WagersController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(WagerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateWager(
        [FromBody] CreateWagerRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating wager for customer {CustomerId}, type {WagerType}",
            request.CustomerId, request.WagerType);

        var result = await wagerService.CreateWagerAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error, Detail = result.ErrorCode });

        return CreatedAtAction(nameof(GetWager), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WagerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWager(int id, CancellationToken cancellationToken)
    {
        var result = await wagerService.GetWagerByIdAsync(id, cancellationToken);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WagerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWagers(
        [FromQuery] int customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await wagerService.GetWagersByCustomerAsync(customerId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("graded")]
    [ProducesResponseType(typeof(PagedResult<WagerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGradedWagers(
        [FromQuery] int? agentId = null,
        [FromQuery] int? gameId  = null,
        [FromQuery] int  page     = 1,
        [FromQuery] int  pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await wagerService.GetGradedWagersAsync(agentId, gameId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(PagedResult<WagerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingWagers(
        [FromQuery] int agentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await wagerService.GetPendingWagersByAgentAsync(agentId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelWager(int id, CancellationToken cancellationToken)
    {
        var result = await wagerService.CancelWagerAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound()
                : BadRequest(new ProblemDetails { Title = result.Error });

        return NoContent();
    }
}
