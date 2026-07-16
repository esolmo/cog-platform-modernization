using System.Net.Http.Json;

namespace CasinoService.Services;

public class AccountsHttpClient(HttpClient httpClient, ILogger<AccountsHttpClient> logger) : IAccountsClient
{
    public async Task<decimal> GetAvailableBalanceAsync(string customerId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<BalanceDto>(
                $"api/customers/{customerId}/balance", ct);
            return response?.AvailableBalance ?? 0m;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AccountsClient] GetAvailableBalance failed for {CustomerId}", customerId);
            return 0m;
        }
    }

    public async Task<int> ReserveDocumentNumberAsync(string customerId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync(
                $"api/customers/{customerId}/casino/reserve-document", null, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<DocNumDto>(cancellationToken: ct);
            return result?.DocumentNumber ?? -1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AccountsClient] ReserveDocumentNumber failed for {CustomerId}", customerId);
            return -1;
        }
    }

    public async Task RollbackDocumentAsync(string customerId, int documentNumber, CancellationToken ct = default)
    {
        try
        {
            await httpClient.PostAsync(
                $"api/customers/{customerId}/casino/rollback/{documentNumber}", null, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AccountsClient] RollbackDocument failed for {CustomerId}, doc={Doc}", customerId, documentNumber);
        }
    }

    public async Task RecordDepositAsync(string customerId, int documentNumber, string transferReference, string remoteReference, decimal amount, CancellationToken ct = default)
    {
        try
        {
            await httpClient.PostAsJsonAsync($"api/customers/{customerId}/casino/deposit", new
            {
                documentNumber, transferReference, remoteReference, amount
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AccountsClient] RecordDeposit failed for {CustomerId}", customerId);
        }
    }

    public async Task RecordWithdrawalAsync(string customerId, int documentNumber, string transferReference, string remoteReference, decimal amount, CancellationToken ct = default)
    {
        try
        {
            await httpClient.PostAsJsonAsync($"api/customers/{customerId}/casino/withdrawal", new
            {
                documentNumber, transferReference, remoteReference, amount
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AccountsClient] RecordWithdrawal failed for {CustomerId}", customerId);
        }
    }

    private record BalanceDto(decimal AvailableBalance);
    private record DocNumDto(int DocumentNumber);
}
