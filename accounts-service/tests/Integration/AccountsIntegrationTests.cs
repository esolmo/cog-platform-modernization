using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AccountsService.Data;
using AccountsService.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Xunit;

namespace AccountsService.Tests.Integration;

[Collection("Accounts Integration")]
public class AccountsIntegrationTests : IAsyncLifetime
{
    private readonly AccountsApiFactory _factory;
    private HttpClient _client = null!;

    public AccountsIntegrationTests(AccountsApiFactory factory)
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

    // ── GET /api/customers/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetCustomer_ExistingId_Returns200()
    {
        var customerId = await _factory.SeedCustomerAsync();
        var response = await _client.GetAsync($"/api/customers/{customerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCustomer_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/customers/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/customers/{id}/balance ──────────────────────────────────────────

    [Fact]
    public async Task GetBalance_ExistingCustomer_Returns200WithBalance()
    {
        var customerId = await _factory.SeedCustomerAsync();
        var response = await _client.GetAsync($"/api/customers/{customerId}/balance");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<BalanceResponse>();
        body!.CreditLimit.Should().Be(1000m);
    }

    // ── POST /api/customers ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomer_ValidRequest_Returns201()
    {
        var agentId = await _factory.SeedAgentAsync();

        var response = await _client.PostAsJsonAsync("/api/customers", new
        {
            LoginName = "newcust",
            AgentId = agentId,
            CreditLimit = 500m,
            MaxStraightWager = 200m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateLoginName_Returns409()
    {
        var customerId = await _factory.SeedCustomerAsync(loginName: "duptest");
        var agentId = await _factory.SeedAgentAsync();

        var response = await _client.PostAsJsonAsync("/api/customers", new
        {
            LoginName = "duptest",
            AgentId = agentId,
            CreditLimit = 500m,
            MaxStraightWager = 200m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── POST /api/transactions ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTransaction_Deposit_Returns201AndUpdatesBalance()
    {
        var customerId = await _factory.SeedCustomerAsync();

        var response = await _client.PostAsJsonAsync("/api/transactions", new
        {
            CustomerId = customerId,
            Code = "Credit",
            Type = "Deposit",
            Amount = 250m,
            Description = "Test deposit",
            EnteredBy = "admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var balanceResp = await _client.GetAsync($"/api/customers/{customerId}/balance");
        var balance = await balanceResp.Content.ReadFromJsonAsync<BalanceResponse>();
        balance!.CurrentBalance.Should().Be(250m);
    }

    // ── GET /api/agents/{id}/customers ────────────────────────────────────────────

    [Fact]
    public async Task GetAgentCustomers_ReturnsOnlyThatAgentsCustomers()
    {
        var agent1 = await _factory.SeedAgentAsync();
        var agent2 = await _factory.SeedAgentAsync();
        await _factory.SeedCustomerAsync(agentId: agent1);
        await _factory.SeedCustomerAsync(agentId: agent1);
        await _factory.SeedCustomerAsync(agentId: agent2);

        var response = await _client.GetAsync($"/api/agents/{agent1}/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customers = await response.Content.ReadFromJsonAsync<List<CustomerSummary>>();
        customers!.Should().HaveCount(2);
        customers.Should().AllSatisfy(c => c.AgentId.Should().Be(agent1));
    }

    // ── /health ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record BalanceResponse(decimal CreditLimit, decimal CurrentBalance, decimal AvailableCredit);
    private record CustomerSummary(int Id, string LoginName, int AgentId);
}

[CollectionDefinition("Accounts Integration")]
public class AccountsIntegrationCollection : ICollectionFixture<AccountsApiFactory> { }

public class AccountsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss1")
        .Build();

    private int _agentSeed;
    private int _customerSeed;

    public async Task InitializeAsync() => await _sql.StartAsync();

    public new async Task DisposeAsync() => await _sql.DisposeAsync();

    private string GetTestConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "AccountsServiceTest"
        };
        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AccountsDbContext>));
            if (desc != null) services.Remove(desc);

            services.AddDbContext<AccountsDbContext>(o => o.UseSqlServer(GetTestConnectionString()));
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
            new(ClaimTypes.Role, "MasterAgent"),
            new(ClaimTypes.Role, "Agent"),
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
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public async Task<int> SeedAgentAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        var agent = new Agent
        {
            LoginName = $"agent{Interlocked.Increment(ref _agentSeed)}",
            AgentType = AgentType.Agent,
            CreditLimitMax = 100000m,
            WagerLimitMax = 10000m,
            CreatedBy = "seed"
        };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();
        return agent.Id;
    }

    public async Task<int> SeedCustomerAsync(string? loginName = null, int agentId = 0)
    {
        if (agentId == 0) agentId = await SeedAgentAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        var suffix = Interlocked.Increment(ref _customerSeed);
        var customer = new Customer
        {
            LoginName = loginName ?? $"cust{suffix}",
            AgentId = agentId,
            Status = CustomerStatus.Active,
            OddsFormat = OddsFormat.American,
            CreatedBy = "seed"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        db.Add(new CustomerBalance { CustomerId = customer.Id, CreditLimit = 1000m });
        db.Add(new CustomerLimits
        {
            CustomerId = customer.Id,
            MaxStraightWager = 500m, MaxParlayWager = 200m,
            MaxParlayPayout = 5000m, MaxTeaserWager = 200m,
            MaxIfBetWager = 200m, MaxLotteryPick3 = 100m,
            MaxLotteryPick4 = 100m, HardCreditLimit = 10000m
        });
        await db.SaveChangesAsync();
        return customer.Id;
    }
}
