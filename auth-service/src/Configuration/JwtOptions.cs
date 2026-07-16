namespace AuthService.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string SecretKey              { get; set; }
    public required string Issuer                 { get; set; }
    public required string Audience               { get; set; }
    public int             AccessTokenExpiryMinutes  { get; set; } = 15;
    public int             RefreshTokenExpiryDays    { get; set; } = 7;
}
