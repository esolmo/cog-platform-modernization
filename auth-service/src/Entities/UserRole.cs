namespace AuthService.Entities;

/// <summary>
/// Many-to-many join between ApplicationUser and Role.
/// Migrated from TbFssPermissionRoles.
/// </summary>
public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedBy { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
