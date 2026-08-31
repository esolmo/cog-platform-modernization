using System.Net.Http.Json;
using System.Text.Json;

namespace AccountsService.Services;

public interface IAuthProvisioningService
{
    /// <summary>
    /// Creates a corresponding auth-service login so a newly created agent can sign in.
    /// Failures are non-fatal — the accounts-service agent is still created; an operator
    /// can provision the login by hand later via auth-service's own user management.
    /// </summary>
    Task ProvisionAgentAsync(
        string loginName,
        string password,
        int domainEntityId,
        string roleName,
        CancellationToken ct = default);
}

public class AuthProvisioningService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AuthProvisioningService> logger) : IAuthProvisioningService
{
    public async Task ProvisionAgentAsync(
        string loginName,
        string password,
        int domainEntityId,
        string roleName,
        CancellationToken ct = default)
    {
        var baseUrl = configuration["Services:AuthService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Services:AuthService:BaseUrl is not configured. Skipping auth provisioning for agent {LoginName}.", loginName);
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("auth-service");

            var serviceUsername = configuration["Services:AuthService:ServiceAccount:Username"];
            var servicePassword = configuration["Services:AuthService:ServiceAccount:Password"];

            if (string.IsNullOrWhiteSpace(serviceUsername) || string.IsNullOrWhiteSpace(servicePassword))
            {
                logger.LogWarning("Auth service account credentials not configured. Skipping auth provisioning for agent {LoginName}.", loginName);
                return;
            }

            var loginResp = await client.PostAsJsonAsync("/api/auth/login",
                new { loginName = serviceUsername, password = servicePassword }, ct);

            if (!loginResp.IsSuccessStatusCode)
            {
                logger.LogWarning("Auth-service login failed ({Status}) for service account. Skipping provisioning for agent {LoginName}.",
                    loginResp.StatusCode, loginName);
                return;
            }

            var loginData   = await loginResp.Content.ReadFromJsonAsync<JsonElement>(ct);
            var accessToken = loginData.GetProperty("accessToken").GetString();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var createResp = await client.PostAsJsonAsync("/api/users", new
            {
                loginName      = loginName,
                password       = password,
                userType       = "Agent",
                domainEntityId = domainEntityId,
                roleNames      = new[] { roleName },
                createdBy      = "accounts-service"
            }, ct);

            if (createResp.IsSuccessStatusCode)
            {
                logger.LogInformation("Provisioned auth-service login for agent {LoginName}.", loginName);
            }
            else
            {
                var body = await createResp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Auth-service login creation failed ({Status}) for agent {LoginName}: {Body}",
                    createResp.StatusCode, loginName, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception while provisioning auth-service login for agent {LoginName}. Agent created locally only.", loginName);
        }
    }
}
