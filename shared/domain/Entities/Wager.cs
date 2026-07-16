namespace Cog.Domain.Entities;

public class Wager
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int AgentId { get; set; }
    public WagerType WagerType { get; set; }
    public WagerStatus Status { get; set; } = WagerStatus.Pending;
    public decimal RiskAmount { get; set; }
    public decimal WinAmount { get; set; }
    public decimal? ActualPayout { get; set; }
    public string? TicketNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? GradedAt { get; set; }
    public string? GradedBy { get; set; }
    public string? IdempotencyKey { get; set; }

    public Customer Customer { get; set; } = null!;
    public Agent Agent { get; set; } = null!;
    public ICollection<WagerItem> Items { get; set; } = new List<WagerItem>();
}

public enum WagerType
{
    Straight = 1,
    Parlay = 2,
    Teaser = 3,
    IfBet = 4,
    Reverse = 5,
    ActionReverse = 6
}

public enum WagerStatus
{
    Pending = 1,
    Won = 2,
    Lost = 3,
    Push = 4,
    Cancelled = 5,
    NoAction = 6
}
