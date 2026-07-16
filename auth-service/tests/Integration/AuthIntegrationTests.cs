using System.Net;
using System.Net.Http.Json;
using AuthService.Data;
using AuthService.Entities;
using AuthService.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace AuthService.Tests.Integration;

/// <summary>
/// Integration tests for auth-service using a real SQL Server + Redis via Testcontainers.
/// Each test class shares one container instance (collection fixture) to avoid
/// container-per-test startup overhead.
/// </summary>
[Collection("Auth Integration")]
public class AuthIntegrationTests : IAsyncLifetime
{
    private readonly AuthApiFactory _factory;
    private HttpClient _client = null!;

    public AuthIntegrationTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── /auth/login ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        await _factory.SeedUserAsync("agent1", "P@ssw0rd!", UserType.Agent);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            LoginName = "agent1",
            Password = "P@ssw0rd!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await _factory.SeedUserAsync("agent2", "correct!", UserType.Agent);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            LoginName = "agent2",
            Password = "wrong!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            LoginName = "nobody",
            Password = "anything"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InactiveUser_Returns401()
    {
        await _factory.SeedUserAsync("inactive1", "P@ss!", UserType.Agent, isActive: false);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            LoginName = "inactive1",
            Password = "P@ss!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── /auth/refresh ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        await _factory.SeedUserAsync("agent3", "P@ssw0rd!", UserType.Agent);

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { LoginName = "agent3", Password = "P@ssw0rd!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens!.RefreshToken });

        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await refreshResp.Content.ReadFromJsonAsync<LoginResponse>();
        newTokens!.AccessToken.Should().NotBe(tokens.AccessToken);
    }

    [Fact]
    public async Task Refresh_RevokedToken_Returns401()
    {
        await _factory.SeedUserAsync("agent4", "P@ssw0rd!", UserType.Agent);

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { LoginName = "agent4", Password = "P@ssw0rd!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        // Use refresh token once (rotates it, old one becomes invalid)
        await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens!.RefreshToken });

        // Re-use stale token
        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new { RefreshToken = tokens.RefreshToken });

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── /auth/me ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_AuthenticatedRequest_Returns200WithProfile()
    {
        await _factory.SeedUserAsync("agent5", "P@ssw0rd!", UserType.Agent);
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { LoginName = "agent5", Password = "P@ssw0rd!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var me = await _client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── /health ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// ── Test fixture ─────────────────────────────────────────────────────────────────

[CollectionDefinition("Auth Integration")]
public class AuthIntegrationCollection : ICollectionFixture<AuthApiFactory> { }

public class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss1")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sqlContainer.StartAsync(), _redisContainer.StartAsync());
    }

    public new async Task DisposeAsync()
    {
        await Task.WhenAll(_sqlContainer.DisposeAsync().AsTask(), _redisContainer.DisposeAsync().AsTask());
    }

    private string GetTestConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString())
        {
            InitialCatalog = "AuthServiceTest"
        };
        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Replace EF registration with test database
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AuthDbContext>(options =>
                options.UseSqlServer(GetTestConnectionString()));

            // Replace Redis with test instance
            services.Configure<StackExchange.Redis.ConfigurationOptions>(_ => { });
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public async Task SeedUserAsync(string loginName, string password,
        UserType userType, bool isActive = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        db.Users.Add(new ApplicationUser
        {
            LoginName = loginName,
            PasswordHash = passwordHash,
            UserType = userType,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    // DTO matching the actual controller response shape
    private record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}

// Alias so the integration test class can reference it
file record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
