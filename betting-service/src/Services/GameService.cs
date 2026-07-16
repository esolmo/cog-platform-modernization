using AutoMapper;
using BettingService.Data;
using BettingService.Models.Requests;
using BettingService.Models.Responses;
using Cog.Domain.Common;
using Cog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettingService.Services;

public class GameService(BettingDbContext db, IMapper mapper, ILogger<GameService> logger) : IGameService
{
    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<PagedResult<GameResponse>> GetGamesAsync(
        int? sportTypeId, DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct)
    {
        var query = db.Games
            .Include(g => g.SportType)
            .Include(g => g.Periods).ThenInclude(p => p.LineSet)
            .AsQueryable();

        if (sportTypeId.HasValue)
            query = query.Where(g => g.SportTypeId == sportTypeId.Value);
        if (dateFrom.HasValue)
            query = query.Where(g => g.GameDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(g => g.GameDate <= dateTo.Value);

        query = query.Where(g => g.Status == GameStatus.Upcoming || g.Status == GameStatus.InProgress)
                     .OrderBy(g => g.GameDate);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<GameResponse>
        {
            Items      = mapper.Map<List<GameResponse>>(items),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<Result<GameResponse>> GetGameByIdAsync(int id, CancellationToken ct)
    {
        var game = await db.Games
            .Include(g => g.SportType)
            .Include(g => g.Periods).ThenInclude(p => p.LineSet)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (game is null)
            return Result<GameResponse>.Failure("Game not found.", "NOT_FOUND");

        return Result<GameResponse>.Success(mapper.Map<GameResponse>(game));
    }

    public async Task<IEnumerable<SportTypeResponse>> GetActiveSportTypesAsync(CancellationToken ct)
    {
        var sports = await db.SportTypes
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        return mapper.Map<List<SportTypeResponse>>(sports);
    }

    public async Task<IEnumerable<SportTypeResponse>> GetAllSportTypesAsync(CancellationToken ct)
    {
        var sports = await db.SportTypes
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(ct);

        return mapper.Map<List<SportTypeResponse>>(sports);
    }

    // ── Game mutations ────────────────────────────────────────────────────────

    public async Task<Result<GameResponse>> CreateGameAsync(
        CreateGameRequest request, CancellationToken ct)
    {
        var sportExists = await db.SportTypes.AnyAsync(s => s.Id == request.SportTypeId, ct);
        if (!sportExists)
            return Result<GameResponse>.Failure(
                $"SportType {request.SportTypeId} not found.", "NOT_FOUND");

        var game = new Game
        {
            SportTypeId    = request.SportTypeId,
            HomeTeam       = request.HomeTeam,
            AwayTeam       = request.AwayTeam,
            GameDate       = request.GameDate,
            RotationNumber = request.RotationNumber,
            Status         = GameStatus.Upcoming,
            CreatedAt      = DateTime.UtcNow,
        };

        foreach (var p in request.Periods)
        {
            game.Periods.Add(new GamePeriod
            {
                PeriodDescription = p.PeriodDescription,
                PeriodNumber      = p.PeriodNumber,
            });
        }

        db.Games.Add(game);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Game created: {Id} {Away} @ {Home} on {Date}",
            game.Id, game.AwayTeam, game.HomeTeam, game.GameDate);

        return await GetGameByIdAsync(game.Id, ct);
    }

    public async Task<Result<GameResponse>> UpdateGameAsync(
        int id, UpdateGameRequest request, CancellationToken ct)
    {
        var game = await db.Games.FindAsync([id], ct);
        if (game is null)
            return Result<GameResponse>.Failure("Game not found.", "NOT_FOUND");

        if (game.Status is GameStatus.Final or GameStatus.Cancelled)
            return Result<GameResponse>.Failure(
                "Cannot edit a Final or Cancelled game.", "INVALID_STATUS");

        game.HomeTeam       = request.HomeTeam;
        game.AwayTeam       = request.AwayTeam;
        game.GameDate       = request.GameDate;
        game.RotationNumber = request.RotationNumber;
        game.UpdatedAt      = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Game {Id} updated", id);

        return await GetGameByIdAsync(id, ct);
    }

    public async Task<Result<GameResponse>> UpdateGameStatusAsync(
        int id, UpdateGameStatusRequest request, CancellationToken ct)
    {
        var game = await db.Games.FindAsync([id], ct);
        if (game is null)
            return Result<GameResponse>.Failure("Game not found.", "NOT_FOUND");

        var prev = game.Status;
        game.Status    = request.Status;
        game.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Game {Id} status {Prev} → {New}", id, prev, request.Status);

        return await GetGameByIdAsync(id, ct);
    }

    public async Task<Result> DeleteGameAsync(int id, CancellationToken ct)
    {
        var game = await db.Games.FindAsync([id], ct);
        if (game is null)
            return Result.Failure("Game not found.", "NOT_FOUND");

        if (game.Status == GameStatus.InProgress)
            return Result.Failure("Cannot delete an in-progress game.", "INVALID_STATUS");

        var periodIds = await db.GamePeriods
            .Where(p => p.GameId == id)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var hasWagers = periodIds.Count > 0 &&
            await db.WagerItems.AnyAsync(wi => periodIds.Contains(wi.GamePeriodId), ct);

        if (hasWagers)
            return Result.Failure(
                "Cannot delete a game that has wagers. Cancel it instead.", "HAS_WAGERS");

        db.Games.Remove(game);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Game {Id} deleted", id);

        return Result.Success();
    }

    // ── Period mutations ──────────────────────────────────────────────────────

    public async Task<Result<GamePeriodResponse>> AddPeriodAsync(
        int gameId, CreateGamePeriodRequest request, CancellationToken ct)
    {
        var game = await db.Games.FindAsync([gameId], ct);
        if (game is null)
            return Result<GamePeriodResponse>.Failure("Game not found.", "NOT_FOUND");

        if (game.Status is GameStatus.Final or GameStatus.Cancelled)
            return Result<GamePeriodResponse>.Failure(
                "Cannot add periods to a Final or Cancelled game.", "INVALID_STATUS");

        var period = new GamePeriod
        {
            GameId            = gameId,
            PeriodDescription = request.PeriodDescription,
            PeriodNumber      = request.PeriodNumber,
        };

        db.GamePeriods.Add(period);
        await db.SaveChangesAsync(ct);

        return Result<GamePeriodResponse>.Success(mapper.Map<GamePeriodResponse>(period));
    }

    public async Task<Result> RemovePeriodAsync(int gameId, int periodId, CancellationToken ct)
    {
        var period = await db.GamePeriods
            .FirstOrDefaultAsync(p => p.Id == periodId && p.GameId == gameId, ct);

        if (period is null)
            return Result.Failure("Period not found.", "NOT_FOUND");

        var hasWagers = await db.WagerItems.AnyAsync(wi => wi.GamePeriodId == periodId, ct);
        if (hasWagers)
            return Result.Failure(
                "Cannot remove a period that has wagers.", "HAS_WAGERS");

        db.GamePeriods.Remove(period);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ── Sport type mutations ──────────────────────────────────────────────────

    public async Task<Result<SportTypeResponse>> CreateSportTypeAsync(
        CreateSportTypeRequest request, CancellationToken ct)
    {
        var codeExists = await db.SportTypes
            .AnyAsync(s => s.Code == request.Code.ToUpperInvariant(), ct);

        if (codeExists)
            return Result<SportTypeResponse>.Failure(
                $"SportType code '{request.Code}' already exists.", "DUPLICATE_CODE");

        var sport = new SportType
        {
            Name         = request.Name,
            Code         = request.Code.ToUpperInvariant(),
            IsActive     = true,
            DisplayOrder = request.DisplayOrder,
        };

        db.SportTypes.Add(sport);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("SportType created: {Code} — {Name}", sport.Code, sport.Name);

        return Result<SportTypeResponse>.Success(mapper.Map<SportTypeResponse>(sport));
    }

    public async Task<Result<SportTypeResponse>> UpdateSportTypeAsync(
        int id, UpdateSportTypeRequest request, CancellationToken ct)
    {
        var sport = await db.SportTypes.FindAsync([id], ct);
        if (sport is null)
            return Result<SportTypeResponse>.Failure("SportType not found.", "NOT_FOUND");

        var codeConflict = await db.SportTypes
            .AnyAsync(s => s.Code == request.Code.ToUpperInvariant() && s.Id != id, ct);

        if (codeConflict)
            return Result<SportTypeResponse>.Failure(
                $"SportType code '{request.Code}' already in use.", "DUPLICATE_CODE");

        sport.Name         = request.Name;
        sport.Code         = request.Code.ToUpperInvariant();
        sport.IsActive     = request.IsActive;
        sport.DisplayOrder = request.DisplayOrder;

        await db.SaveChangesAsync(ct);

        return Result<SportTypeResponse>.Success(mapper.Map<SportTypeResponse>(sport));
    }
}
