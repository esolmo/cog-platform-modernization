namespace AdminService.Models;

public record SystemConfigDto(
    int Id,
    string Key,
    string Value,
    string Category,
    string Description,
    bool IsEncrypted,
    DateTime UpdatedAt
);

public record UpsertConfigRequest(
    string Key,
    string Value,
    string Category,
    string Description,
    bool IsEncrypted
);

public record AuditLogDto(
    long Id,
    int UserId,
    string Username,
    string Action,
    string EntityType,
    string EntityId,
    string? OldValues,
    string? NewValues,
    string IpAddress,
    DateTime OccurredAt
);

public record AuditLogPagedResult(
    IReadOnlyList<AuditLogDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
