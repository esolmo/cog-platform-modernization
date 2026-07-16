namespace AccountsService.Models.Responses;

public class AgentFiguresResponse
{
    public int      AgentId         { get; set; }
    public string   AgentLoginName  { get; set; } = string.Empty;
    public DateTime From            { get; set; }
    public DateTime To              { get; set; }

    // Period totals across all direct customers
    public decimal  TotalCredits    { get; set; }   // deposits / collections received
    public decimal  TotalDebits     { get; set; }   // payouts / withdrawals
    public decimal  NetTransactions { get; set; }   // Credits - Debits
    public decimal  CasinoAdjustments { get; set; }
    public decimal  FreePlayIssued  { get; set; }
    public int      TransactionCount { get; set; }

    public IReadOnlyList<CustomerFiguresLine> Customers { get; set; } = [];
}

public class CustomerFiguresLine
{
    public int     CustomerId      { get; set; }
    public string  LoginName       { get; set; } = string.Empty;
    public decimal Credits         { get; set; }
    public decimal Debits          { get; set; }
    public decimal Net             { get; set; }
    public decimal CasinoAdj       { get; set; }
    public decimal FreePlay        { get; set; }
    public decimal CurrentBalance  { get; set; }
    public int     TxCount         { get; set; }
}
