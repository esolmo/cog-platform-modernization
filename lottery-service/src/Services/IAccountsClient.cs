namespace LotteryService.Services;

/// <summary>
/// HTTP client interface for querying the accounts-service balance endpoint.
/// </summary>
public interface IAccountsClient
{
    Task<decimal> GetBalanceAsync(int customerId, CancellationToken ct = default);
}

public class AccountsClient(HttpClient http, ILogger<AccountsClient> logger) : IAccountsClient
{
    public async Task<decimal> GetBalanceAsync(int customerId, CancellationToken ct = default)
    {
        try
        {
            var response = await http.GetFromJsonAsync<BalanceResponse>(
                $"/api/customers/{customerId}/balance", ct);
            return response?.Balance ?? 0m;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve balance for customer {CustomerId}", customerId);
            return 0m;
        }
    }

    private record BalanceResponse(decimal Balance);
}
