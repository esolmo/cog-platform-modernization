namespace AccountsService.Entities;

/// <summary>
/// Free play credit awards to customers.
/// Replaces legacy FreePlay table and AgCustFreePlayFrame.asp / AgCustFreePlayProcessFrame.asp.
/// </summary>
public class CustomerFreePlay
{
    public int      Id              { get; set; }
    public int      CustomerId      { get; set; }
    public decimal  Amount          { get; set; }
    public string   Description     { get; set; } = string.Empty;
    public DateTime IssuedAt        { get; set; } = DateTime.UtcNow;
    public string   IssuedBy        { get; set; } = string.Empty;
    public DateTime? ExpiresAt      { get; set; }
    public bool     IsRedeemed      { get; set; } = false;
    public DateTime? RedeemedAt     { get; set; }
    public decimal  RedeemedAmount  { get; set; } = 0;

    // Navigation
    public Customer Customer { get; set; } = null!;
}
