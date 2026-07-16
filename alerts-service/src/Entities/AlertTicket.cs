namespace AlertsService.Entities;

/// <summary>
/// Represents a betting action alert ticket (migrated from CRows in COGLib).
/// </summary>
public class AlertTicket
{
    public int Id { get; set; }
    public int WagerNumber { get; set; }
    public int AgentId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerLoginName { get; set; } = string.Empty;
    public string InetWagerNumber { get; set; } = string.Empty;
    public WagerType WagerType { get; set; }
    public AlertType AlertType { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsVipAlert { get; set; }
    public int VipAgentId { get; set; }
    public bool IsSharpAction { get; set; }
    public bool IsSquareAction { get; set; }
    public DateTime InsertedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public ICollection<AlertAttribute> Attributes { get; set; } = [];
    public ICollection<AlertDetail> Details { get; set; } = [];
}

public enum WagerType
{
    Straight = 1,
    Parlay = 2,
    Teaser = 3,
    IfBet = 4,
    Lottery = 5,
    LiveGame = 6,
    Special = 7,
    Horse = 13
}

public enum AlertType
{
    Straight = 1,
    Parlay = 2,
    Feeder = 3,
    ActionLine = 4,
    LiveGame = 6,
    Special = 7,
    Action = 13
}
