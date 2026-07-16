namespace Cog.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public int? RelatedWagerId { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public Wager? RelatedWager { get; set; }
}

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    WagerPlaced = 3,
    WagerWon = 4,
    WagerLost = 5,
    WagerPush = 6,
    WagerCancelled = 7,
    FreePlayCredited = 8,
    FreePlayUsed = 9,
    CasinoDebit = 10,
    CasinoCredit = 11,
    Adjustment = 12
}
