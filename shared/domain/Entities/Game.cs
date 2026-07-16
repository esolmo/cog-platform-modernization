namespace Cog.Domain.Entities;

public class Game
{
    public int Id { get; set; }
    public int SportTypeId { get; set; }
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
    public DateTime GameDate { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Upcoming;
    public string? RotationNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public SportType SportType { get; set; } = null!;
    public ICollection<GamePeriod> Periods { get; set; } = new List<GamePeriod>();
}

public enum GameStatus
{
    Upcoming = 1,
    InProgress = 2,
    Final = 3,
    Postponed = 4,
    Cancelled = 5
}
