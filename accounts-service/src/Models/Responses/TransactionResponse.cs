namespace AccountsService.Models.Responses;

public class TransactionResponse
{
    public int      Id              { get; set; }
    public int      CustomerId      { get; set; }
    public string   Code            { get; set; } = string.Empty;
    public string   Type            { get; set; } = string.Empty;
    public decimal  Amount          { get; set; }
    public decimal  BalanceBefore   { get; set; }
    public decimal  BalanceAfter    { get; set; }
    public string?  Description     { get; set; }
    public string?  Reference       { get; set; }
    public string   EnteredBy       { get; set; } = string.Empty;
    public bool     IsVerified      { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime? ValueDate      { get; set; }
}
