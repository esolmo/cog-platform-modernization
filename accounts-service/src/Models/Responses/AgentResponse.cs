using AccountsService.Entities;

namespace AccountsService.Models.Responses;

public class AgentResponse
{
    public int      Id              { get; set; }
    public string   LoginName       { get; set; } = string.Empty;
    public string?  Name            { get; set; }
    public int?     ParentAgentId   { get; set; }
    public string?  ParentLoginName { get; set; }
    public string   AgentType       { get; set; } = string.Empty;
    public decimal  CreditLimitMax  { get; set; }
    public decimal  WagerLimitMax   { get; set; }
    public string   CommissionType  { get; set; } = string.Empty;
    public decimal  CommissionRate  { get; set; }
    public bool     IsActive        { get; set; }
    public int      CustomerCount   { get; set; }
    public int      SubAgentCount   { get; set; }
}

public class AgentHierarchyNode
{
    public int      Id          { get; set; }
    public string   LoginName   { get; set; } = string.Empty;
    public string?  Name        { get; set; }
    public AgentType AgentType  { get; set; }
    public int      Level       { get; set; }
    public List<AgentHierarchyNode> Children { get; set; } = [];
}
