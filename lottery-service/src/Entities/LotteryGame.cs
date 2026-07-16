namespace LotteryService.Entities;

public enum LotteryGameType
{
    Pick3 = 1,
    Pick4 = 2
}

public enum PickType
{
    Straight = 1,
    Boxed = 2
}

public class LotteryGame
{
    public int Id { get; set; }
    public LotteryGameType GameType { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<DrawingDetail> Drawings { get; set; } = new List<DrawingDetail>();
}
