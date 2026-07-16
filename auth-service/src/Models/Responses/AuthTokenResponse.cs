namespace AuthService.Models.Responses;

public class AuthTokenResponse
{
    public required string AccessToken            { get; set; }
    public required string RefreshToken           { get; set; }
    public DateTime        AccessTokenExpiresAt   { get; set; }
    public DateTime        RefreshTokenExpiresAt  { get; set; }
    public int             UserId                 { get; set; }
    public required string LoginName              { get; set; }
    public required string UserType               { get; set; }
    public int?            DomainEntityId         { get; set; }
    public List<string>    Roles                  { get; set; } = [];
    public List<string>    Permissions            { get; set; } = [];
}
