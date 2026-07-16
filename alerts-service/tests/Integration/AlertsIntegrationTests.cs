using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AlertsService.Data;
using AlertsService.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace AlertsService.Tests.Integration;

[Collection("Alerts Integration")]
public class AlertsIntegrationTests : IAsyncLifetime
{
    private readonly AlertsApiFactory _factory;
    private HttpClient _client = null!;

    public AlertsIntegrationTests(AlertsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.GenerateTestJwt());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/alerts ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAlerts_Returns200()
    {
        var response = await _client.GetAsync("/api/alerts?agentId=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAlerts_ReturnsOnlyAgentAlerts()
    {
        await _factory.SeedAlertAsync(agentId: 1);
        await _factory.SeedAlertAsync(agentId: 1);
        await _factory.SeedAlertAsync(agentId: 2);

        var response = await _client.GetAsync("/api/alerts?agentId=1");
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertSummary>>();
        alerts!.Should().HaveCount(2);
        alerts.Should().AllSatisfy(a => a.AgentId.Should().Be(1));
    }

    // ── POST /api/alerts ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAlert_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/alerts", new
        {
            WagerNumber = 12345,
            AgentId = 10,
            CustomerId = 50,
            CustomerLoginName = "testcust",
            InetWagerNumber = "INET-001",
            WagerType = 1,
            AlertType = 1,
            Amount = 500m,
            Description = "Test alert",
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── DELETE /api/alerts/{id} ───────────────────────────────────────────────────

    [Fact]
    public async Task DismissAlert_ExistingAlert_Returns204()
    {
        var alertId = await _factory.SeedAlertAsync(agentId: 5);
        var response = await _client.DeleteAsync($"/api/alerts/{alertId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DismissAlert_UnknownId_Returns404()
    {
        var response = await _client.DeleteAsync("/api/alerts/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/alerts/vip/{agentId} ─────────────────────────────────────────────

    [Fact]
    public async Task GetVipSettings_Returns200()
    {
        var response = await _client.GetAsync("/api/alerts/vip/1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── SignalR hub ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignalR_CanConnect()
    {
        var hubUrl = new UriBuilder(_factory.Server.BaseAddress)
        {
            Path = "/hubs/alerts"
        }.Uri.ToString();

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(_factory.GenerateTestJwt());
            })
            .Build();

        await connection.StartAsync();
        connection.State.Should().Be(HubConnectionState.Connected);
        await connection.StopAsync();
    }

    // ── /health ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record AlertSummary(int Id, int AgentId, int WagerNumber, decimal Amount);
}

[CollectionDefinition("Alerts Integration")]
public class AlertsIntegrationCollection : ICollectionFixture<AlertsApiFactory> { }

public class AlertsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss1")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private int _alertSeed;

    public async Task InitializeAsync() =>
        await Task.WhenAll(_sql.StartAsync(), _redis.StartAsync());

    public new async Task DisposeAsync() =>
        await Task.WhenAll(_sql.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());

    private string GetTestConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "AlertsServiceTest"
        };
        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AlertsDbContext>));
            if (desc != null) services.Remove(desc);

            services.AddDbContext<AlertsDbContext>(o => o.UseSqlServer(GetTestConnectionString()));

            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(_redis.GetConnectionString()));

            services.AddSignalR().AddStackExchangeRedis(_redis.GetConnectionString());
        });
    }

    /// <summary>
    /// Mints a JWT signed with the same shared dev key/issuer/audience the service validates
    /// against (see appsettings.json "Jwt" section), matching the claim shape auth-service's
    /// TokenService issues.
    /// </summary>
    public string GenerateTestJwt()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c="));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "1"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("login_name", "testagent"),
            new("user_type", "Employee"),
            new("domain_id", "1"),
            new(ClaimTypes.Role, "Admin"),
        };

        var token = new JwtSecurityToken(
            issuer: "cog-auth-service",
            audience: "cog-services",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlertsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public async Task<int> SeedAlertAsync(int agentId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AlertsDbContext>();
        var n = Interlocked.Increment(ref _alertSeed);
        var ticket = new AlertTicket
        {
            WagerNumber = n,
            AgentId = agentId,
            CustomerId = 100 + n,
            CustomerLoginName = $"cust{n}",
            InetWagerNumber = $"INET-{n:D4}",
            WagerType = WagerType.Straight,
            AlertType = AlertType.Straight,
            Amount = 100m * n,
            Description = $"Alert {n}",
            InsertedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        db.AlertTickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket.Id;
    }
}
