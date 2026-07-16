namespace LotteryService.Entities;

public class LotteryTicket
{
    public long Id { get; set; }
    public long DrawingDetailId { get; set; }
    public int CustomerId { get; set; }
    public int AgentId { get; set; }
    public DateTime DateToPlay { get; set; }
    public DateTime EventDate { get; set; }
    public decimal Total { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public DrawingDetail DrawingDetail { get; set; } = null!;
    public ICollection<LotteryPickEntry> Picks { get; set; } = new List<LotteryPickEntry>();
}
