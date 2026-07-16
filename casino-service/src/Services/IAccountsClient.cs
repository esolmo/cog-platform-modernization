namespace CasinoService.Services;

/// <summary>
/// HTTP client for calling accounts-service to read/write COG customer balances
/// and to record casino fund transfers as ledger transactions.
/// </summary>
public interface IAccountsClient
{
    Task<decimal> GetAvailableBalanceAsync(string customerId, CancellationToken ct = default);
    Task<int> ReserveDocumentNumberAsync(string customerId, CancellationToken ct = default);
    Task RollbackDocumentAsync(string customerId, int documentNumber, CancellationToken ct = default);
    Task RecordDepositAsync(string customerId, int documentNumber, string transferReference, string remoteReference, decimal amount, CancellationToken ct = default);
    Task RecordWithdrawalAsync(string customerId, int documentNumber, string transferReference, string remoteReference, decimal amount, CancellationToken ct = default);
}
