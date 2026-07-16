namespace BettingService.Models.Requests;

public class ProvisionCustomerRequest
{
    public string  LoginName      { get; set; } = string.Empty;
    public string  AgentLoginName { get; set; } = string.Empty;
    public string? Email          { get; set; }
    public string? Phone          { get; set; }
    public decimal MaxStraightWager { get; set; }
    public decimal MaxParlayWager   { get; set; }
    public decimal MaxTeaserWager   { get; set; }
    public decimal MaxIfBetWager    { get; set; }
    public decimal MinWager         { get; set; } = 1m;
    public decimal CreditLimit      { get; set; }
}
