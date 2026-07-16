using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AdminService.Data;
using AdminService.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Xunit;

namespace AdminService.Tests.Integration;

[Collection("Admin Integration")]
public class AdminIntegrationTests : IAsyncLifetime
{
    private readonly AdminApiFactory _factory;
    private HttpClient _client = null!;

    public AdminIntegrationTests(AdminApiFactory factory)
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

    // ── GET /api/users ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_Returns200WithSeedData()
    {
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/users ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Username = "newadmin",
            Email = "newadmin@cog.test",
            Password = "Adm!n_P@ss1",
            FirstName = "New",
            LastName = "Admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_Returns409()
    {
        await _factory.SeedUserAsync("dupuser");

        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Username = "dupuser",
            Email = "dup2@cog.test",
            Password = "Adm!n_P@ss1",
            FirstName = "Dup",
            LastName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── GET /api/users/{id} ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUser_ExistingId_Returns200()
    {
        var userId = await _factory.SeedUserAsync("getuser");
        var response = await _client.GetAsync($"/api/users/{userId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUser_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/users/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/users/{id} ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_ValidRequest_Returns200()
    {
        var userId = await _factory.SeedUserAsync("updateme");
        var response = await _client.PutAsJsonAsync($"/api/users/{userId}", new
        {
            FirstName = "Updated",
            LastName = "Name"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/roles ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoles_Returns200WithSeededRoles()
    {
        var response = await _client.GetAsync("/api/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<List<RoleSummary>>();
        roles!.Should().HaveCountGreaterOrEqualTo(6); // seeded roles
    }

    // ── GET /api/config ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetConfig_Returns200()
    {
        var response = await _client.GetAsync("/api/config");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/audit-logs ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAuditLogs_Returns200()
    {
        var response = await _client.GetAsync("/api/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── /health ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record RoleSummary(int Id, string Name, bool IsSystemRole);
}

[CollectionDefinition("Admin Integration")]
public class AdminIntegrationCollection : ICollectionFixture<AdminApiFactory> { }

public class AdminApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss1")
        .Build();

    private int _userSeed;

    public async Task InitializeAsync() => await _sql.StartAsync();

    public new async Task DisposeAsync() => await _sql.DisposeAsync();

    private string GetTestConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "AdminServiceTest"
        };
        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AdminDbContext>));
            if (desc != null) services.Remove(desc);

            services.AddDbContext<AdminDbContext>(o => o.UseSqlServer(GetTestConnectionString()));
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
            new("login_name", "testadmin"),
            new("user_type", "Employee"),
            new("domain_id", "1"),
            new(ClaimTypes.Role, "Admin"),
            new("permission", "Users.Manage"),
            new("permission", "Roles.Manage"),
            new("permission", "System.Config"),
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
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public async Task<int> SeedUserAsync(string username)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var user = new ApplicationUser
        {
            Username = username,
            Email = $"{username}@test.cog",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test_P@ss1"),
            MaxAccessLevel = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }
}
