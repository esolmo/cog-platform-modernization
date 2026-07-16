using AccountsService.Entities;

namespace AccountsService.Models.Requests;

public class CreateAgentRequest
{
    public string    LoginName       { get; set; } = string.Empty;
    public string?   Name            { get; set; }
    public int?      ParentAgentId   { get; set; }
    public AgentType AgentType       { get; set; } = AgentType.Agent;
    public decimal   CreditLimitMax  { get; set; }
    public decimal   WagerLimitMax   { get; set; }

    // Commission
    public CommissionType CommissionType { get; set; } = CommissionType.WeeklyProfit;
    public decimal        CommissionRate { get; set; } = 50m;  // default 50 %

    public string    CreatedBy       { get; set; } = string.Empty;
}
