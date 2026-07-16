namespace Cog.Domain.Entities;

public class CasinoProfile
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public required string ExternalPlayerId { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActivityAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<CasinoTransaction> Transactions { get; set; } = new List<CasinoTransaction>();
}
