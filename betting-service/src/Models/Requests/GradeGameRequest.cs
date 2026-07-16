namespace BettingService.Models.Requests;

public class GradeGameRequest
{
    public List<PeriodScoreRequest> PeriodScores { get; set; } = [];
}

public class PeriodScoreRequest
{
    public int PeriodId  { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
}
