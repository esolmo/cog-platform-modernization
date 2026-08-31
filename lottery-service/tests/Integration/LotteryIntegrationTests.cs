using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LotteryService.Data;
using LotteryService.Entities;
using LotteryService.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Xunit;

namespace LotteryService.Tests.Integration;

[Collection("Lottery Integration")]
public class LotteryIntegrationTests : IAsyncLifetime
{
    private readonly LotteryApiFactory _factory;
    private HttpClient _client = null!;

    public LotteryIntegrationTests(LotteryApiFactory factory)
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

    // ── GET /api/lottery/games ────────────────────────────────────────────────────

    [Fact]
    public async Task GetGames_Returns200WithSeededGames()
    {
        var response = await _client.GetAsync("/api/lottery/games");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var games = await response.Content.ReadFromJsonAsync<List<GameSummary>>();
        games!.Should().HaveCountGreaterOrEqualTo(2); // Pick3 + Pick4 from seed
    }

    // ── GET /api/lottery/games/{id}/drawings ──────────────────────────────────────

    [Fact]
    public async Task GetOpenDrawings_Returns200()
    {
        await _factory.SeedDrawingAsync(LotteryGameType.Pick3);
        var response = await _client.GetAsync("/api/lottery/games/1/drawings");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var drawings = await response.Content.ReadFromJsonAsync<List<DrawingSummary>>();
        drawings!.Should().NotBeEmpty();
    }

    // ── POST /api/lottery/tickets ─────────────────────────────────────────────────

    [Fact]
    public async Task PurchaseTicket_StraightPick3_Returns201()
    {
        var drawingId = await _factory.SeedDrawingAsync(LotteryGameType.Pick3);

        var response = await _client.PostAsJsonAsync("/api/lottery/tickets", new
        {
            DrawingDetailId = drawingId,
            DateToPlay = DateTime.UtcNow.Date,
            PickType = 1, // Straight
            Picks = new[]
            {
                new { Number1 = 1, Number2 = 2, Number3 = 3, Amount = 1m }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        ticket!.Picks.Should().HaveCount(1); // straight = 1 entry per pick
    }

    [Fact]
    public async Task PurchaseTicket_BoxedPick3_ExpandsToSixPermutations()
    {
        var drawingId = await _factory.SeedDrawingAsync(LotteryGameType.Pick3);

        var response = await _client.PostAsJsonAsync("/api/lottery/tickets", new
        {
            DrawingDetailId = drawingId,
            DateToPlay = DateTime.UtcNow.Date,
            PickType = 2, // Boxed
            Picks = new[]
            {
                new { Number1 = 1, Number2 = 2, Number3 = 3, Amount = 6m }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        ticket!.Picks.Should().HaveCount(6); // 3! = 6 permutations
        ticket.Picks.Should().AllSatisfy(p => p.Amount.Should().Be(1m)); // 6 / 6
    }

    [Fact]
    public async Task PurchaseTicket_BoxedPick4_Expands24Permutations()
    {
        var drawingId = await _factory.SeedDrawingAsync(LotteryGameType.Pick4);

        var response = await _client.PostAsJsonAsync("/api/lottery/tickets", new
        {
            DrawingDetailId = drawingId,
            DateToPlay = DateTime.UtcNow.Date,
            PickType = 2, // Boxed
            Picks = new[]
            {
                new { Number1 = 1, Number2 = 2, Number3 = 3, Number4 = 4, Amount = 24m }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        ticket!.Picks.Should().HaveCount(24); // 4! = 24 permutations
    }

    [Fact]
    public async Task PurchaseTicket_ClosedDrawing_Returns422()
    {
        var drawingId = await _factory.SeedDrawingAsync(LotteryGameType.Pick3, isOpen: false);

        var response = await _client.PostAsJsonAsync("/api/lottery/tickets", new
        {
            DrawingDetailId = drawingId,
            DateToPlay = DateTime.UtcNow.Date,
            PickType = 1,
            Picks = new[] { new { Number1 = 5, Number2 = 5, Number3 = 5, Amount = 1m } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── GET /api/lottery/tickets/my ──────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerTickets_Returns200()
    {
        var drawingId = await _factory.SeedDrawingAsync(LotteryGameType.Pick3);
        await _client.PostAsJsonAsync("/api/lottery/tickets", new
        {
            DrawingDetailId = drawingId, DateToPlay = DateTime.UtcNow.Date, PickType = 1,
            Picks = new[] { new { Number1 = 7, Number2 = 7, Number3 = 7, Amount = 2m } }
        });

        var response = await _client.GetAsync("/api/lottery/tickets/my");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketSummary>>();
        tickets!.Should().HaveCount(1);
    }

    // ── /health ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record GameSummary(int Id, string Name, string GameType);
    private record DrawingSummary(int Id, string Name, bool IsOpen, DateTime DrawingTime);
    private record PickEntry(int Number1, int Number2, int Number3, int Number4, decimal Amount);
    private record TicketResponse(int Id, decimal Total, List<PickEntry> Picks);
    private record TicketSummary(int Id, decimal Total);
}

[CollectionDefinition("Lottery Integration")]
public class LotteryIntegrationCollection : ICollectionFixture<LotteryApiFactory> { }

public class LotteryApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss1")
        .Build();

    private int _drawingSeed;

    public async Task InitializeAsync() => await _sql.StartAsync();

    public new async Task DisposeAsync() => await _sql.DisposeAsync();

    private string GetTestConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "LotteryServiceTest"
        };
        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<LotteryDbContext>));
            if (desc != null) services.Remove(desc);

            services.AddDbContext<LotteryDbContext>(o => o.UseSqlServer(GetTestConnectionString()));

            // Avoid a real HTTP dependency on accounts-service in isolated integration tests.
            services.RemoveAll<IAccountsClient>();
            services.AddSingleton<IAccountsClient>(new StubAccountsClient());
        });
    }

    private class StubAccountsClient : IAccountsClient
    {
        public Task<decimal> GetBalanceAsync(int customerId, CancellationToken ct = default) =>
            Task.FromResult(10000m);
    }

    /// <summary>
    /// Mints a JWT signed with the same shared dev key/issuer/audience the service validates
    /// against (see appsettings.json "Jwt" section), matching the claim shape auth-service's
    /// TokenService issues, plus the "customerId"/"agentId" claims TicketsController requires.
    /// </summary>
    public string GenerateTestJwt()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c="));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "1"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("login_name", "testcustomer"),
            new("user_type", "Customer"),
            new("domain_id", "1"),
            new("customerId", "1"),
            new("agentId", "1"),
            new(ClaimTypes.Role, "Customer"),
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
        var db = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync(); // runs seed: Pick3 (Id=1) + Pick4 (Id=2)
    }

    public async Task<long> SeedDrawingAsync(LotteryGameType gameType, bool isOpen = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();
        var gameId = gameType == LotteryGameType.Pick3 ? 1 : 2;
        var n = Interlocked.Increment(ref _drawingSeed);
        var now = DateTime.UtcNow;
        var drawing = new DrawingDetail
        {
            LotteryGameId = gameId,
            Name = $"{gameType} Drawing {n}",
            // Purchasing requires DrawingDate > now (see LotteryGameService.PurchaseTicketAsync)
            DrawingDate = isOpen ? now.AddHours(4) : now.AddHours(-1),
            TimeZoneId = "UTC",
            MinutesToDraw = 60,
            IsActive = true
        };
        db.DrawingDetails.Add(drawing);
        await db.SaveChangesAsync();
        return drawing.Id;
    }
}
