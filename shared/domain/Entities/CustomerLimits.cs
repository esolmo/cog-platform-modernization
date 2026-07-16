namespace Cog.Domain.Entities;

public class CustomerLimits
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal MaxWagerStraight { get; set; }
    public decimal MaxWagerParlay { get; set; }
    public decimal MaxWagerTeaser { get; set; }
    public decimal MaxWagerIfBet { get; set; }
    public decimal MaxWagerReverse { get; set; }
    public decimal MinWager { get; set; }
    public decimal MaxWinPerTicket { get; set; }
    public bool AllowStraight { get; set; } = true;
    public bool AllowParlay { get; set; } = true;
    public bool AllowTeaser { get; set; } = true;
    public bool AllowIfBet { get; set; } = true;
    public bool AllowReverse { get; set; } = true;
    public bool AllowCasino { get; set; } = true;
    public bool AllowLottery { get; set; } = true;

    public Customer Customer { get; set; } = null!;
}
