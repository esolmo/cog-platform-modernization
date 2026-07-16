namespace AccountsService.Models.Requests;

public class UpdateCreditLimitRequest
{
    public decimal CreditLimit      { get; set; }
    public decimal WagerLimit       { get; set; }
    public decimal HardCreditLimit  { get; set; }
    public string  UpdatedBy        { get; set; } = string.Empty;
}
