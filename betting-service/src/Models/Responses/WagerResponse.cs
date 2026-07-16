using Cog.Domain.Entities;

namespace BettingService.Models.Responses;

public class WagerResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerLoginName { get; set; } = string.Empty;
    public WagerType WagerType { get; set; }
    public WagerStatus Status { get; set; }
    public decimal RiskAmount { get; set; }
    public decimal WinAmount { get; set; }
    public decimal? ActualPayout { get; set; }
    public string? TicketNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public string? GradedBy { get; set; }
    public List<WagerItemResponse> Items { get; set; } = [];
}

public class WagerItemResponse
{
    public int Id { get; set; }
    public int GamePeriodId { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public string PeriodDescription { get; set; } = string.Empty;
    public WagerItemType ItemType { get; set; }
    public WagerSide Side { get; set; }
    public decimal LineAtTimeOfWager { get; set; }
    public WagerItemStatus Status { get; set; }
}
