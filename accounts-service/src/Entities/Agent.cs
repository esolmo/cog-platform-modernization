namespace AccountsService.Entities;

/// <summary>
/// Migrated from legacy Agent table.
/// AgentType maps to legacy char(1) column: M=Master, A=Agent, S=SubAgent.
/// </summary>
public class Agent
{
    public int      Id               { get; set; }
    public string   LoginName        { get; set; } = string.Empty;
    public string?  Name             { get; set; }
    public int?     ParentAgentId    { get; set; }
    public AgentType AgentType       { get; set; } = AgentType.Agent;

    public decimal  CreditLimitMax   { get; set; }
    public decimal  WagerLimitMax    { get; set; }

    // Commission settings — drive the weekly settlement calculation
    public CommissionType CommissionType { get; set; } = CommissionType.WeeklyProfit;
    public decimal  CommissionRate   { get; set; } = 50m;

    public bool     IsActive         { get; set; } = true;
    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
    public string   CreatedBy        { get; set; } = string.Empty;

    // Navigation
    public Agent?                   ParentAgent  { get; set; }
    public ICollection<Agent>       SubAgents    { get; set; } = [];
    public ICollection<Customer>    Customers    { get; set; } = [];
}

public enum AgentType
{
    Master   = 1,   // M
    Agent    = 2,   // A
    SubAgent = 3    // S
}
