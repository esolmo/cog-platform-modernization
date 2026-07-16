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
public class GamesController(IGameService gameService, IWagerService wagerService) : ControllerBase
{
    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GameResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGames(
        [FromQuery] int?      sportTypeId = null,
        [FromQuery] DateTime? dateFrom    = null,
        [FromQuery] DateTime? dateTo      = null,
        [FromQuery] int       page        = 1,
        [FromQuery] int       pageSize    = 50,
        CancellationToken     ct          = default)
    {
        var result = await gameService.GetGamesAsync(sportTypeId, dateFrom, dateTo, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame(int id, CancellationToken ct)
    {
        var result = await gameService.GetGameByIdAsync(id, ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return Ok(result.Value);
    }

    [HttpGet("sports")]
    [ProducesResponseType(typeof(IEnumerable<SportTypeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSportTypes(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var sports = includeInactive
            ? await gameService.GetAllSportTypesAsync(ct)
            : await gameService.GetActiveSportTypesAsync(ct);
        return Ok(sports);
    }

    // ── Game mutations ────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Admin,LinesManager")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGame(
        [FromBody] CreateGameRequest request, CancellationToken ct)
    {
        var result = await gameService.CreateGameAsync(request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "NOT_FOUND"
                ? NotFound(new { result.Error })
                : BadRequest(new { result.Error });
        }
        return CreatedAtAction(nameof(GetGame), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,LinesManager")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateGame(
        int id, [FromBody] UpdateGameRequest request, CancellationToken ct)
    {
        var result = await gameService.UpdateGameAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"      => NotFound(new { result.Error }),
                "INVALID_STATUS" => UnprocessableEntity(new { result.Error }),
                _                => BadRequest(new { result.Error })
            };
        }
        return Ok(result.Value);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,LinesManager")]
    [ProducesResponseType(typeof(GameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGameStatus(
        int id, [FromBody] UpdateGameStatusRequest request, CancellationToken ct)
    {
        var result = await gameService.UpdateGameStatusAsync(id, request, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteGame(int id, CancellationToken ct)
    {
        var result = await gameService.DeleteGameAsync(id, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"      => NotFound(new { result.Error }),
                "INVALID_STATUS" => UnprocessableEntity(new { result.Error }),
                "HAS_WAGERS"     => UnprocessableEntity(new { result.Error }),
                _                => BadRequest(new { result.Error })
            };
        }
        return NoContent();
    }

    // ── Period management ─────────────────────────────────────────────────────

    [HttpPost("{id:int}/periods")]
    [Authorize(Roles = "Admin,LinesManager")]
    [ProducesResponseType(typeof(GamePeriodResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPeriod(
        int id, [FromBody] CreateGamePeriodRequest request, CancellationToken ct)
    {
        var result = await gameService.AddPeriodAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"      => NotFound(new { result.Error }),
                "INVALID_STATUS" => UnprocessableEntity(new { result.Error }),
                _                => BadRequest(new { result.Error })
            };
        }
        return CreatedAtAction(nameof(GetGame), new { id }, result.Value);
    }

    [HttpDelete("{id:int}/periods/{periodId:int}")]
    [Authorize(Roles = "Admin,LinesManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePeriod(int id, int periodId, CancellationToken ct)
    {
        var result = await gameService.RemovePeriodAsync(id, periodId, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"  => NotFound(new { result.Error }),
                "HAS_WAGERS" => UnprocessableEntity(new { result.Error }),
                _            => BadRequest(new { result.Error })
            };
        }
        return NoContent();
    }

    // ── Grading ───────────────────────────────────────────────────────────────

    [HttpPost("{id:int}/grade")]
    [Authorize(Roles = "Admin,LinesManager")]
    [ProducesResponseType(typeof(GradeGameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GradeGame(
        int id, [FromBody] GradeGameRequest request, CancellationToken ct)
    {
        var result = await wagerService.GradeGameAsync(id, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"      => NotFound(new { result.Error }),
                "ALREADY_GRADED" => Conflict(new { result.Error }),
                _                => BadRequest(new ProblemDetails { Title = result.Error, Detail = result.ErrorCode })
            };
        }
        return Ok(result.Value);
    }

    // ── Sport type management ─────────────────────────────────────────────────

    [HttpPost("sports")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SportTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSportType(
        [FromBody] CreateSportTypeRequest request, CancellationToken ct)
    {
        var result = await gameService.CreateSportTypeAsync(request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "DUPLICATE_CODE"
                ? Conflict(new { result.Error })
                : BadRequest(new { result.Error });
        }
        return CreatedAtAction(nameof(GetSportTypes), result.Value);
    }

    [HttpPut("sports/{sportId:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SportTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSportType(
        int sportId, [FromBody] UpdateSportTypeRequest request, CancellationToken ct)
    {
        var result = await gameService.UpdateSportTypeAsync(sportId, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"      => NotFound(new { result.Error }),
                "DUPLICATE_CODE" => Conflict(new { result.Error }),
                _                => BadRequest(new { result.Error })
            };
        }
        return Ok(result.Value);
    }
}
