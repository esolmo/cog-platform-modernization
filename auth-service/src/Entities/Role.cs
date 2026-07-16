namespace AuthService.Entities;

/// <summary>
/// Application role. Migrated from TbFssRoles (ID, Name, CreateBy).
/// Predefined roles replace the legacy BitPermission bitmask system.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>
/// Well-known role names. Add to database seed.
/// </summary>
public static class RoleNames
{
    public const string MasterAgent    = "MasterAgent";
    public const string Agent          = "Agent";
    public const string SubAgent       = "SubAgent";
    public const string LinesManager   = "LinesManager";
    public const string Admin          = "Admin";
    public const string CustomerWeb    = "CustomerWeb";
    public const string ReportsViewer  = "ReportsViewer";
    public const string LotteryManager = "LotteryManager";
}
