using AdminService.Data;
using AdminService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Services;

public interface IAuditService
{
    Task LogAsync(
        int userId,
        string action,
        string entityType,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken ct = default);
}

public class AuditService(AdminDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task LogAsync(
        int userId,
        string action,
        string entityType,
        string entityId,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var resolvedIp = ipAddress
            ?? httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        var username = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Username)
            .FirstOrDefaultAsync(ct) ?? "system";

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = resolvedIp,
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }
}
