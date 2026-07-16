namespace Cog.Domain.Entities;

public class CasinoTransaction
{
    public int Id { get; set; }
    public int CasinoProfileId { get; set; }
    public CasinoTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public string? GameReference { get; set; }
    public string? ExternalTransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CasinoProfile CasinoProfile { get; set; } = null!;
}

public enum CasinoTransactionType
{
    Bet = 1,
    Win = 2,
    Refund = 3,
    Deposit = 4,
    Withdrawal = 5
}
