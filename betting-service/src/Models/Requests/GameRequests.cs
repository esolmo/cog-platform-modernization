using Cog.Domain.Entities;

namespace BettingService.Models.Requests;

public class CreateGameRequest
{
    public int SportTypeId { get; set; }
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
    public DateTime GameDate { get; set; }
    public string? RotationNumber { get; set; }
    public List<CreateGamePeriodRequest> Periods { get; set; } = [];
}

public class UpdateGameRequest
{
    public required string HomeTeam { get; set; }
    public required string AwayTeam { get; set; }
    public DateTime GameDate { get; set; }
    public string? RotationNumber { get; set; }
}

public class UpdateGameStatusRequest
{
    public GameStatus Status { get; set; }
}

public class CreateGamePeriodRequest
{
    public required string PeriodDescription { get; set; }
    public int PeriodNumber { get; set; }
}

public class CreateSportTypeRequest
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateSportTypeRequest
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}
