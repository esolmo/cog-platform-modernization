namespace AdminService.Entities;

/// <summary>
/// Immutable audit trail for all administrative actions.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;       // e.g. "user.create", "role.assign"
    public string EntityType { get; set; } = string.Empty;   // e.g. "ApplicationUser", "Role"
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }  // JSON snapshot before change
    public string? NewValues { get; set; }  // JSON snapshot after change
    public string IpAddress { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
