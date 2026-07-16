namespace AccountsService.Entities;

/// <summary>
/// Weekly agent settlement record.
/// Replaces legacy AgentDistribution table populated by AgentDistribution_Calculation SP.
/// One record per agent per week-ending date.
/// </summary>
public class AgentDistribution
{
    public int      Id                  { get; set; }
    public int      AgentId             { get; set; }
    public DateTime WeekEnding          { get; set; }   // Saturday of the week (agent BOW-driven)

    // Sports book figures
    public decimal  WinAmount           { get; set; }   // customer losses = agent revenue
    public decimal  LossAmount          { get; set; }   // customer wins  = agent cost
    public decimal  NetAmount           { get; set; }   // WinAmount - LossAmount

    // Casino figures
    public decimal  CasinoWinAmount     { get; set; }
    public decimal  CasinoLossAmount    { get; set; }
    public decimal  CasinoFeeAmount     { get; set; }   // per-head or % fee to house

    // Live Dealer figures
    public decimal  LiveDealerWin       { get; set; }
    public decimal  LiveDealerLoss      { get; set; }
    public decimal  LiveDealerFee       { get; set; }

    // Adjustments
    public decimal  CreditAdjustments   { get; set; }
    public decimal  DebitAdjustments    { get; set; }

    // Commission calculation
    public CommissionType CommissionType { get; set; }
    public decimal  CommissionRate      { get; set; }   // percentage, e.g. 50.00 = 50 %
    public decimal  CommissionAmount    { get; set; }   // calculated commission

    // Makeup (carryover balance) — replaces GetAgentMakeUp SP
    public decimal  PreviousMakeup      { get; set; }
    public decimal  NewMakeup           { get; set; }   // carries forward to next week if negative

    // Final settlement
    public decimal  HeadCountFee        { get; set; }   // per-active-player fee
    public int      ActivePlayerCount   { get; set; }
    public decimal  NewBalance          { get; set; }   // amount owed to/from agent this week

    public bool     IsConfirmed         { get; set; }   // locked by agent after review
    public DateTime CalculatedAt        { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt        { get; set; }
    public string   CalculatedBy        { get; set; } = string.Empty;

    // Navigation
    public Agent Agent { get; set; } = null!;
}

/// <summary>
/// Commission types from legacy CommissionType table.
/// P = Weekly Profit (agent earns % of net book profit)
/// A = Split (agent and master split revenue/expense)
/// S = Split variant
/// Q = Affiliate Weekly Profit (like P but different makeup handling)
/// T = Red Figure / Settlement (agent pays on negative balance)
/// R = Red Figure variant
/// </summary>
public enum CommissionType
{
    WeeklyProfit       = 1,   // P
    Split              = 2,   // A
    SplitVariant       = 3,   // S
    AffiliateWeekly    = 4,   // Q
    RedFigure          = 5,   // T
    RedFigureVariant   = 6    // R
}
