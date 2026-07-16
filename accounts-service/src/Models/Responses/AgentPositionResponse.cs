namespace AccountsService.Models.Responses;

public class AgentPositionResponse
{
    public int     AgentId               { get; set; }
    public string  AgentLoginName        { get; set; } = string.Empty;
    public int     TotalCustomers        { get; set; }
    public int     ActiveCustomers       { get; set; }

    // Outstanding balance exposure (negative = customers owe the agent)
    public decimal TotalCurrentBalance    { get; set; }
    public decimal TotalPendingWager      { get; set; }
    public int     TotalPendingWagerCount { get; set; }
    public decimal TotalFreePlay          { get; set; }

    // Credit headroom
    public decimal TotalCreditLimit      { get; set; }
    public decimal TotalAvailableCredit  { get; set; }

    public DateTime AsOf { get; set; }
}

public class AgentPositionSummaryResponse
{
    public int     AgentId            { get; set; }
    public string  LoginName          { get; set; } = string.Empty;
    public string? Name               { get; set; }
    public string  AgentType          { get; set; } = string.Empty;
    public int     CustomerCount      { get; set; }
    public decimal TotalBalance       { get; set; }
    public decimal TotalPending       { get; set; }
    public decimal TotalCreditLimit   { get; set; }
    public decimal TotalAvailableCredit { get; set; }
}
