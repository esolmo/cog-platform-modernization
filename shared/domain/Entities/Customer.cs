namespace Cog.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public required string LoginName { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int AgentId { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public bool IsVip { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? Notes { get; set; }

    public Agent Agent { get; set; } = null!;
    public CustomerBalance? Balance { get; set; }
    public CustomerLimits? Limits { get; set; }
    public ICollection<Wager> Wagers { get; set; } = new List<Wager>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public enum CustomerStatus
{
    Active = 1,
    Suspended = 2,
    Closed = 3
}
