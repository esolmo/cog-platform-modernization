using System.Net.Http.Json;
using System.Text.Json;

namespace AdminService.Services;

public interface IAuthProvisioningService
{
    /// <summary>
    /// Creates a corresponding auth-service user so the new admin user can log in.
    /// Failures are non-fatal — the admin-service user is still created.
    /// </summary>
    Task ProvisionUserAsync(
        string username,
        string password,
        string? email,
        string? maxAccessLevel,
        IReadOnlyList<string> roleNames,
        CancellationToken ct = default);
}

public class AuthProvisioningService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AuthProvisioningService> logger) : IAuthProvisioningService
{
    public async Task ProvisionUserAsync(
        string username,
        string password,
        string? email,
        string? maxAccessLevel,
        IReadOnlyList<string> roleNames,
        CancellationToken ct = default)
    {
        var baseUrl = configuration["Services:AuthService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Services:AuthService:BaseUrl is not configured. Skipping auth provisioning for {Username}.", username);
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("auth-service");

            // 1. Authenticate with the service account to get a JWT
            var serviceUsername = configuration["Services:AuthService:ServiceAccount:Username"];
            var servicePassword = configuration["Services:AuthService:ServiceAccount:Password"];

            if (string.IsNullOrWhiteSpace(serviceUsername) || string.IsNullOrWhiteSpace(servicePassword))
            {
                logger.LogWarning("Auth service account credentials not configured. Skipping auth provisioning for {Username}.", username);
                return;
            }

            var loginResp = await client.PostAsJsonAsync("/api/auth/login",
                new { loginName = serviceUsername, password = servicePassword }, ct);

            if (!loginResp.IsSuccessStatusCode)
            {
                logger.LogWarning("Auth-service login failed ({Status}) for service account. Skipping provisioning for {Username}.",
                    loginResp.StatusCode, username);
                return;
            }

            var loginData = await loginResp.Content.ReadFromJsonAsync<JsonElement>(ct);
            var accessToken = loginData.GetProperty("accessToken").GetString();

            // 2. Create the user in auth-service
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var createResp = await client.PostAsJsonAsync("/api/users", new
            {
                loginName      = username,
                password       = password,
                email          = email,
                userType       = "Employee",
                maxLevel       = maxAccessLevel,
                roleNames      = roleNames,
                createdBy      = "admin-service"
            }, ct);

            if (createResp.IsSuccessStatusCode)
            {
                logger.LogInformation("Provisioned auth-service user for {Username}.", username);
            }
            else
            {
                var body = await createResp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Auth-service user creation failed ({Status}) for {Username}: {Body}",
                    createResp.StatusCode, username, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Exception while provisioning auth-service user for {Username}. User created locally only.", username);
        }
    }
}
