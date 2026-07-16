using System.Net.Http.Json;
using System.Text.Json;

namespace AccountsService.Services;

public interface IBettingProvisioningService
{
    Task ProvisionAgentAsync(
        string loginName, string? name, int? parentAgentId, int agentType,
        CancellationToken ct = default);

    Task ProvisionCustomerAsync(
        string loginName, string agentLoginName,
        string? email, string? phone,
        decimal maxStraightWager, decimal maxParlayWager,
        decimal maxTeaserWager, decimal maxIfBetWager,
        decimal creditLimit,
        CancellationToken ct = default);
}

public class BettingProvisioningService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<BettingProvisioningService> logger) : IBettingProvisioningService
{
    public Task ProvisionAgentAsync(
        string loginName, string? name, int? parentAgentId, int agentType,
        CancellationToken ct = default)
        => ProvisionAsync("/api/internal/agents", new
        {
            loginName,
            name,
            parentAgentId,
            agentType,
        }, loginName, "agent", ct);

    public Task ProvisionCustomerAsync(
        string loginName, string agentLoginName,
        string? email, string? phone,
        decimal maxStraightWager, decimal maxParlayWager,
        decimal maxTeaserWager, decimal maxIfBetWager,
        decimal creditLimit,
        CancellationToken ct = default)
        => ProvisionAsync("/api/internal/customers", new
        {
            loginName,
            agentLoginName,
            email,
            phone,
            maxStraightWager,
            maxParlayWager,
            maxTeaserWager,
            maxIfBetWager,
            creditLimit,
        }, loginName, "customer", ct);

    private async Task ProvisionAsync(string endpoint, object body, string identity, string entityType, CancellationToken ct)
    {
        var baseUrl = configuration["Services:BettingService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Services:BettingService:BaseUrl not configured. Skipping betting provisioning for {EntityType} {Identity}.",
                entityType, identity);
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("betting-service");

            var serviceUsername = configuration["Services:AuthService:ServiceAccount:Username"];
            var servicePassword = configuration["Services:AuthService:ServiceAccount:Password"];

            if (string.IsNullOrWhiteSpace(serviceUsername) || string.IsNullOrWhiteSpace(servicePassword))
            {
                logger.LogWarning("Auth service account credentials not configured. Skipping betting provisioning for {Identity}.", identity);
                return;
            }

            var authClient = httpClientFactory.CreateClient("auth-service");
            var loginResp = await authClient.PostAsJsonAsync("/api/auth/login",
                new { loginName = serviceUsername, password = servicePassword }, ct);

            if (!loginResp.IsSuccessStatusCode)
            {
                logger.LogWarning("Auth-service login failed ({Status}) for service account. Skipping betting provisioning for {Identity}.",
                    loginResp.StatusCode, identity);
                return;
            }

            var loginData  = await loginResp.Content.ReadFromJsonAsync<JsonElement>(ct);
            var accessToken = loginData.GetProperty("accessToken").GetString();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var resp = await client.PostAsJsonAsync(endpoint, body, ct);

            if (resp.IsSuccessStatusCode)
                logger.LogInformation("Provisioned {EntityType} {Identity} in betting-service.", entityType, identity);
            else
            {
                var bodyText = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Betting-service provisioning failed ({Status}) for {EntityType} {Identity}: {Body}",
                    resp.StatusCode, entityType, identity, bodyText);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception while provisioning {EntityType} {Identity} in betting-service. Created locally only.",
                entityType, identity);
        }
    }
}
