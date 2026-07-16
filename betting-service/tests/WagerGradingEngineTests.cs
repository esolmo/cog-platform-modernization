using BettingService.Services;
using Cog.Domain.Entities;
using FluentAssertions;

namespace BettingService.Tests;

public class WagerGradingEngineTests
{
    // ── GradeSpread ──────────────────────────────────────────────────────────

    [Fact]
    public void GradeSpread_HomeFavourite_HomeCovers_ReturnsWon()
    {
        // spread = -3.5 → home -3.5 → home must win by more than 3.5
        var result = WagerGradingEngine.GradeSpread(WagerSide.Home, homeScore: 31, awayScore: 24, spread: -3.5m);
        result.Should().Be(WagerItemStatus.Won);
    }

    [Fact]
    public void GradeSpread_HomeFavourite_HomeFailsToCover_ReturnsLost()
    {
        var result = WagerGradingEngine.GradeSpread(WagerSide.Home, homeScore: 27, awayScore: 24, spread: -3.5m);
        result.Should().Be(WagerItemStatus.Lost);
    }

    [Fact]
    public void GradeSpread_HomeFavourite_AwayCovers_ReturnsAwayWon()
    {
        var result = WagerGradingEngine.GradeSpread(WagerSide.Away, homeScore: 27, awayScore: 24, spread: -3.5m);
        result.Should().Be(WagerItemStatus.Won);
    }

    [Fact]
    public void GradeSpread_ExactWholePointSpread_Push()
    {
        // spread = -3 and home wins by exactly 3 → push for both sides
        var resultHome = WagerGradingEngine.GradeSpread(WagerSide.Home, homeScore: 27, awayScore: 24, spread: -3m);
        var resultAway = WagerGradingEngine.GradeSpread(WagerSide.Away, homeScore: 27, awayScore: 24, spread: -3m);
        resultHome.Should().Be(WagerItemStatus.Push);
        resultAway.Should().Be(WagerItemStatus.Push);
    }

    // ── GradeMoneyLine ───────────────────────────────────────────────────────

    [Fact]
    public void GradeMoneyLine_HomeWins_HomeSideWon()
    {
        var result = WagerGradingEngine.GradeMoneyLine(WagerSide.Home, homeScore: 3, awayScore: 1);
        result.Should().Be(WagerItemStatus.Won);
    }

    [Fact]
    public void GradeMoneyLine_HomeWins_AwaySideLost()
    {
        var result = WagerGradingEngine.GradeMoneyLine(WagerSide.Away, homeScore: 3, awayScore: 1);
        result.Should().Be(WagerItemStatus.Lost);
    }

    [Fact]
    public void GradeMoneyLine_AwayWins_AwaySideWon()
    {
        var result = WagerGradingEngine.GradeMoneyLine(WagerSide.Away, homeScore: 1, awayScore: 2);
        result.Should().Be(WagerItemStatus.Won);
    }

    [Fact]
    public void GradeMoneyLine_Tie_ReturnsPush()
    {
        var result = WagerGradingEngine.GradeMoneyLine(WagerSide.Home, homeScore: 1, awayScore: 1);
        result.Should().Be(WagerItemStatus.Push);
    }

    // ── GradeTotal ───────────────────────────────────────────────────────────

    [Fact]
    public void GradeTotal_CombinedExceedsLine_OverWon()
    {
        var result = WagerGradingEngine.GradeTotal(WagerSide.Over, homeScore: 28, awayScore: 24, total: 47.5m);
        result.Should().Be(WagerItemStatus.Won);
    }

    [Fact]
    public void GradeTotal_CombinedBelowLine_UnderWon()
    {
        var result = WagerGradingEngine.GradeTotal(WagerSide.Under, homeScore: 17, awayScore: 14, total: 47.5m);
        result.Should().Be(WagerItemStatus.Won);
    }

    [Fact]
    public void GradeTotal_CombinedExceedsLine_UnderLost()
    {
        var result = WagerGradingEngine.GradeTotal(WagerSide.Under, homeScore: 28, awayScore: 24, total: 47.5m);
        result.Should().Be(WagerItemStatus.Lost);
    }

    [Fact]
    public void GradeTotal_CombinedExactlyOnLine_Push()
    {
        var result = WagerGradingEngine.GradeTotal(WagerSide.Over, homeScore: 24, awayScore: 24, total: 48m);
        result.Should().Be(WagerItemStatus.Push);
    }

    // ── GradeStraight ────────────────────────────────────────────────────────

    [Fact]
    public void GradeStraight_WonItem_ReturnsRiskPlusWin()
    {
        var wager = BuildStraightWager(riskAmount: 100m, winAmount: 90.91m, itemStatus: WagerItemStatus.Won);
        var (status, payout) = WagerGradingEngine.GradeStraight(wager);
        status.Should().Be(WagerStatus.Won);
        payout.Should().BeApproximately(190.91m, 0.01m);
    }

