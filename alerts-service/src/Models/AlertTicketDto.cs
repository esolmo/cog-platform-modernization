namespace AlertsService.Models;

public record AlertTicketDto(
    int Id,
    int WagerNumber,
    int AgentId,
    string CustomerLoginName,
    string InetWagerNumber,
    string WagerType,
    decimal Amount,
    string Description,
    bool IsVipAlert,
    bool IsSharpAction,
    bool IsSquareAction,
    DateTime InsertedAt,
    IReadOnlyList<AttributeDto> Attributes,
    IReadOnlyList<AlertDetailDto> Details
);

public record AlertDetailDto(
    string Description,
    string SportKey,
    IReadOnlyList<AttributeDto> Attributes
);

public record AttributeDto(string Name, string Value);

public record AlertFilterRequest(
    int AgentId,
    int LastWagerNumber,
    string TypesFilter,
    bool ShowSharp,
    bool ShowSquare,
    string SportsFilter,
    decimal FilterAmount,
    bool RefreshVip,
    string? EmailAddress
);

public record BroadcastAlertRequest(
    string AgentLoginName,
    AlertTicketDto Alert
);

public record VipSettingsDto(
    IReadOnlyList<CustomerRefDto> Customers,
    string Email
);

public record CustomerRefDto(int Id, string LoginName);

public record UpdateVipRequest(
    int AgentId,
    IReadOnlyList<CustomerRefDto> Customers,
    string Email
);

public record CreateAlertRequest(
    int WagerNumber,
    int AgentId,
    int CustomerId,
    string CustomerLoginName,
    string InetWagerNumber,
    int WagerType,
    int AlertType,
    decimal Amount,
    string Description,
    DateTime ExpiresAt
);
