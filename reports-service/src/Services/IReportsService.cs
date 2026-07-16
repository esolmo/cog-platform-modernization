using ReportsService.Models;

namespace ReportsService.Services;

public interface IReportsService
{
    Task<List<WagerActivityRow>> GetWagerActivityAsync(WagerActivityRequest request, CancellationToken ct = default);
    Task<List<ChangedTransactionRow>> GetChangedTransactionsAsync(ChangedTransactionsRequest request, CancellationToken ct = default);
    Task<List<AgentRow>> SearchAgentsAsync(AgentSearchRequest request, CancellationToken ct = default);
    Task<List<CustomerRow>> SearchCustomersAsync(AgentSearchRequest request, CancellationToken ct = default);
    Task<List<PackageTrackerRow>> GetPackageTrackerAsync(PackageTrackerRequest request, CancellationToken ct = default);
}
