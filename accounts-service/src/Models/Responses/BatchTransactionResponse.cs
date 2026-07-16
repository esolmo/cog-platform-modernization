namespace AccountsService.Models.Responses;

public class BatchTransactionResponse
{
    public int TotalRequested { get; set; }
    public int Succeeded      { get; set; }
    public int Failed         { get; set; }
    public IReadOnlyList<BatchTransactionLineResult> Results { get; set; } = [];
}

public class BatchTransactionLineResult
{
    public int     Index      { get; set; }
    public bool    IsSuccess  { get; set; }
    public string? Error      { get; set; }
    public string? ErrorCode  { get; set; }
    public TransactionResponse? Transaction { get; set; }
}
