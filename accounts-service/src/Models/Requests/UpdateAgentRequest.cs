using AccountsService.Entities;

namespace AccountsService.Models.Requests;

public class UpdateAgentRequest
{
    public string?   Name            { get; set; }
    public decimal?  CreditLimitMax  { get; set; }
    public decimal?  WagerLimitMax   { get; set; }
    public CommissionType? CommissionType { get; set; }
    public decimal?  CommissionRate  { get; set; }
    public string    UpdatedBy       { get; set; } = string.Empty;
}

public class UpdateAgentCreditLimitsRequest
{
    public decimal   CreditLimitMax  { get; set; }
    public decimal   WagerLimitMax   { get; set; }
    public string    UpdatedBy       { get; set; } = string.Empty;
}

public class MoveAgentRequest
{
    public int       NewParentAgentId { get; set; }
    public string    UpdatedBy        { get; set; } = string.Empty;
}
