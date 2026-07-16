namespace AccountsService.Entities;

/// <summary>
/// Migrated from legacy Customer table (TbFssLogins / Customer SQL schema).
/// LoginName max 10 chars, matching legacy constraint.
/// </summary>
public class Customer
{
    public int    Id              { get; set; }
    public string LoginName       { get; set; } = string.Empty;
    public string? AlternateLoginName { get; set; }

    // The legacy PasswordHash is NOT stored here — auth-service owns credentials.
    // DomainEntityId in auth-service.ApplicationUser.DomainEntityId references this Id.

    public int    AgentId         { get; set; }
    public bool   IsActive        { get; set; } = true;

    public CustomerStatus Status  { get; set; } = CustomerStatus.Active;
    public OddsFormat OddsFormat  { get; set; } = OddsFormat.American;

    public bool   InstantActionEnabled { get; set; } = true;

    public string? Email          { get; set; }
    public string? Phone          { get; set; }

    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
    public string   CreatedBy     { get; set; } = string.Empty;
    public DateTime? UpdatedAt    { get; set; }
    public string?  UpdatedBy     { get; set; }

    // Navigation
    public Agent                Agent       { get; set; } = null!;
    public CustomerBalance      Balance     { get; set; } = null!;
    public CustomerLimits       Limits      { get; set; } = null!;
    public CustomerPermissions? Permissions { get; set; }
    public ICollection<CustomerTransaction> Transactions { get; set; } = [];
    public ICollection<CustomerComment>     Comments     { get; set; } = [];
    public ICollection<CustomerFreePlay>    FreePlays    { get; set; } = [];
}

public enum CustomerStatus
{
    Active   = 1,
    Inactive = 2,
    Suspended = 3
}

public enum OddsFormat
{
    American = 1,
    Decimal  = 2,
    Fractional = 3
}