    [Fact]
    public void GradeStraight_LostItem_ReturnsZero()
    {
        var wager = BuildStraightWager(riskAmount: 100m, winAmount: 90.91m, itemStatus: WagerItemStatus.Lost);
        var (status, payout) = WagerGradingEngine.GradeStraight(wager);
        status.Should().Be(WagerStatus.Lost);
        payout.Should().Be(0m);
    }

    [Fact]
    public void GradeStraight_PushedItem_ReturnsStake()
    {
        var wager = BuildStraightWager(riskAmount: 100m, winAmount: 90.91m, itemStatus: WagerItemStatus.Push);
        var (status, payout) = WagerGradingEngine.GradeStraight(wager);
        status.Should().Be(WagerStatus.Push);
        payout.Should().Be(100m);
    }

    // ── GradeParlay ──────────────────────────────────────────────────────────

    [Fact]
    public void GradeParlay_AllLegsWon_ReturnsCompoundPayout()
    {
        // 2-team parlay, both at -110 each
        // Each decimal odds = (100/110) + 1 ≈ 1.9091
        // Product ≈ 3.6447 → payout ≈ 364.47
        var wager = BuildParlayWager(100m, new[]
        {
            (WagerItemStatus.Won, -110m),
            (WagerItemStatus.Won, -110m)
        });
        var (status, payout) = WagerGradingEngine.GradeParlay(wager);
        status.Should().Be(WagerStatus.Won);
        payout.Should().BeApproximately(364.46m, 0.10m);
    }

    [Fact]
    public void GradeParlay_OneLegLost_ReturnsLost()
    {
        var wager = BuildParlayWager(100m, new[]
        {
            (WagerItemStatus.Won,  -110m),
            (WagerItemStatus.Lost, -110m)
        });
        var (status, payout) = WagerGradingEngine.GradeParlay(wager);
        status.Should().Be(WagerStatus.Lost);
        payout.Should().Be(0m);
    }

    [Fact]
    public void GradeParlay_OneLegPushesOtherWins_ReducesToSingleLeg()
    {
        // Push leg excluded → parlay reduces to single leg at -110
        // Payout = 100 × ((100/110) + 1) ≈ 190.91
        var wager = BuildParlayWager(100m, new[]
        {
            (WagerItemStatus.Won,  -110m),
            (WagerItemStatus.Push, -110m)
        });
        var (status, payout) = WagerGradingEngine.GradeParlay(wager);
        status.Should().Be(WagerStatus.Won);
        payout.Should().BeApproximately(190.91m, 0.01m);
    }

    [Fact]
    public void GradeParlay_AllLegsPush_ReturnsPushWithStake()
    {
        var wager = BuildParlayWager(100m, new[]
        {
            (WagerItemStatus.Push, -110m),
            (WagerItemStatus.Push, -110m)
        });
        var (status, payout) = WagerGradingEngine.GradeParlay(wager);
        status.Should().Be(WagerStatus.Push);
        payout.Should().Be(100m);
    }

    [Fact]
    public void GradeParlay_PlusOddsLeg_ConvertsCorrectly()
    {
        // +150 leg → decimal = (150/100) + 1 = 2.5 → payout = 100 × 2.5 = 250
        var wager = BuildParlayWager(100m, new[]
        {
            (WagerItemStatus.Won, 150m)
        });
        var (status, payout) = WagerGradingEngine.GradeParlay(wager);
        status.Should().Be(WagerStatus.Won);
        payout.Should().BeApproximately(250m, 0.01m);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Wager BuildStraightWager(decimal riskAmount, decimal winAmount, WagerItemStatus itemStatus)
        => new()
        {
            WagerType  = WagerType.Straight,
            RiskAmount = riskAmount,
            WinAmount  = winAmount,
            Items = new List<WagerItem>
            {
                new()
                {
                    Status            = itemStatus,
                    LineAtTimeOfWager = -110m,
                    ItemType          = WagerItemType.Spread,
                    Side              = WagerSide.Home
                }
            }
        };

    private static Wager BuildParlayWager(decimal riskAmount, (WagerItemStatus Status, decimal Line)[] legs)
        => new()
        {
            WagerType  = WagerType.Parlay,
            RiskAmount = riskAmount,
            WinAmount  = 0m,
            Items      = legs.Select((l, _) => new WagerItem
            {
                Status            = l.Status,
                LineAtTimeOfWager = l.Line,
                ItemType          = WagerItemType.Spread,
                Side              = WagerSide.Home
            }).ToList()
        };
}
