namespace Cog.Domain.Entities;

public class AgentPermission
{
    public int Id { get; set; }
    public int AgentId { get; set; }
    public required string PermissionKey { get; set; }
    public bool IsGranted { get; set; } = true;

    public Agent Agent { get; set; } = null!;
}
