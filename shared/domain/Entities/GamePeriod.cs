namespace Cog.Domain.Entities;

public class GamePeriod
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public required string PeriodDescription { get; set; }
    public int PeriodNumber { get; set; }

    public Game Game { get; set; } = null!;
    public LineSet? LineSet { get; set; }
}
