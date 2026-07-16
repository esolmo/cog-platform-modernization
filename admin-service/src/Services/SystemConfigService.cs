using AdminService.Common;
using AdminService.Data;
using AdminService.Entities;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Services;

public interface ISystemConfigService
{
    Task<IReadOnlyList<SystemConfigDto>> GetAllAsync(string? category = null, CancellationToken ct = default);
    Task<SystemConfigDto?> GetAsync(string key, CancellationToken ct = default);
    Task<Result<SystemConfigDto>> UpsertAsync(UpsertConfigRequest request, int updatedByUserId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string key, int deletedByUserId, CancellationToken ct = default);
    Task<AuditLogPagedResult> GetAuditLogsAsync(int page, int pageSize, string? action = null, int? userId = null, CancellationToken ct = default);
}

public class SystemConfigService(
    AdminDbContext db,
    IAuditService auditService,
    ILogger<SystemConfigService> logger) : ISystemConfigService
{
    public async Task<IReadOnlyList<SystemConfigDto>> GetAllAsync(
        string? category = null, CancellationToken ct = default)
    {
        var query = db.SystemConfigurations.AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        var configs = await query.OrderBy(c => c.Category).ThenBy(c => c.Key).ToListAsync(ct);
        return configs.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<SystemConfigDto?> GetAsync(string key, CancellationToken ct = default)
    {
        var config = await db.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == key, ct);
        return config is null ? null : MapToDto(config);
    }

    public async Task<Result<SystemConfigDto>> UpsertAsync(
        UpsertConfigRequest request, int updatedByUserId, CancellationToken ct = default)
    {
        var existing = await db.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == request.Key, ct);
        string? oldValues = null;

        if (existing is null)
        {
            existing = new SystemConfiguration
            {
                Key = request.Key,
                Category = request.Category
            };
            db.SystemConfigurations.Add(existing);
        }
        else
        {
            oldValues = System.Text.Json.JsonSerializer.Serialize(new { existing.Value });
        }

        existing.Value = request.Value;
        existing.Description = request.Description;
        existing.IsEncrypted = request.IsEncrypted;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = updatedByUserId.ToString();

        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(updatedByUserId, "config.upsert", "SystemConfiguration", request.Key,
            oldValues: oldValues,
            newValues: System.Text.Json.JsonSerializer.Serialize(new { request.Key, value = request.IsEncrypted ? "[ENCRYPTED]" : request.Value }),
            ct: ct);

        logger.LogInformation("Config key {Key} updated by user {UserId}", request.Key, updatedByUserId);
        return Result<SystemConfigDto>.Success(MapToDto(existing));
    }

    public async Task<Result<bool>> DeleteAsync(string key, int deletedByUserId, CancellationToken ct = default)
    {
        var config = await db.SystemConfigurations.FirstOrDefaultAsync(c => c.Key == key, ct);
        if (config is null)
            return Result<bool>.Failure("Configuration key not found.", "NOT_FOUND");

        db.SystemConfigurations.Remove(config);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(deletedByUserId, "config.delete", "SystemConfiguration", key, ct: ct);
        return Result<bool>.Success(true);
    }

    public async Task<AuditLogPagedResult> GetAuditLogsAsync(
        int page, int pageSize, string? action = null, int? userId = null, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new AuditLogPagedResult(
            items.Select(a => new AuditLogDto(
                a.Id, a.UserId, a.Username, a.Action, a.EntityType, a.EntityId,
                a.OldValues, a.NewValues, a.IpAddress, a.OccurredAt))
            .ToList()
            .AsReadOnly(),
            total, page, pageSize, totalPages);
    }

    private static SystemConfigDto MapToDto(SystemConfiguration c) =>
        new(c.Id, c.Key, c.IsEncrypted ? "[ENCRYPTED]" : c.Value, c.Category, c.Description, c.IsEncrypted, c.UpdatedAt);
}
