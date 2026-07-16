using AccountsService.Entities;

namespace AccountsService.Models.Requests;

public class UpdateCustomerRequest
{
    public string?    AlternateLoginName  { get; set; }
    public string?    Email               { get; set; }
    public string?    Phone               { get; set; }
    public OddsFormat? OddsFormat         { get; set; }
    public bool?      InstantActionEnabled { get; set; }
    public string     UpdatedBy           { get; set; } = string.Empty;
}
