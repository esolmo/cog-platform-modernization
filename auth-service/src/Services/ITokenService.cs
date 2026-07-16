using AuthService.Entities;
using AuthService.Models.Responses;

namespace AuthService.Services;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    string HashToken(string token);
    bool ValidateAccessToken(string token, out int userId, out string loginName);
}
