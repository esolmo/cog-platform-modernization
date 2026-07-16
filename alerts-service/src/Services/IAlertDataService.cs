using AlertsService.Models;

namespace AlertsService.Services;

public interface IAlertDataService
{
    Task<IReadOnlyList<AlertTicketDto>> GetActionAlertsAsync(AlertFilterRequest filter, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerRefDto>> GetCustomersAsync(int agentId, CancellationToken ct = default);
    Task<VipSettingsDto> GetVipSettingsAsync(int agentId, CancellationToken ct = default);
    Task UpdateVipSettingsAsync(UpdateVipRequest request, CancellationToken ct = default);
    Task<string> GetAgentEmailAsync(int agentId, CancellationToken ct = default);
    Task<AlertTicketDto> CreateAlertAsync(CreateAlertRequest request, CancellationToken ct = default);
    Task<bool> DismissAlertAsync(int id, CancellationToken ct = default);
}
