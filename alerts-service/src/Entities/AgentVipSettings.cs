namespace AlertsService.Entities;

/// <summary>
/// Stores which customers an agent has marked as VIP and their notification email.
/// </summary>
public class AgentVipSettings
{
    public int Id { get; set; }
    public int AgentId { get; set; }
    public string NotificationEmail { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AgentVipCustomer> VipCustomers { get; set; } = [];
}

public class AgentVipCustomer
{
    public int Id { get; set; }
    public int AgentVipSettingsId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerLoginName { get; set; } = string.Empty;

    public AgentVipSettings AgentVipSettings { get; set; } = null!;
}
