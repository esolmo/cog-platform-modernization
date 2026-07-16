using AuthService.Configuration;
using AuthService.Data;
using AuthService.Entities;
using AuthService.Models.Requests;
using AuthService.Models.Responses;
using Cog.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthService.Services;

public class AuthService(
    AuthDbContext db,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<Result<AuthTokenResponse>> LoginAsync(
        LoginRequest request, string? ipAddress, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.LoginName == request.LoginName, ct);

        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Login failed: user {LoginName} not found or inactive", request.LoginName);
            return Result<AuthTokenResponse>.Failure("Invalid credentials", "INVALID_CREDENTIALS");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Login failed: invalid password for {LoginName}", request.LoginName);
            return Result<AuthTokenResponse>.Failure("Invalid credentials", "INVALID_CREDENTIALS");
        }

        var roles       = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var accessToken  = tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = tokenService.GenerateRefreshToken();
        var tokenHash    = tokenService.HashToken(refreshToken);

        // Revoke previous active refresh tokens for this user (single-session policy per user)
        var existingTokens = await db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);
        foreach (var existingToken in existingTokens)
        {
            existingToken.IsRevoked = true;
            existingToken.RevokedReason = "Replaced by new login";
        }

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId       = user.Id,
            TokenHash    = tokenHash,
            ExpiresAt    = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays),
            CreatedByIp  = ipAddress
        });

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {LoginName} (ID {UserId}) logged in successfully", user.LoginName, user.Id);

        return Result<AuthTokenResponse>.Success(new AuthTokenResponse
        {
            AccessToken            = accessToken,
            RefreshToken           = refreshToken,
            AccessTokenExpiresAt   = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes),
            RefreshTokenExpiresAt  = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays),
            UserId                 = user.Id,
            LoginName              = user.LoginName,
            UserType               = user.UserType.ToString(),
            DomainEntityId         = user.DomainEntityId,
            Roles                  = roles,
            Permissions            = permissions
        });
    }

    public async Task<Result<AuthTokenResponse>> RefreshAsync(
        string refreshToken, string? ipAddress, CancellationToken ct)
    {
        var tokenHash = tokenService.HashToken(refreshToken);

        var storedToken = await db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (storedToken is null)
            return Result<AuthTokenResponse>.Failure("Invalid refresh token", "INVALID_TOKEN");

        if (!storedToken.IsActive)
        {
            // Token reuse detected — revoke entire family
            if (storedToken.IsRevoked)
            {
                logger.LogWarning("Refresh token reuse detected for user {UserId} — revoking all tokens",
                    storedToken.UserId);
                await RevokeAllUserTokensAsync(storedToken.UserId, "Token reuse detected", ct);
            }
            return Result<AuthTokenResponse>.Failure("Refresh token is no longer valid", "TOKEN_EXPIRED");
        }

        var user        = storedToken.User;
        var roles       = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToList();

        var newAccessToken  = tokenService.GenerateAccessToken(user, roles, permissions);
        var newRefreshToken = tokenService.GenerateRefreshToken();
        var newTokenHash    = tokenService.HashToken(newRefreshToken);

        // Rotate: revoke old, issue new
        storedToken.IsRevoked = true;
        storedToken.RevokedReason = "Replaced by token rotation";
        storedToken.ReplacedByTokenHash = newTokenHash;

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId      = user.Id,
            TokenHash   = newTokenHash,
            ExpiresAt   = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays),
            CreatedByIp = ipAddress
        });

        await db.SaveChangesAsync(ct);

        return Result<AuthTokenResponse>.Success(new AuthTokenResponse
        {
            AccessToken           = newAccessToken,
            RefreshToken          = newRefreshToken,
            AccessTokenExpiresAt  = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays),
            UserId                = user.Id,
            LoginName             = user.LoginName,
            UserType              = user.UserType.ToString(),
            DomainEntityId        = user.DomainEntityId,
            Roles                 = roles,
            Permissions           = permissions
        });
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var tokenHash   = tokenService.HashToken(refreshToken);
        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (storedToken is null || !storedToken.IsActive)
            return Result.Success(); // idempotent

        storedToken.IsRevoked     = true;
        storedToken.RevokedReason = "User logout";
        await db.SaveChangesAsync(ct);

        logger.LogInformation("User {UserId} logged out", storedToken.UserId);
        return Result.Success();
    }

    public async Task<Result<UserInfoResponse>> GetCurrentUserAsync(int userId, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return Result<UserInfoResponse>.Failure("User not found", "NOT_FOUND");

        return Result<UserInfoResponse>.Success(MapToUserInfo(user));
    }

    private async Task RevokeAllUserTokensAsync(int userId, string reason, CancellationToken ct)
    {
        var tokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.IsRevoked     = true;
            token.RevokedReason = reason;
        }
        await db.SaveChangesAsync(ct);
    }

    private static UserInfoResponse MapToUserInfo(ApplicationUser user) => new()
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
