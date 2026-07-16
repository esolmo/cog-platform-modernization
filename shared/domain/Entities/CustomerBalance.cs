namespace Cog.Domain.Entities;

public class CustomerBalance
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Balance { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal TemporaryCreditLimit { get; set; }
    public decimal CasinoBalance { get; set; }
    public decimal FreePlaysBalance { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
}
