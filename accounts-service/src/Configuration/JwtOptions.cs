using System.ComponentModel.DataAnnotations;

namespace AccountsService.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string SecretKey  { get; init; } = string.Empty;
    [Required] public string Issuer     { get; init; } = string.Empty;
    [Required] public string Audience   { get; init; } = string.Empty;
}
