namespace AuthService.Models.Responses;

public class UserInfoResponse
{
    public int          Id             { get; set; }
    public string       LoginName      { get; set; } = string.Empty;
    public string?      Email          { get; set; }
    public string       UserType       { get; set; } = string.Empty;
    public int?         DomainEntityId { get; set; }
    public bool         IsActive       { get; set; }
    public string?      MaxLevel       { get; set; }
    public List<string> Roles          { get; set; } = [];
    public List<string> Permissions    { get; set; } = [];
}
