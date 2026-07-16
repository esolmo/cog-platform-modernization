using BettingService.Models.Requests;
using BettingService.Models.Responses;
using BettingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LinesController(ILineService lineService) : ControllerBase
{
    [HttpGet("{gamePeriodId:int}")]
    [ProducesResponseType(typeof(LineSetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLines(int gamePeriodId, CancellationToken cancellationToken)
    {
        var result = await lineService.GetLinesByGamePeriodAsync(gamePeriodId, cancellationToken);
        if (!result.IsSuccess) return NotFound();
        return Ok(result.Value);
    }

    [HttpPut("{gamePeriodId:int}/spread")]
    [Authorize(Roles = "LinesManager,Admin")]
    [ProducesResponseType(typeof(LineSetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetSpread(
        int gamePeriodId,
        [FromBody] SetSpreadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lineService.SetSpreadAsync(gamePeriodId, request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{gamePeriodId:int}/moneyline")]
    [Authorize(Roles = "LinesManager,Admin")]
    [ProducesResponseType(typeof(LineSetResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetMoneyLine(
        int gamePeriodId,
        [FromBody] SetMoneyLineRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lineService.SetMoneyLineAsync(gamePeriodId, request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{gamePeriodId:int}/total")]
    [Authorize(Roles = "LinesManager,Admin")]
    [ProducesResponseType(typeof(LineSetResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetTotal(
        int gamePeriodId,
        [FromBody] SetTotalRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lineService.SetTotalAsync(gamePeriodId, request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{gamePeriodId:int}/shade")]
    [Authorize(Roles = "LinesManager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApplyShade(
        int gamePeriodId,
        [FromBody] ApplyShadeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await lineService.ApplyShadeAsync(gamePeriodId, request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Title = result.Error });
        return NoContent();
    }

    [HttpDelete("{gamePeriodId:int}/shade/{agentId:int}")]
    [Authorize(Roles = "LinesManager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveShade(
        int gamePeriodId,
        int agentId,
        CancellationToken cancellationToken)
    {
        var result = await lineService.RemoveShadeAsync(gamePeriodId, agentId, cancellationToken);
        if (!result.IsSuccess) return NotFound();
        return NoContent();
    }
}
