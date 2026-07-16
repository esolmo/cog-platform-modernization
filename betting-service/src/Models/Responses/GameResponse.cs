using Cog.Domain.Entities;

namespace BettingService.Models.Responses;

public class GameResponse
{
    public int Id { get; set; }
    public int SportTypeId { get; set; }
    public string SportName { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime GameDate { get; set; }
    public GameStatus Status { get; set; }
    public string? RotationNumber { get; set; }
    public List<GamePeriodResponse> Periods { get; set; } = [];
}

public class GamePeriodResponse
{
    public int Id { get; set; }
    public string PeriodDescription { get; set; } = string.Empty;
    public int PeriodNumber { get; set; }
    public LineSetResponse? Lines { get; set; }
}

public class LineSetResponse
{
    public int Id { get; set; }
    public decimal? Spread { get; set; }
    public decimal? SpreadJuice { get; set; }
    public decimal? HomeMoneyLine { get; set; }
    public decimal? AwayMoneyLine { get; set; }
    public decimal? Total { get; set; }
    public decimal? OverJuice { get; set; }
    public decimal? UnderJuice { get; set; }
    public bool OfferingSpread { get; set; }
    public bool OfferingMoneyLine { get; set; }
    public bool OfferingTotal { get; set; }
    public DateTime LastModified { get; set; }
}

public class SportTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}
