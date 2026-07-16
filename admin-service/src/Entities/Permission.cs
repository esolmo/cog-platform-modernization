namespace AdminService.Entities;

/// <summary>
/// Granular permission replacing the legacy BitPermission bitmask constants from Constants.asp.
/// Each permission maps to one functional capability in the platform.
/// </summary>
public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;       // e.g. "wagers.create"
    public string Category { get; set; } = string.Empty;   // e.g. "Wagers", "Accounts", "Admin"
    public string Description { get; set; } = string.Empty;
    public int LegacyBitValue { get; set; }                // original bitmask value for migration

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
