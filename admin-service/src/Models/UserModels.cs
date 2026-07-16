namespace AdminService.Models;

public record UserDto(
    int Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    string MaxAccessLevel,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Roles
);

public record CreateUserRequest(
    string Username,
    string Password,
    string Email,
    string FirstName,
    string LastName,
    string MaxAccessLevel,
    IReadOnlyList<int> RoleIds
);

public record UpdateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string MaxAccessLevel,
    bool IsActive,
    IReadOnlyList<int> RoleIds
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ResetPasswordRequest(string NewPassword);

public record UserPagedResult(
    IReadOnlyList<UserDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
);
