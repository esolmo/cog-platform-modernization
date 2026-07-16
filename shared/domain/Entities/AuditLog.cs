namespace Cog.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public required string EntityType { get; set; }
    public int EntityId { get; set; }
    public required string Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public required string PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
