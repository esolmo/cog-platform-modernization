namespace BettingService.Models.Responses;

public class GradeGameResponse
{
    public int     GameId        { get; set; }
    public int     WagersGraded  { get; set; }
    public int     WagersWon     { get; set; }
    public int     WagersLost    { get; set; }
    public int     WagersPushed  { get; set; }
    public decimal TotalPayout   { get; set; }
}
