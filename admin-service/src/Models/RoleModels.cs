namespace AdminService.Models;

public record RoleDto(
    int Id,
    string Name,
    string Description,
    bool IsActive,
    bool IsSystemRole,
    IReadOnlyList<PermissionDto> Permissions,
    int UserCount
);

public record PermissionDto(
    int Id,
    string Name,
    string Category,
    string Description
);

public record CreateRoleRequest(
    string Name,
    string Description,
    IReadOnlyList<int> PermissionIds
);

public record UpdateRoleRequest(
    string Description,
    bool IsActive,
    IReadOnlyList<int> PermissionIds
);

public record AssignRoleRequest(IReadOnlyList<int> RoleIds);
