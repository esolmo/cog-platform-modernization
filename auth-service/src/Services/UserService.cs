using AuthService.Data;
using AuthService.Entities;
using AuthService.Models.Requests;
using AuthService.Models.Responses;
using Cog.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class UserService(AuthDbContext db, ILogger<UserService> logger) : IUserService
{
    public async Task<Result<UserInfoResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(u => u.LoginName == request.LoginName, ct);
        if (exists)
            return Result<UserInfoResponse>.Failure("Login name already in use", "DUPLICATE_LOGIN");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        var user = new ApplicationUser
        {
            LoginName      = request.LoginName,
            PasswordHash   = passwordHash,
            Email          = request.Email,
            UserType       = request.UserType,
            DomainEntityId = request.DomainEntityId,
            MaxLevel       = request.MaxLevel,
            CreatedBy      = request.CreatedBy,
            IsActive       = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        // Assign initial roles by ID
        foreach (var roleId in request.RoleIds)
        {
            var roleExists = await db.Roles.AnyAsync(r => r.Id == roleId, ct);
            if (roleExists)
                db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId, AssignedBy = request.CreatedBy });
        }

        // Assign initial roles by name (used by inter-service provisioning)
        if (request.RoleNames.Count > 0)
        {
            var rolesByName = await db.Roles
                .Where(r => request.RoleNames.Contains(r.Name))
                .ToListAsync(ct);
            foreach (var role in rolesByName)
            {
                var alreadyQueued = db.UserRoles.Local.Any(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
                if (!alreadyQueued)
                    db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedBy = request.CreatedBy });
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created user {LoginName} (ID {UserId})", user.LoginName, user.Id);
        return await GetUserByIdAsync(user.Id, ct);
    }

    public async Task<Result<UserInfoResponse>> GetUserByIdAsync(int id, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null) return Result<UserInfoResponse>.Failure("User not found", "NOT_FOUND");

        return Result<UserInfoResponse>.Success(MapToResponse(user));
    }

    public async Task<PagedResult<UserInfoResponse>> GetUsersAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.LoginName);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<UserInfoResponse>
        {
            Items      = items.Select(MapToResponse).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<Result> AssignRoleAsync(int userId, int roleId, string assignedBy, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return Result.Failure("User not found", "NOT_FOUND");

        var roleExists = await db.Roles.AnyAsync(r => r.Id == roleId, ct);
        if (!roleExists) return Result.Failure("Role not found", "ROLE_NOT_FOUND");

        var alreadyAssigned = await db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

        if (alreadyAssigned) return Result.Success();

        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId, AssignedBy = assignedBy });
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(int userId, int roleId, CancellationToken ct)
    {
        var userRole = await db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

        if (userRole is null) return Result.Success();

        db.UserRoles.Remove(userRole);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeactivateUserAsync(int userId, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return Result.Failure("User not found", "NOT_FOUND");

        user.IsActive = false;

        // Revoke all active refresh tokens
        var tokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(ct);
        foreach (var token in tokens)
        {
            token.IsRevoked     = true;
            token.RevokedReason = "Account deactivated";
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await db.Users.FindAsync([userId], ct);
        if (user is null) return Result.Failure("User not found", "NOT_FOUND");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect", "INVALID_PASSWORD");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static UserInfoResponse MapToResponse(ApplicationUser user) => new()
    {
        Id             = user.Id,
        LoginName      = user.LoginName,
        Email          = user.Email,
        UserType       = user.UserType.ToString(),
        DomainEntityId = user.DomainEntityId,
        IsActive       = user.IsActive,
        MaxLevel       = user.MaxLevel,
        Roles          = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
        Permissions    = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList()
    };
}
