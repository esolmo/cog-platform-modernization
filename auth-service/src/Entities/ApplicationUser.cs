namespace AuthService.Entities;

/// <summary>
/// Represents any user that can log in to a COG application.
/// Migrated from TbFssLogins (LoginId, RoleId, Active, MaxLevel, Email).
/// Covers three legacy principal types: Agent, Customer/Player, Employee/Admin.
/// </summary>
public class ApplicationUser
{
    public int Id { get; set; }
    public required string LoginName { get; set; }
    public required string PasswordHash { get; set; }
    public UserType UserType { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Email { get; set; }

    /// <summary>
    /// For agents: the hierarchy level cap (migrated from TbFssLogins.MaxLevel).
    /// Null means no restriction.
    /// </summary>
    public string? MaxLevel { get; set; }

    /// <summary>
    /// Reference to the corresponding domain entity (AgentId or CustomerId).
    /// Null for employees who exist only in the auth system.
    /// </summary>
    public int? DomainEntityId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? CreatedBy { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public enum UserType
{
    Agent = 1,
    Customer = 2,
    Employee = 3
}
