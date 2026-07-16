namespace AccountsService.Models.Requests;

public class BatchCreateTransactionRequest
{
    public IReadOnlyList<CreateTransactionRequest> Transactions { get; set; } = [];
    public bool StopOnFirstError { get; set; } = false;
}
