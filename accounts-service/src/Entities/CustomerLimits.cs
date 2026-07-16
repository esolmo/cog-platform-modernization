namespace AccountsService.Entities;

/// <summary>
/// Per-customer wager limits — migrated from Customer table fields.
/// Hard limits and max wager profiles enforced before wager creation.
/// </summary>
public class CustomerLimits
{
    public int     CustomerId          { get; set; }

    // Sport betting limits
    public decimal MaxStraightWager    { get; set; }
    public decimal MaxParlayWager      { get; set; }
    public decimal MaxParlayPayout     { get; set; }
    public decimal MaxTeaserWager      { get; set; }
    public decimal MaxIfBetWager       { get; set; }
    public int     MaxParlayLegs       { get; set; } = 8;
    public int     MinimumWager        { get; set; } = 5;

    // Lottery limits
    public decimal MaxLotteryPick3     { get; set; }
    public decimal MaxLotteryPick4     { get; set; }

    // Casino limits
    public decimal CasinoWagerLimit    { get; set; }
    public decimal CasinoCreditLimit   { get; set; }

    // Hard credit limit — cannot be exceeded even with temp adjustment
    public decimal HardCreditLimit     { get; set; }

    // Settlement / book balance — replaces legacy SettleFigure column
    public decimal SettleFigure        { get; set; }

    public Customer Customer { get; set; } = null!;
}
