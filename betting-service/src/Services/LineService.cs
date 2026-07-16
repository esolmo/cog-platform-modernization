using AutoMapper;
using BettingService.Data;
using BettingService.Models.Requests;
using BettingService.Models.Responses;
using Cog.Domain.Common;
using Cog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Services;

public class LineService(BettingDbContext db, IMapper mapper, ILogger<LineService> logger) : ILineService
{
    public async Task<Result<LineSetResponse>> GetLinesByGamePeriodAsync(int gamePeriodId, CancellationToken ct)
    {
        var lineSet = await db.LineSets
            .FirstOrDefaultAsync(l => l.GamePeriodId == gamePeriodId, ct);

        if (lineSet is null) return Result<LineSetResponse>.Failure("Lines not found", "NOT_FOUND");
        return Result<LineSetResponse>.Success(mapper.Map<LineSetResponse>(lineSet));
    }

    public async Task<Result<LineSetResponse>> SetSpreadAsync(
        int gamePeriodId, SetSpreadRequest request, CancellationToken ct)
    {
        var lineSet = await GetOrCreateLineSet(gamePeriodId, ct);
        lineSet.Spread = request.Spread;
        lineSet.SpreadJuice = request.Juice;
        lineSet.OfferingSpread = true;
        lineSet.LastModified = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Spread set on period {GamePeriodId}: {Spread} ({Juice})", gamePeriodId, request.Spread, request.Juice);
        return Result<LineSetResponse>.Success(mapper.Map<LineSetResponse>(lineSet));
    }

    public async Task<Result<LineSetResponse>> SetMoneyLineAsync(
        int gamePeriodId, SetMoneyLineRequest request, CancellationToken ct)
    {
        var lineSet = await GetOrCreateLineSet(gamePeriodId, ct);
        lineSet.HomeMoneyLine = request.HomeMoneyLine;
        lineSet.AwayMoneyLine = request.AwayMoneyLine;
        lineSet.OfferingMoneyLine = true;
        lineSet.LastModified = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<LineSetResponse>.Success(mapper.Map<LineSetResponse>(lineSet));
    }

    public async Task<Result<LineSetResponse>> SetTotalAsync(
        int gamePeriodId, SetTotalRequest request, CancellationToken ct)
    {
        var lineSet = await GetOrCreateLineSet(gamePeriodId, ct);
        lineSet.Total = request.Total;
        lineSet.OverJuice = request.OverJuice;
        lineSet.UnderJuice = request.UnderJuice;
        lineSet.OfferingTotal = true;
        lineSet.LastModified = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<LineSetResponse>.Success(mapper.Map<LineSetResponse>(lineSet));
    }

    public async Task<Result> ApplyShadeAsync(int gamePeriodId, ApplyShadeRequest request, CancellationToken ct)
    {
        var lineSet = await db.LineSets
            .Include(l => l.Shades)
            .FirstOrDefaultAsync(l => l.GamePeriodId == gamePeriodId, ct);

        if (lineSet is null) return Result.Failure("Line set not found", "NOT_FOUND");

        var shade = lineSet.Shades.FirstOrDefault(s => s.AgentId == request.AgentId);
        if (shade is null)
        {
            shade = new LineShade { LineSetId = lineSet.Id, AgentId = request.AgentId };
            lineSet.Shades.Add(shade);
        }

        shade.SpreadAdjustment = request.SpreadAdjustment;
        shade.HomeMoneyLineAdjustment = request.HomeMoneyLineAdjustment;
        shade.AwayMoneyLineAdjustment = request.AwayMoneyLineAdjustment;
        shade.TotalAdjustment = request.TotalAdjustment;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveShadeAsync(int gamePeriodId, int agentId, CancellationToken ct)
    {
        var shade = await db.LineShades
            .Include(s => s.LineSet)
            .FirstOrDefaultAsync(s => s.LineSet.GamePeriodId == gamePeriodId && s.AgentId == agentId, ct);

        if (shade is null) return Result.Failure("Shade not found", "NOT_FOUND");

        db.LineShades.Remove(shade);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<LineSet> GetOrCreateLineSet(int gamePeriodId, CancellationToken ct)
    {
        var lineSet = await db.LineSets.FirstOrDefaultAsync(l => l.GamePeriodId == gamePeriodId, ct);
        if (lineSet is not null) return lineSet;

        lineSet = new LineSet { GamePeriodId = gamePeriodId };
        db.LineSets.Add(lineSet);
        return lineSet;
    }
}
