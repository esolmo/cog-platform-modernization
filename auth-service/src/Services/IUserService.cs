using AuthService.Models.Requests;
using AuthService.Models.Responses;
using Cog.Domain.Common;

namespace AuthService.Services;

public interface IUserService
{
    Task<Result<UserInfoResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct);
    Task<Result<UserInfoResponse>> GetUserByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<UserInfoResponse>> GetUsersAsync(int page, int pageSize, CancellationToken ct);
    Task<Result> AssignRoleAsync(int userId, int roleId, string assignedBy, CancellationToken ct);
    Task<Result> RemoveRoleAsync(int userId, int roleId, CancellationToken ct);
    Task<Result> DeactivateUserAsync(int userId, CancellationToken ct);
    Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct);
}
