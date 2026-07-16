namespace CasinoService.Entities;

public enum CasinoTransactionType { Deposit, Withdrawal }
public enum CasinoTransactionStatus { Pending, Completed, RolledBack }

public class CasinoTransaction
{
    public int Id { get; set; }
    public int CasinoPlayerId { get; set; }
    public CasinoTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Internal document number (from spNextDocNum).</summary>
    public int DocumentNumber { get; set; }

    /// <summary>Transfer reference returned by the external provider on init.</summary>
    public string TransferReference { get; set; } = string.Empty;

    /// <summary>Remote reference returned by the external provider on init.</summary>
    public string RemoteReference { get; set; } = string.Empty;

    public CasinoTransactionStatus Status { get; set; } = CasinoTransactionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public CasinoPlayer Player { get; set; } = null!;
}
