namespace AuthService.Entities;

/// <summary>
/// Fine-grained permission. Migrated from TbFssPermission.
/// Permissions are grouped by feature area and assigned to roles.
/// </summary>
public class Permission
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; }
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Well-known permission names, organised by category.
/// </summary>
public static class PermissionNames
{
    // Wager permissions
    public const string WagersCreate    = "Wagers.Create";
    public const string WagersRead      = "Wagers.Read";
    public const string WagersCancel    = "Wagers.Cancel";
    public const string WagersGrade     = "Wagers.Grade";

    // Lines permissions (migrated from LinesManager desktop)
    public const string LinesRead       = "Lines.Read";
    public const string LinesWrite      = "Lines.Write";
    public const string LinesShade      = "Lines.Shade";

    // Account permissions (migrated from Accounts desktop + web)
    public const string AccountsRead    = "Accounts.Read";
    public const string AccountsWrite   = "Accounts.Write";
    public const string AccountsDeposit = "Accounts.Deposit";
    public const string AccountsWithdraw= "Accounts.Withdraw";

    // Agent permissions
    public const string AgentsRead      = "Agents.Read";
    public const string AgentsManage    = "Agents.Manage";

    // Reports permissions
    public const string ReportsView     = "Reports.View";
    public const string ReportsExport   = "Reports.Export";

    // Admin permissions
    public const string UsersManage     = "Users.Manage";
    public const string RolesManage     = "Roles.Manage";
    public const string SystemConfig    = "System.Config";

    // Lottery permissions
    public const string LotteryManage   = "Lottery.Manage";
    public const string LotteryPlay     = "Lottery.Play";
}
