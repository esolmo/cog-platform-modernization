using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace LotteryService.Services;

/// <summary>
/// HTTP client interface for querying the accounts-service balance endpoint.
/// </summary>
public interface IAccountsClient
{
    Task<decimal> GetBalanceAsync(int customerId, CancellationToken ct = default);
}

public class AccountsClient(
    HttpClient http, IHttpContextAccessor httpContextAccessor, ILogger<AccountsClient> logger) : IAccountsClient
{
    public async Task<decimal> GetBalanceAsync(int customerId, CancellationToken ct = default)
    {
        try
        {
            // accounts-service's balance endpoint requires an authenticated caller
            // ([Authorize] at the controller level) — forward the current request's
            // own bearer token rather than calling anonymously, which would otherwise
            // always 401 and silently resolve to a $0 balance (see GetBalanceAsync's
            // catch-and-swallow below), blocking every real purchase.
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/customers/{customerId}/balance");
            var incomingAuth = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(incomingAuth) && AuthenticationHeaderValue.TryParse(incomingAuth, out var authHeader))
                request.Headers.Authorization = authHeader;

            var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<BalanceResponse>(ct);
            return body?.AvailableCredit ?? 0m;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve balance for customer {CustomerId}", customerId);
            return 0m;
        }
    }

    private record BalanceResponse(decimal CreditLimit, decimal CurrentBalance, decimal AvailableCredit);
}
