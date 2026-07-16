using AuthService.Models.Requests;
using AuthService.Models.Responses;
using Cog.Domain.Common;

namespace AuthService.Services;

public interface IAuthService
{
    Task<Result<AuthTokenResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct);
    Task<Result<AuthTokenResponse>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct);
    Task<Result<UserInfoResponse>> GetCurrentUserAsync(int userId, CancellationToken ct);
}
