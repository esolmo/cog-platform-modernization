namespace Cog.Domain.Entities;

public class WagerItem
{
    public int Id { get; set; }
    public int WagerId { get; set; }
    public int GamePeriodId { get; set; }
    public WagerItemType ItemType { get; set; }
    public WagerSide Side { get; set; }
    public decimal LineAtTimeOfWager { get; set; }
    public WagerItemStatus Status { get; set; } = WagerItemStatus.Pending;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }

    public Wager Wager { get; set; } = null!;
    public GamePeriod GamePeriod { get; set; } = null!;
}

public enum WagerItemType
{
    Spread = 1,
    MoneyLine = 2,
    Total = 3,
    TeamTotal = 4
}

public enum WagerSide
{
    Home = 1,
    Away = 2,
    Over = 3,
    Under = 4
}

public enum WagerItemStatus
{
    Pending = 1,
    Won = 2,
    Lost = 3,
    Push = 4,
    NoAction = 5
}
