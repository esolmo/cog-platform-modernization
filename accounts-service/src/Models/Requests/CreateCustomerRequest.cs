using AccountsService.Entities;

namespace AccountsService.Models.Requests;

public class CreateCustomerRequest
{
    public string   LoginName           { get; set; } = string.Empty;
    public string?  AlternateLoginName  { get; set; }
    public int      AgentId             { get; set; }
    public string?  Email               { get; set; }
    public string?  Phone               { get; set; }
    public OddsFormat OddsFormat        { get; set; } = OddsFormat.American;
    public decimal  CreditLimit         { get; set; }
    public decimal  WagerLimit          { get; set; }
    public decimal  MaxStraightWager    { get; set; }
    public decimal  MaxParlayWager      { get; set; }
    public decimal  MaxParlayPayout     { get; set; }
    public decimal  MaxTeaserWager      { get; set; }
    public decimal  MaxIfBetWager       { get; set; }
    public decimal  HardCreditLimit     { get; set; }
    public string   CreatedBy           { get; set; } = string.Empty;
}
