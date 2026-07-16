namespace AccountsService.Entities;

/// <summary>
/// Migrated from legacy CustomerTransaction table.
/// TranCode: C=Credit, D=Debit. TranType: W=Wire, C=Cash, etc.
/// DocumentNumber is the legacy primary key — preserved for audit trail.
/// </summary>
public class CustomerTransaction
{
    public int      Id                  { get; set; }
    public int      CustomerId          { get; set; }
    public int      AgentId             { get; set; }

    public TransactionCode  Code        { get; set; }
    public TransactionType  Type        { get; set; }
    public decimal  Amount              { get; set; }
    public decimal  BalanceBefore       { get; set; }
    public decimal  BalanceAfter        { get; set; }

    public string?  Description         { get; set; }
    public string?  Reference           { get; set; }
    public string?  PaymentMethod       { get; set; }
    public string   EnteredBy           { get; set; } = string.Empty;

    public bool     IsVerified          { get; set; }
    public DateTime? VerifiedAt         { get; set; }

    public DateTime TransactionDate     { get; set; } = DateTime.UtcNow;
    public DateTime? ValueDate          { get; set; }

    // Casino adjustment flag (migrated from CasinoAdjustmentFlag bit)
    public bool     IsCasinoAdjustment  { get; set; }

    // Navigation
    public Customer Customer            { get; set; } = null!;
}

public enum TransactionCode
{
    Credit = 1,   // C
    Debit  = 2    // D
}

public enum TransactionType
{
    Wire              = 1,
    Cash              = 2,
    Check             = 3,
    BankTransfer      = 4,
    FreePlay          = 5,
    CreditAdjustment  = 6,
    WagerSettlement   = 7,
    Reversal          = 8,
    CasinoAdjustment  = 9,
    ManualCorrection  = 10
}
