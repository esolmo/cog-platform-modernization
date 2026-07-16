using Cog.Domain.Entities;

namespace BettingService.Services;

/// <summary>
/// Pure, stateless grading calculations. Made internal so unit tests can reach them directly.
/// </summary>
internal static class WagerGradingEngine
{
    internal static WagerItemStatus GradeItem(
        WagerItemType type, WagerSide side, int homeScore, int awayScore, LineSet? lineSet)
        => type switch
        {
            WagerItemType.Spread    => GradeSpread(side, homeScore, awayScore, lineSet?.Spread ?? 0),
            WagerItemType.MoneyLine => GradeMoneyLine(side, homeScore, awayScore),
            WagerItemType.Total     => GradeTotal(side, homeScore, awayScore, lineSet?.Total ?? 0),
            _                       => WagerItemStatus.NoAction
        };

    // coverMargin > 0 → home covered; < 0 → away covered; = 0 → push
    // spread is stored as the home team's line (negative = home favourite)
    internal static WagerItemStatus GradeSpread(WagerSide side, int homeScore, int awayScore, decimal spread)
    {
        var coverMargin = homeScore - awayScore + spread;
        return side switch
        {
            WagerSide.Home => coverMargin > 0 ? WagerItemStatus.Won
                            : coverMargin < 0 ? WagerItemStatus.Lost
                            : WagerItemStatus.Push,
            WagerSide.Away => coverMargin < 0 ? WagerItemStatus.Won
                            : coverMargin > 0 ? WagerItemStatus.Lost
                            : WagerItemStatus.Push,
            _              => WagerItemStatus.NoAction
        };
    }

    internal static WagerItemStatus GradeMoneyLine(WagerSide side, int homeScore, int awayScore)
    {
        var diff = homeScore - awayScore;
        return side switch
        {
            WagerSide.Home => diff > 0 ? WagerItemStatus.Won : diff < 0 ? WagerItemStatus.Lost : WagerItemStatus.Push,
            WagerSide.Away => diff < 0 ? WagerItemStatus.Won : diff > 0 ? WagerItemStatus.Lost : WagerItemStatus.Push,
            _              => WagerItemStatus.NoAction
        };
    }

    internal static WagerItemStatus GradeTotal(WagerSide side, int homeScore, int awayScore, decimal total)
    {
        var combined = (decimal)(homeScore + awayScore);
        return side switch
        {
            WagerSide.Over  => combined > total ? WagerItemStatus.Won : combined < total ? WagerItemStatus.Lost : WagerItemStatus.Push,
            WagerSide.Under => combined < total ? WagerItemStatus.Won : combined > total ? WagerItemStatus.Lost : WagerItemStatus.Push,
            _               => WagerItemStatus.NoAction
        };
    }

    internal static (WagerStatus Status, decimal? Payout) GradeWager(Wager wager)
        => wager.WagerType switch
        {
            WagerType.Parlay or WagerType.Teaser => GradeParlay(wager),
            _                                     => GradeStraight(wager)
        };

    internal static (WagerStatus, decimal?) GradeStraight(Wager wager)
    {
        var item = wager.Items.First();
        return item.Status switch
        {
            WagerItemStatus.Won  => (WagerStatus.Won,  wager.RiskAmount + wager.WinAmount),
            WagerItemStatus.Push => (WagerStatus.Push, wager.RiskAmount),
            WagerItemStatus.Lost => (WagerStatus.Lost, 0m),
            _                    => (WagerStatus.NoAction, null)
        };
    }

    internal static (WagerStatus, decimal?) GradeParlay(Wager wager)
    {
        var items = wager.Items;

        // Any unresolved item → no-action (shouldn't reach here under normal flow)
        if (items.Any(i => i.Status == WagerItemStatus.Pending))
            return (WagerStatus.NoAction, null);

        // Any loss → parlay lost
        if (items.Any(i => i.Status == WagerItemStatus.Lost))
            return (WagerStatus.Lost, 0m);

        var winningItems = items.Where(i => i.Status == WagerItemStatus.Won).ToList();

        // All legs pushed → return stake
        if (winningItems.Count == 0)
            return (WagerStatus.Push, wager.RiskAmount);

        // Compound payout using American-to-decimal odds conversion
        var oddsProduct = winningItems.Aggregate(1m, (acc, item) =>
        {
            var line = item.LineAtTimeOfWager;
            var decimalOdds = line < 0
                ? (100m / Math.Abs(line)) + 1m
                : (line / 100m) + 1m;
            return acc * decimalOdds;
        });

        return (WagerStatus.Won, Math.Round(wager.RiskAmount * oddsProduct, 2));
    }
}
