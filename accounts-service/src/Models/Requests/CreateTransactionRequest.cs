using AccountsService.Entities;

namespace AccountsService.Models.Requests;

public class CreateTransactionRequest
{
    public int              CustomerId     { get; set; }
    public TransactionCode  Code           { get; set; }
    public TransactionType  Type           { get; set; }
    public decimal          Amount         { get; set; }
    public string?          Description    { get; set; }
    public string?          Reference      { get; set; }
    public string?          PaymentMethod  { get; set; }
    public DateTime?        ValueDate      { get; set; }
    public string           EnteredBy      { get; set; } = string.Empty;
}
