namespace AdminService.Entities;

/// <summary>
/// Application role — migrated from TApplicationRolesModel in LoginsRoles.
/// Replaces the BitPermission bitmask system with named, composable roles.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsSystemRole { get; set; } // cannot be deleted

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
