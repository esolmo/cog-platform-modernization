using LotteryService.Data;
using LotteryService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotteryService.Controllers;

[ApiController]
[Route("api/lottery/games")]
[Authorize]
public class GamesController(LotteryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetGames(CancellationToken ct)
    {
        var games = await db.LotteryGames
            .Where(g => g.IsActive)
            .Select(g => new LotteryGameDto(g.Id, g.Name, g.GameType, g.IsActive))
            .ToListAsync(ct);
        return Ok(games);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetGame(int id, CancellationToken ct)
    {
        var game = await db.LotteryGames.FindAsync([id], ct);
        if (game is null) return NotFound();
        return Ok(new LotteryGameDto(game.Id, game.Name, game.GameType, game.IsActive));
    }

    [HttpGet("{id:int}/drawings")]
    public async Task<IActionResult> GetDrawings(int id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var drawings = await db.DrawingDetails
            .Include(d => d.LotteryGame)
            .Where(d => d.LotteryGameId == id && d.IsActive && d.DrawingDate > now)
            .OrderBy(d => d.DrawingDate)
            .Select(d => new DrawingDetailDto(
                d.Id, d.LotteryGameId, d.LotteryGame.Name, d.Name,
                d.DrawingDate, d.TimeZoneId, d.MinutesToDraw))
            .ToListAsync(ct);
        return Ok(drawings);
    }
}
