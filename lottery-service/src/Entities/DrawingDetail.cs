namespace LotteryService.Entities;

public class DrawingDetail
{
    public long Id { get; set; }
    public int LotteryGameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DrawingDate { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public int MinutesToDraw { get; set; }
    public bool IsActive { get; set; } = true;

    public LotteryGame LotteryGame { get; set; } = null!;
    public ICollection<LotteryTicket> Tickets { get; set; } = new List<LotteryTicket>();
}
