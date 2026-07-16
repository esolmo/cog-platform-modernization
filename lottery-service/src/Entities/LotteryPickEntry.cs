namespace LotteryService.Entities;

public class LotteryPickEntry
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public int Number1 { get; set; }
    public int Number2 { get; set; }
    public int Number3 { get; set; }
    public int Number4 { get; set; }
    public PickType PickType { get; set; }
    public LotteryGameType LotteryGameType { get; set; }
    public int PlayCount { get; set; }
    public decimal Amount { get; set; }
    public decimal Cost { get; set; }
    public decimal Prize { get; set; }

    public LotteryTicket Ticket { get; set; } = null!;
}
