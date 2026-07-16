namespace Cog.Domain.Entities;

public class Agent
{
    public int Id { get; set; }
    public required string LoginName { get; set; }
    public required string PasswordHash { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public int? ParentAgentId { get; set; }
    public AgentType AgentType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Agent? ParentAgent { get; set; }
    public ICollection<Agent> SubAgents { get; set; } = new List<Agent>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<AgentPermission> Permissions { get; set; } = new List<AgentPermission>();
}

public enum AgentType
{
    Master = 1,
    Agent = 2,
    SubAgent = 3
}
