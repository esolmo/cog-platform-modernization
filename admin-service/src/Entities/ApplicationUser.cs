namespace AdminService.Entities;

/// <summary>
/// System user (admin/operator) — migrated from TApplicationUserModel in LoginsRoles.
/// Not to be confused with Customer (account holder) from accounts-service.
/// </summary>
public class ApplicationUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string MaxAccessLevel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
