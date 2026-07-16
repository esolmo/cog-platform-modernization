namespace AccountsService.Entities;

/// <summary>
/// Migrated from legacy CustomerBalance table.
/// One-to-one with Customer; owned by customer bounded context.
/// </summary>
public class CustomerBalance
{
    public int     CustomerId              { get; set; }
    public decimal CreditLimit             { get; set; }
    public decimal WagerLimit              { get; set; }
    public decimal CurrentBalance          { get; set; }
    public decimal PendingWagerBalance     { get; set; }
    public int     PendingWagerCount       { get; set; }
    public decimal TempCreditAdjustment    { get; set; }
    public DateTime? TempCreditAdjExpiry   { get; set; }
    public decimal FreePlayBalance         { get; set; }
    public decimal FreePlayPendingBalance  { get; set; }
    public int     FreePlayPendingCount    { get; set; }

    // Computed: effective credit available
    public decimal AvailableCredit =>
        CreditLimit + TempCreditAdjustment - CurrentBalance - PendingWagerBalance;

    public Customer Customer { get; set; } = null!;
}
