using AccountsService.Entities;

namespace AccountsService.Models.Responses;

public class CustomerResponse
{
    public int     Id                  { get; set; }
    public string  LoginName           { get; set; } = string.Empty;
    public string? AlternateLoginName  { get; set; }
    public int     AgentId             { get; set; }
    public string  AgentLoginName      { get; set; } = string.Empty;
    public string  Status              { get; set; } = string.Empty;
    public string  OddsFormat          { get; set; } = string.Empty;
    public bool    InstantActionEnabled { get; set; }
    public string? Email               { get; set; }
    public string? Phone               { get; set; }
    public DateTime CreatedAt          { get; set; }
    public BalanceSummary Balance      { get; set; } = new();
}

public class BalanceSummary
{
    public decimal CreditLimit          { get; set; }
    public decimal WagerLimit           { get; set; }
    public decimal CurrentBalance       { get; set; }
    public decimal AvailableCredit      { get; set; }
    public decimal PendingWagerBalance  { get; set; }
    public int     PendingWagerCount    { get; set; }
    public decimal FreePlayBalance      { get; set; }
}
