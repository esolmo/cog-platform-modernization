using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BettingService.Data;
using Cog.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace BettingService.Tests.Integration;

[Collection("Betting Integration")]
public class WagersIntegrationTests : IAsyncLifetime
{
    private readonly BettingApiFactory _factory;
    private HttpClient _client = null!;

    public WagersIntegrationTests(BettingApiFactory factory)
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

    // ── GET /api/wagers ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetWagers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/wagers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/wagers ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateWager_ValidStraightBet_Returns201()
    {
        var (gameId, periodId) = await _factory.SeedGameAsync();
        await _factory.SeedCustomerAsync(1);

        var response = await _client.PostAsJsonAsync("/api/wagers", new
        {
            CustomerId = 1,
            WagerType = 1, // Straight
            RiskAmount = 110m,
            Items = new[]
            {
                new { GamePeriodId = periodId, ItemType = 1, Side = 1 } // Spread, Home
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<WagerResponse>();
        body!.RiskAmount.Should().Be(110m);
        body.Status.Should().Be(1); // Pending
    }

    [Fact]
    public async Task CreateWager_IdempotentKey_ReturnsSameWager()
    {
        var (_, periodId) = await _factory.SeedGameAsync();
        await _factory.SeedCustomerAsync(2);

        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new
        {
            CustomerId = 2,
            WagerType = 1,
            RiskAmount = 55m,
            IdempotencyKey = idempotencyKey,
            Items = new[] { new { GamePeriodId = periodId, ItemType = 1, Side = 1 } }
        };

        var first = await _client.PostAsJsonAsync("/api/wagers", request);
        var second = await _client.PostAsJsonAsync("/api/wagers", request);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var w1 = await first.Content.ReadFromJsonAsync<WagerResponse>();
        var w2 = await second.Content.ReadFromJsonAsync<WagerResponse>();
        w1!.Id.Should().Be(w2!.Id);
    }

    [Fact]
    public async Task CreateWager_MissingItems_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/wagers", new
        {
            CustomerId = 1,
            WagerType = 1,
            RiskAmount = 100m,
            Items = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DELETE /api/wagers/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task CancelWager_ExistingPendingWager_Returns200()
    {
        var (_, periodId) = await _factory.SeedGameAsync();
        await _factory.SeedCustomerAsync(3);

        var createResp = await _client.PostAsJsonAsync("/api/wagers", new
        {
            CustomerId = 3,
            WagerType = 1,
            RiskAmount = 100m,
            Items = new[] { new { GamePeriodId = periodId, ItemType = 2, Side = 2 } }
        });
        var created = await createResp.Content.ReadFromJsonAsync<WagerResponse>();

        var cancel = await _client.DeleteAsync($"/api/wagers/{created!.Id}");
        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/games ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGames_ReturnsOk()
    {
        await _factory.SeedGameAsync();
        var response = await _client.GetAsync("/api/games");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── /health ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record WagerResponse(int Id, decimal RiskAmount, decimal WinAmount, int Status, int WagerType);
}

[CollectionDefinition("Betting Integration")]
public class BettingIntegrationCollection : ICollectionFixture<BettingApiFactory> { }

public class BettingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss1")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private int _agentIdSeed = 100;
    private int _gameSeed = 1;

    public async Task InitializeAsync() =>
        await Task.WhenAll(_sql.StartAsync(), _redis.StartAsync());

    public new async Task DisposeAsync() =>
        await Task.WhenAll(_sql.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());

    private string GetTestConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "BettingServiceTest"
        };
        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<BettingDbContext>));
            if (desc != null) services.Remove(desc);

            services.AddDbContext<BettingDbContext>(o => o.UseSqlServer(GetTestConnectionString()));

            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(_redis.GetConnectionString()));
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BettingDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        // Base agent required for all customer FKs — explicit Id requires IDENTITY_INSERT.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Agents] ON");
        db.Agents.Add(new Agent
        {
            Id = 1, LoginName = "baseagent", PasswordHash = "x",
            Name = "Base Agent", AgentType = AgentType.Master
        });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Agents] OFF");
        await transaction.CommitAsync();
    }

    public async Task<(int GameId, int PeriodId)> SeedGameAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BettingDbContext>();

        var gameId = Interlocked.Increment(ref _gameSeed) + 100;
        var sport = await db.SportTypes.FirstOrDefaultAsync()
                    ?? new SportType { Name = "Football", Code = "FB", DisplayOrder = 1 };
        if (sport.Id == 0) { db.SportTypes.Add(sport); await db.SaveChangesAsync(); }

        var game = new Game
        {
            Id = gameId, SportTypeId = sport.Id,
            HomeTeam = $"Home {gameId}", AwayTeam = $"Away {gameId}",
            GameDate = DateTime.UtcNow.AddDays(1), Status = GameStatus.Upcoming
        };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var period = new GamePeriod
        { GameId = gameId, PeriodDescription = "Full Game", PeriodNumber = 0 };
        db.GamePeriods.Add(period);
        await db.SaveChangesAsync();

        db.LineSets.Add(new LineSet
        {
            GamePeriodId = period.Id, Spread = -3m, SpreadJuice = -110m,
            HomeMoneyLine = -150m, AwayMoneyLine = 130m,
            Total = 45m, OverJuice = -110m, UnderJuice = -110m
        });
        await db.SaveChangesAsync();

        return (gameId, period.Id);
    }

    public async Task SeedCustomerAsync(int customerId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BettingDbContext>();
        if (await db.Customers.AnyAsync(c => c.Id == customerId)) return;

        db.Customers.Add(new Customer
        {
            Id = customerId, LoginName = $"cust{customerId}", PasswordHash = "x",
            FirstName = "Test", LastName = "Customer", AgentId = 1,
            Status = CustomerStatus.Active
        });
        db.CustomerBalances.Add(new CustomerBalance
        {
            CustomerId = customerId, Balance = 0, CreditLimit = 5000
        });
        db.CustomerLimits.Add(new CustomerLimits
        {
            CustomerId = customerId, MaxWagerStraight = 500, MaxWagerParlay = 500,
            MaxWagerTeaser = 500, MaxWagerIfBet = 500, MaxWagerReverse = 500,
            MinWager = 5, MaxWinPerTicket = 50000
        });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Mints a JWT signed with the same shared dev key/issuer/audience the service validates
    /// against (see appsettings.json "Jwt" section), matching the claim shape auth-service's
    /// TokenService issues. Production auth flows are covered by auth-service integration tests.
    /// </summary>
    public string GenerateTestJwt()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c="));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "1"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("login_name", "baseagent"),
            new("user_type", "Employee"),
            new("domain_id", "1"),
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "LinesManager"),
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
}
