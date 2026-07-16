using AdminService.Common;
using AdminService.Data;
using AdminService.Entities;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Services;

public interface IUserService
{
    Task<UserPagedResult> GetUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<UserDto?> GetUserAsync(int userId, CancellationToken ct = default);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, int createdByUserId, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateUserAsync(int userId, UpdateUserRequest request, int updatedByUserId, CancellationToken ct = default);
    Task<Result<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(int userId, ResetPasswordRequest request, int resetByUserId, CancellationToken ct = default);
    Task<Result<bool>> DeleteUserAsync(int userId, int deletedByUserId, CancellationToken ct = default);
    Task<Result<bool>> AssignRolesAsync(int userId, AssignRoleRequest request, int assignedByUserId, CancellationToken ct = default);
}

public class UserService(
    AdminDbContext db,
    IAuditService auditService,
    IAuthProvisioningService authProvisioning,
    ILogger<UserService> logger) : IUserService
{
    public async Task<UserPagedResult> GetUsersAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new UserPagedResult(
            items.Select(MapToDto).ToList().AsReadOnly(),
            totalCount, page, pageSize, totalPages,
            page < totalPages, page > 1);
    }

    public async Task<UserDto?> GetUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user is null ? null : MapToDto(user);
    }

    public async Task<Result<UserDto>> CreateUserAsync(
        CreateUserRequest request, int createdByUserId, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username, ct))
            return Result<UserDto>.Failure($"Username '{request.Username}' is already taken.", "USERNAME_TAKEN");

        if (await db.Users.AnyAsync(u => u.Email == request.Email, ct))
            return Result<UserDto>.Failure($"Email '{request.Email}' is already in use.", "EMAIL_TAKEN");

        var user = new ApplicationUser
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MaxAccessLevel = request.MaxAccessLevel,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        if (request.RoleIds.Count > 0)
        {
            var roleAssignment = new AssignRoleRequest(request.RoleIds);
            await AssignRolesAsync(user.Id, roleAssignment, createdByUserId, ct);
        }

        await auditService.LogAsync(createdByUserId, "user.create", "ApplicationUser", user.Id.ToString(),
            newValues: System.Text.Json.JsonSerializer.Serialize(new { user.Username, user.Email }), ct: ct);

        // Resolve local role names and sync the user to auth-service so they can log in
        var roleNames = request.RoleIds.Count > 0
            ? await db.Roles
                .Where(r => request.RoleIds.Contains(r.Id))
                .Select(r => r.Name)
                .ToListAsync(ct)
            : new List<string>();

        await authProvisioning.ProvisionUserAsync(
            request.Username, request.Password, request.Email,
            request.MaxAccessLevel, roleNames, ct);

        logger.LogInformation("User {Username} created by user {CreatedBy}", request.Username, createdByUserId);
        var created = await GetUserAsync(user.Id, ct);
        return Result<UserDto>.Success(created!);
    }

    public async Task<Result<UserDto>> UpdateUserAsync(
        int userId, UpdateUserRequest request, int updatedByUserId, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<UserDto>.Failure("User not found.", "NOT_FOUND");

        var oldValues = System.Text.Json.JsonSerializer.Serialize(new { user.Email, user.IsActive });

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.MaxAccessLevel = request.MaxAccessLevel;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Replace role assignments
        db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var roleId in request.RoleIds)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedByUserId = updatedByUserId
            });
        }

        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(updatedByUserId, "user.update", "ApplicationUser", userId.ToString(),
            oldValues: oldValues,
            newValues: System.Text.Json.JsonSerializer.Serialize(new { request.Email, request.IsActive }),
            ct: ct);

        var updated = await GetUserAsync(userId, ct);
        return Result<UserDto>.Success(updated!);
    }

    public async Task<Result<bool>> ChangePasswordAsync(
        int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return Result<bool>.Failure("User not found.", "NOT_FOUND");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Result<bool>.Failure("Current password is incorrect.", "INVALID_PASSWORD");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(
        int userId, ResetPasswordRequest request, int resetByUserId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return Result<bool>.Failure("User not found.", "NOT_FOUND");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync(resetByUserId, "user.password_reset", "ApplicationUser", userId.ToString(), ct: ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteUserAsync(
        int userId, int deletedByUserId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return Result<bool>.Failure("User not found.", "NOT_FOUND");

        // Soft delete — preserve for audit trail
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync(deletedByUserId, "user.delete", "ApplicationUser", userId.ToString(),
            oldValues: System.Text.Json.JsonSerializer.Serialize(new { user.Username }), ct: ct);

        logger.LogInformation("User {UserId} deactivated by {DeletedBy}", userId, deletedByUserId);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> AssignRolesAsync(
        int userId, AssignRoleRequest request, int assignedByUserId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null)
            return Result<bool>.Failure("User not found.", "NOT_FOUND");

        var existingRoles = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(ct);
        db.UserRoles.RemoveRange(existingRoles);

        foreach (var roleId in request.RoleIds)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedByUserId = assignedByUserId
            });
        }

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private static UserDto MapToDto(ApplicationUser user) =>
        new(user.Id, user.Username, user.Email, user.FirstName, user.LastName,
            user.IsActive, user.MaxAccessLevel, user.CreatedAt, user.LastLoginAt,
            user.UserRoles.Select(ur => ur.Role.Name).ToList().AsReadOnly());
}
