using AutoMapper;
using BettingService.Data;
using BettingService.Models.Requests;
using BettingService.Models.Responses;
using Cog.Domain.Common;
using Cog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Services;

public class WagerService(
    BettingDbContext db,
    IMapper mapper,
    ICurrentUser currentUser,
    ILogger<WagerService> logger) : IWagerService
{
    public async Task<Result<WagerResponse>> CreateWagerAsync(
        CreateWagerRequest request, CancellationToken ct)
    {
        // Idempotency check
        if (request.IdempotencyKey is not null)
        {
            var existing = await db.Wagers
                .Include(w => w.Items).ThenInclude(i => i.GamePeriod).ThenInclude(gp => gp.Game)
                .Include(w => w.Customer)
                .FirstOrDefaultAsync(w => w.IdempotencyKey == request.IdempotencyKey, ct);
            if (existing is not null)
                return Result<WagerResponse>.Success(mapper.Map<WagerResponse>(existing));
        }

        var customer = await db.Customers
            .Include(c => c.Limits)
            .Include(c => c.Balance)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer is null)
            return Result<WagerResponse>.Failure("Customer not found", "NOT_FOUND");

        if (customer.Status != CustomerStatus.Active)
            return Result<WagerResponse>.Failure("Customer account is not active", "CUSTOMER_INACTIVE");

        var limitCheck = ValidateWagerLimits(customer, request);
        if (!limitCheck.IsSuccess) return limitCheck;

        // Load game periods and validate lines exist
        var periodIds = request.Items.Select(i => i.GamePeriodId).Distinct().ToList();
        var periods = await db.GamePeriods
            .Include(gp => gp.LineSet)
            .Include(gp => gp.Game)
            .Where(gp => periodIds.Contains(gp.Id))
            .ToListAsync(ct);

        if (periods.Count != periodIds.Count)
            return Result<WagerResponse>.Failure("One or more games not found", "GAME_NOT_FOUND");

        var offlinePeriods = periods.Where(p =>
            p.LineSet is null ||
            p.Game.Status != GameStatus.Upcoming).ToList();
        if (offlinePeriods.Any())
            return Result<WagerResponse>.Failure(
                "One or more games are not available for wagering", "GAME_OFFLINE");

        var winAmount = CalculateWinAmount(request, periods);

        var wager = new Wager
        {
            CustomerId = request.CustomerId,
            AgentId = currentUser.AgentId,
            WagerType = request.WagerType,
            RiskAmount = request.RiskAmount,
            WinAmount = winAmount,
            IdempotencyKey = request.IdempotencyKey,
            Items = request.Items.Select(item =>
            {
                var period = periods.First(p => p.Id == item.GamePeriodId);
                var line = GetLineForItem(period.LineSet!, item);
                return new WagerItem
                {
                    GamePeriodId = item.GamePeriodId,
                    ItemType = item.ItemType,
                    Side = item.Side,
                    LineAtTimeOfWager = line
                };
            }).ToList()
        };

        db.Wagers.Add(wager);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Wager {WagerId} created for customer {CustomerId}", wager.Id, request.CustomerId);

        await db.Entry(wager).Reference(w => w.Customer).LoadAsync(ct);
        return Result<WagerResponse>.Success(mapper.Map<WagerResponse>(wager));
    }

    public async Task<Result<WagerResponse>> GetWagerByIdAsync(int id, CancellationToken ct)
    {
        var wager = await db.Wagers
            .Include(w => w.Customer)
            .Include(w => w.Items).ThenInclude(i => i.GamePeriod).ThenInclude(gp => gp.Game)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (wager is null) return Result<WagerResponse>.Failure("Not found", "NOT_FOUND");
        return Result<WagerResponse>.Success(mapper.Map<WagerResponse>(wager));
    }

    public async Task<PagedResult<WagerResponse>> GetWagersByCustomerAsync(
        int customerId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Wagers
            .Include(w => w.Customer)
            .Include(w => w.Items).ThenInclude(i => i.GamePeriod).ThenInclude(gp => gp.Game)
            .Where(w => w.CustomerId == customerId)
            .OrderByDescending(w => w.CreatedAt);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    public async Task<PagedResult<WagerResponse>> GetPendingWagersByAgentAsync(
        int agentId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Wagers
            .Include(w => w.Customer)
            .Include(w => w.Items).ThenInclude(i => i.GamePeriod).ThenInclude(gp => gp.Game)
            .Where(w => w.AgentId == agentId && w.Status == WagerStatus.Pending)
            .OrderByDescending(w => w.CreatedAt);

        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    public async Task<Result<GradeGameResponse>> GradeGameAsync(
        int gameId, GradeGameRequest request, CancellationToken ct)
    {
        var game = await db.Games
            .Include(g => g.Periods).ThenInclude(p => p.LineSet)
            .FirstOrDefaultAsync(g => g.Id == gameId, ct);

        if (game is null)
            return Result<GradeGameResponse>.Failure("Game not found", "NOT_FOUND");

        if (game.Status == GameStatus.Final)
            return Result<GradeGameResponse>.Failure("Game has already been graded", "ALREADY_GRADED");

        if (game.Status == GameStatus.Cancelled)
            return Result<GradeGameResponse>.Failure("Cannot grade a cancelled game", "INVALID_STATE");

        var validPeriodIds = game.Periods.Select(p => p.Id).ToHashSet();
        var badIds = request.PeriodScores.Where(s => !validPeriodIds.Contains(s.PeriodId)).Select(s => s.PeriodId).ToList();
        if (badIds.Count > 0)
            return Result<GradeGameResponse>.Failure(
                $"Period IDs not in game: {string.Join(", ", badIds)}", "INVALID_PERIOD");

        var scoreMap  = request.PeriodScores.ToDictionary(s => s.PeriodId);
        var periodMap = game.Periods.ToDictionary(p => p.Id);
        var periodIds = game.Periods.Select(p => p.Id).ToList();

        // Load pending items for this game's periods
        var wagerItems = await db.WagerItems
            .Include(i => i.GamePeriod)
            .Where(i => periodIds.Contains(i.GamePeriodId) && i.Status == WagerItemStatus.Pending)
            .ToListAsync(ct);

        // Grade each item that has a score provided
        foreach (var item in wagerItems)
        {
            if (!scoreMap.TryGetValue(item.GamePeriodId, out var score)) continue;
            item.HomeScore = score.HomeScore;
            item.AwayScore = score.AwayScore;
            item.Status    = WagerGradingEngine.GradeItem(
                item.ItemType, item.Side, score.HomeScore, score.AwayScore,
                periodMap[item.GamePeriodId].LineSet);
        }

        // Load parent wagers (include ALL their items — multi-game parlays stay pending if unresolved)
        var wagerIds = wagerItems.Select(i => i.WagerId).Distinct().ToList();
        var wagers = await db.Wagers
            .Include(w => w.Items)
            .Where(w => wagerIds.Contains(w.Id) && w.Status == WagerStatus.Pending)
            .ToListAsync(ct);

        var gradedBy = currentUser.LoginName;
        var gradedAt = DateTime.UtcNow;
        int won = 0, lost = 0, pushed = 0;
        decimal totalPayout = 0;

        foreach (var wager in wagers)
        {
            // Skip if any leg is still pending (multi-game parlay waiting on another game)
            if (wager.Items.Any(i => i.Status == WagerItemStatus.Pending)) continue;

            var (status, payout) = WagerGradingEngine.GradeWager(wager);
            wager.Status      = status;
            wager.ActualPayout = payout;
            wager.GradedAt    = gradedAt;
            wager.GradedBy    = gradedBy;

            totalPayout += payout ?? 0;
            if      (status == WagerStatus.Won)  won++;
            else if (status == WagerStatus.Push) pushed++;
            else                                 lost++;
        }

        game.Status    = GameStatus.Final;
        game.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Game {GameId} graded by {GradedBy}: {Won} won, {Lost} lost, {Pushed} pushed, payout {Payout:C}",
            gameId, gradedBy, won, lost, pushed, totalPayout);

        return Result<GradeGameResponse>.Success(new GradeGameResponse
        {
            GameId       = gameId,
            WagersGraded = won + lost + pushed,
            WagersWon    = won,
            WagersLost   = lost,
            WagersPushed = pushed,
            TotalPayout  = totalPayout
        });
    }

    public async Task<PagedResult<WagerResponse>> GetGradedWagersAsync(
        int? agentId, int? gameId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Wagers
            .Include(w => w.Customer)
            .Include(w => w.Items).ThenInclude(i => i.GamePeriod).ThenInclude(gp => gp.Game)
            .Where(w => w.Status == WagerStatus.Won
                     || w.Status == WagerStatus.Lost
                     || w.Status == WagerStatus.Push);

        if (agentId.HasValue)
            query = query.Where(w => w.AgentId == agentId.Value);

        if (gameId.HasValue)
            query = query.Where(w => w.Items.Any(i => i.GamePeriod.GameId == gameId.Value));

        query = query.OrderByDescending(w => w.GradedAt);
        return await ToPagedResultAsync(query, page, pageSize, ct);
    }

    public async Task<Result> CancelWagerAsync(int id, CancellationToken ct)
    {
        var wager = await db.Wagers.FindAsync([id], ct);
        if (wager is null) return Result.Failure("Not found", "NOT_FOUND");
        if (wager.Status != WagerStatus.Pending)
            return Result.Failure("Only pending wagers can be cancelled", "INVALID_STATE");

        wager.Status = WagerStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static Result<WagerResponse> ValidateWagerLimits(Customer customer, CreateWagerRequest request)
    {
        var limits = customer.Limits;
        if (limits is null) return Result<WagerResponse>.Success(null!); // no limits configured = no restriction

        var max = request.WagerType switch
        {
            WagerType.Straight => limits.MaxWagerStraight,
            WagerType.Parlay => limits.MaxWagerParlay,
            WagerType.Teaser => limits.MaxWagerTeaser,
            WagerType.IfBet => limits.MaxWagerIfBet,
            WagerType.Reverse or WagerType.ActionReverse => limits.MaxWagerReverse,
            _ => 0m
        };

        if (max > 0 && request.RiskAmount > max)
            return Result<WagerResponse>.Failure(
                $"Wager amount exceeds limit of {max}", "LIMIT_EXCEEDED");

        if (request.RiskAmount < limits.MinWager)
            return Result<WagerResponse>.Failure(
                $"Wager amount is below minimum of {limits.MinWager}", "BELOW_MINIMUM");

        return Result<WagerResponse>.Success(null!);
    }

    private static decimal GetLineForItem(LineSet lineSet, WagerItemRequest item) => item.ItemType switch
    {
        WagerItemType.Spread    => lineSet.SpreadJuice ?? -110,
        WagerItemType.MoneyLine => item.Side == WagerSide.Home
            ? lineSet.HomeMoneyLine ?? 0
            : lineSet.AwayMoneyLine ?? 0,
        WagerItemType.Total     => item.Side == WagerSide.Over
            ? lineSet.OverJuice  ?? -110
            : lineSet.UnderJuice ?? -110,
        _ => -110
    };

    private static decimal CalculateWinAmount(CreateWagerRequest request, List<GamePeriod> periods)
    {
        // Straight wager: American odds payout calculation
        if (request.WagerType == WagerType.Straight && request.Items.Count == 1)
        {
            var item = request.Items[0];
            var period = periods.First(p => p.Id == item.GamePeriodId);
            var line = GetLineForItem(period.LineSet!, item);
            return line < 0
                ? request.RiskAmount * 100 / Math.Abs(line)
                : request.RiskAmount * line / 100;
        }

        // Parlay: compound payout (simplified — full calculation to be migrated from Delphi)
        if (request.WagerType == WagerType.Parlay)
            return request.RiskAmount * (decimal)Math.Pow(1.909, request.Items.Count);

        return request.RiskAmount; // default placeholder for other wager types
    }

    private static async Task<PagedResult<WagerResponse>> ToPagedResultAsync(
        IQueryable<Wager> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<WagerResponse>
        {
            Items = items.Select(w => MapToResponse(w)).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static WagerResponse MapToResponse(Wager w) => new()
    {
        Id = w.Id,
        CustomerId = w.CustomerId,
        CustomerLoginName = w.Customer?.LoginName ?? string.Empty,
        WagerType = w.WagerType,
        Status = w.Status,
        RiskAmount = w.RiskAmount,
        WinAmount = w.WinAmount,
        ActualPayout = w.ActualPayout,
        TicketNumber = w.TicketNumber,
        CreatedAt = w.CreatedAt,
        GradedAt = w.GradedAt,
        GradedBy = w.GradedBy,
        Items = w.Items.Select(i => new WagerItemResponse
        {
            Id = i.Id,
            GamePeriodId = i.GamePeriodId,
            HomeTeam = i.GamePeriod?.Game?.HomeTeam ?? string.Empty,
            AwayTeam = i.GamePeriod?.Game?.AwayTeam ?? string.Empty,
            PeriodDescription = i.GamePeriod?.PeriodDescription ?? string.Empty,
            ItemType = i.ItemType,
            Side = i.Side,
            LineAtTimeOfWager = i.LineAtTimeOfWager,
            Status = i.Status
        }).ToList()
    };
}
