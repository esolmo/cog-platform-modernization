using Cog.Domain.Entities;

namespace BettingService.Models.Requests;

public class ProvisionAgentRequest
{
    public string   LoginName     { get; set; } = string.Empty;
    public string?  Name          { get; set; }
    public int?     ParentAgentId { get; set; }
    public AgentType AgentType    { get; set; } = AgentType.Agent;
}
