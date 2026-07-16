using AdminService.Common;
using AdminService.Data;
using AdminService.Entities;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<RoleDto?> GetRoleAsync(int roleId, CancellationToken ct = default);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default);
    Task<Result<RoleDto>> CreateRoleAsync(CreateRoleRequest request, int createdByUserId, CancellationToken ct = default);
    Task<Result<RoleDto>> UpdateRoleAsync(int roleId, UpdateRoleRequest request, int updatedByUserId, CancellationToken ct = default);
    Task<Result<bool>> DeleteRoleAsync(int roleId, int deletedByUserId, CancellationToken ct = default);
}

public class RoleService(
    AdminDbContext db,
    IAuditService auditService,
    ILogger<RoleService> logger) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        var roles = await db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return roles.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<RoleDto?> GetRoleAsync(int roleId, CancellationToken ct = default)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        return role is null ? null : MapToDto(role);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default)
    {
        var permissions = await db.Permissions
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync(ct);

        return permissions
            .Select(p => new PermissionDto(p.Id, p.Name, p.Category, p.Description))
            .ToList()
            .AsReadOnly();
    }

    public async Task<Result<RoleDto>> CreateRoleAsync(
        CreateRoleRequest request, int createdByUserId, CancellationToken ct = default)
    {
        if (await db.Roles.AnyAsync(r => r.Name == request.Name, ct))
            return Result<RoleDto>.Failure($"Role '{request.Name}' already exists.", "ROLE_EXISTS");

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            IsSystemRole = false
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        foreach (var permId in request.PermissionIds)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permId });
        }
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync(createdByUserId, "role.create", "Role", role.Id.ToString(),
            newValues: System.Text.Json.JsonSerializer.Serialize(new { role.Name }), ct: ct);

        logger.LogInformation("Role {RoleName} created by user {UserId}", request.Name, createdByUserId);
        var created = await GetRoleAsync(role.Id, ct);
        return Result<RoleDto>.Success(created!);
    }

    public async Task<Result<RoleDto>> UpdateRoleAsync(
        int roleId, UpdateRoleRequest request, int updatedByUserId, CancellationToken ct = default)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, ct);

        if (role is null)
            return Result<RoleDto>.Failure("Role not found.", "NOT_FOUND");

        if (role.IsSystemRole && !request.IsActive)
            return Result<RoleDto>.Failure("System roles cannot be deactivated.", "SYSTEM_ROLE_PROTECTED");

        role.Description = request.Description;
        role.IsActive = request.IsActive;

        db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var permId in request.PermissionIds)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
        }

        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(updatedByUserId, "role.update", "Role", roleId.ToString(), ct: ct);

        var updated = await GetRoleAsync(roleId, ct);
        return Result<RoleDto>.Success(updated!);
    }

    public async Task<Result<bool>> DeleteRoleAsync(
        int roleId, int deletedByUserId, CancellationToken ct = default)
    {
        var role = await db.Roles.FindAsync([roleId], ct);
        if (role is null)
            return Result<bool>.Failure("Role not found.", "NOT_FOUND");

        if (role.IsSystemRole)
            return Result<bool>.Failure("System roles cannot be deleted.", "SYSTEM_ROLE_PROTECTED");

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync(deletedByUserId, "role.delete", "Role", roleId.ToString(),
            oldValues: System.Text.Json.JsonSerializer.Serialize(new { role.Name }), ct: ct);

        return Result<bool>.Success(true);
    }

    private static RoleDto MapToDto(Role role) =>
        new(role.Id, role.Name, role.Description, role.IsActive, role.IsSystemRole,
            role.RolePermissions
                .Select(rp => new PermissionDto(rp.Permission.Id, rp.Permission.Name, rp.Permission.Category, rp.Permission.Description))
                .OrderBy(p => p.Category).ThenBy(p => p.Name)
                .ToList()
                .AsReadOnly(),
            role.UserRoles.Count);
}
