using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CasinoService.Data;
using CasinoService.Entities;
using CasinoService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;
using Xunit;

namespace CasinoService.Tests.Integration;

/// <summary>
/// Integration tests for casino-service using a real SQL Server via Testcontainers.
/// ILiveDealerClient and IAccountsClient are replaced with in-test stubs so these tests
/// don't depend on the real external Live Dealer XML API or a live accounts-service.
/// </summary>
[Collection("Casino Integration")]
public class CasinoIntegrationTests : IAsyncLifetime
{
    private const string CustomerId = "1001";

    private readonly CasinoApiFactory _factory;
    private HttpClient _client = null!;

    public CasinoIntegrationTests(CasinoApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _factory.Dealer.Reset();
        _factory.Accounts.Reset();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.GenerateTestJwt(CustomerId));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── /health ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_Returns200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── auth ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NoBearerToken_Returns401()
    {
        using var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.PostAsJsonAsync("/api/casino/register",
            new { Nickname = "Anon", IpAddress = "127.0.0.1" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/casino/register ───────────────────────────────────────────────

    [Fact]
    public async Task Register_NewPlayer_Returns200AndPersistsPlayer()
    {
        _factory.Dealer.AddPlayerResult = new LiveDealerPlayerResult(
            Success: true, CustId: CustomerId, Nickname: "Tester", Ticket: "tok-1",
            ExternalPlayerId: "ext-1001", Balance: 0, BonusBalance: 0, Playthrough: 0,
            ErrorCode: null, ErrorDescription: null);

        var response = await _client.PostAsJsonAsync("/api/casino/register",
            new { Nickname = "Tester", IpAddress = "127.0.0.1" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();
        player!.Nickname.Should().Be("Tester");
        player.ExternalPlayerId.Should().Be("ext-1001");

        (await _factory.CountPlayersAsync(CustomerId)).Should().Be(1);
    }

    [Fact]
    public async Task Register_AlreadyRegistered_Returns400()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Existing", "ext-1001");

        var response = await _client.PostAsJsonAsync("/api/casino/register",
            new { Nickname = "Duplicate", IpAddress = "127.0.0.1" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        body!.Code.Should().Be("ALREADY_REGISTERED");
    }

    [Fact]
    public async Task Register_ExternalProviderRejects_Returns400AndDoesNotPersist()
    {
        _factory.Dealer.AddPlayerResult = new LiveDealerPlayerResult(
            Success: false, CustId: null, Nickname: null, Ticket: null, ExternalPlayerId: null,
            Balance: 0, BonusBalance: 0, Playthrough: 0,
            ErrorCode: "201", ErrorDescription: "Display name already exists");

        var response = await _client.PostAsJsonAsync("/api/casino/register",
            new { Nickname = "Taken", IpAddress = "127.0.0.1" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        body!.Code.Should().Be("201");
        (await _factory.CountPlayersAsync(CustomerId)).Should().Be(0);
    }

    // ── GET /api/casino/session ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSession_NotRegistered_Returns404()
    {
        var response = await _client.GetAsync("/api/casino/session");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSession_RegisteredPlayer_Returns200WithLobbyUrl()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Tester", "ext-1001");
        _factory.Dealer.LoginResult = new LiveDealerPlayerResult(
            Success: true, CustId: CustomerId, Nickname: "Tester", Ticket: "ticket-abc",
            ExternalPlayerId: "ext-1001", Balance: 100m, BonusBalance: 5m, Playthrough: 0,
            ErrorCode: null, ErrorDescription: null);

        var response = await _client.GetAsync("/api/casino/session");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var session = await response.Content.ReadFromJsonAsync<SessionResponse>();
        session!.LobbyUrl.Should().Contain("ticket-abc");
        session.CasinoBalance.Should().Be(100m);
        session.BonusBalance.Should().Be(5m);
    }

    // ── GET /api/casino/balance ─────────────────────────────────────────────────

    [Fact]
    public async Task GetBalance_NotRegistered_Returns400()
    {
        var response = await _client.GetAsync("/api/casino/balance");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBalance_RegisteredPlayer_Returns200WithBothBalances()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Tester", "ext-1001");
        _factory.Accounts.AvailableBalance = 250m;
        _factory.Dealer.BalanceResult = new LiveDealerBalanceResult(
            Success: true, Balance: 75m, BonusBalance: 10m, ErrorCode: null, ErrorDescription: null);

        var response = await _client.GetAsync("/api/casino/balance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();
        balance!.AvailableBalance.Should().Be(250m);
        balance.CasinoBalance.Should().Be(75m);
        balance.BonusBalance.Should().Be(10m);
    }

    // ── POST /api/casino/deposit ────────────────────────────────────────────────

    [Fact]
    public async Task Deposit_NotRegistered_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/casino/deposit", new { Amount = 50m });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Deposit_InsufficientFunds_Returns400()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Tester", "ext-1001");
        _factory.Accounts.AvailableBalance = 20m;

        var response = await _client.PostAsJsonAsync("/api/casino/deposit", new { Amount = 100m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        body!.Code.Should().Be("INSUFFICIENT_FUNDS");
    }

    [Fact]
    public async Task Deposit_Success_Returns200AndPersistsCompletedTransaction()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Tester", "ext-1001");
        _factory.Accounts.AvailableBalance = 500m;
        _factory.ConfigureSuccessfulTransfer(remainingAvailable: 400m, remainingCasinoBalance: 100m);

        var response = await _client.PostAsJsonAsync("/api/casino/deposit", new { Amount = 100m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await response.Content.ReadFromJsonAsync<TransferResponseDto>();
        transfer!.Success.Should().BeTrue();
        transfer.AvailableBalance.Should().Be(400m);
        transfer.CasinoBalance.Should().Be(100m);

        var tx = await _factory.GetLatestTransactionAsync(CustomerId);
        tx.Should().NotBeNull();
        tx!.TransactionType.Should().Be(CasinoTransactionType.Deposit);
        tx.Status.Should().Be(CasinoTransactionStatus.Completed);
        tx.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task Deposit_ExternalInitFails_RollsBackDocumentAndReturns400()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Tester", "ext-1001");
        _factory.Accounts.AvailableBalance = 500m;
        _factory.Accounts.DocumentNumberToReserve = 42;
        _factory.Dealer.InitTransferResult = new LiveDealerTransferResult(
            Success: false, TransferReference: null, RemoteReference: null,
            ErrorCode: "500", ErrorDescription: "Provider unavailable");

        var response = await _client.PostAsJsonAsync("/api/casino/deposit", new { Amount = 50m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.Accounts.RolledBackDocumentNumbers.Should().Contain(42);
        (await _factory.GetLatestTransactionAsync(CustomerId)).Should().BeNull(
            "no transaction should be persisted when the provider rejects the transfer");
    }

    [Fact]
    public async Task Deposit_ZeroAmount_Returns400ValidationError()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Tester", "ext-1001");

        var response = await _client.PostAsJsonAsync("/api/casino/deposit", new { Amount = 0m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/casino/withdraw ───────────────────────────────────────────────

    [Fact]
    public async Task Withdraw_Success_Returns200AndPersistsCompletedTransaction()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Tester", "ext-1001");
        _factory.ConfigureSuccessfulTransfer(remainingAvailable: 150m, remainingCasinoBalance: 0m);

        var response = await _client.PostAsJsonAsync("/api/casino/withdraw", new { Amount = 100m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var transfer = await response.Content.ReadFromJsonAsync<TransferResponseDto>();
        transfer!.Success.Should().BeTrue();

        var tx = await _factory.GetLatestTransactionAsync(CustomerId);
        tx!.TransactionType.Should().Be(CasinoTransactionType.Withdrawal);
        tx.Status.Should().Be(CasinoTransactionStatus.Completed);
    }

    [Fact]
    public async Task Withdraw_ExternalConfirmFails_RollsBackDocument()
    {
        await _factory.SeedPlayerAsync(CustomerId, "Tester", "ext-1001");
        _factory.Accounts.DocumentNumberToReserve = 77;
        _factory.Dealer.InitTransferResult = new LiveDealerTransferResult(
            Success: true, TransferReference: "tr-1", RemoteReference: "rr-1",
            ErrorCode: null, ErrorDescription: null);
        _factory.Dealer.ConfirmTransferResult = new LiveDealerTransferResult(
            Success: false, TransferReference: null, RemoteReference: null,
            ErrorCode: "502", ErrorDescription: "Confirm timed out");

        var response = await _client.PostAsJsonAsync("/api/casino/withdraw", new { Amount = 30m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.Accounts.RolledBackDocumentNumbers.Should().Contain(77);
    }

    // ── DTOs matching the actual controller response shapes ───────────────────────

    private record PlayerResponse(string CustomerId, string Nickname, string ExternalPlayerId, DateTime RegisteredAt);
    private record SessionResponse(string CustomerId, string Nickname, string LobbyUrl, decimal CasinoBalance, decimal BonusBalance, decimal Playthrough);
    private record BalanceResponse(decimal AvailableBalance, decimal CasinoBalance, decimal BonusBalance);
    private record TransferResponseDto(bool Success, decimal AvailableBalance, decimal CasinoBalance, string? ErrorMessage);
    private record ErrorBody(string Error, string Code);
}

[CollectionDefinition("Casino Integration")]
public class CasinoIntegrationCollection : ICollectionFixture<CasinoApiFactory> { }

public class CasinoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DevJwtSecretKey = "6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c=";

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss1")
        .Build();

    public StubLiveDealerClient Dealer { get; } = new();
    public StubAccountsClient   Accounts { get; } = new();

    public async Task InitializeAsync() => await _sql.StartAsync();

    public new async Task DisposeAsync() => await _sql.DisposeAsync();

    private string GetTestConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "CasinoServiceTest"
        };
        return builder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<CasinoDbContext>));
            if (desc != null) services.Remove(desc);

            services.AddDbContext<CasinoDbContext>(o => o.UseSqlServer(GetTestConnectionString()));

            services.RemoveAll<ILiveDealerClient>();
            services.AddSingleton<ILiveDealerClient>(Dealer);

            services.RemoveAll<IAccountsClient>();
            services.AddSingleton<IAccountsClient>(Accounts);
        });
    }

    /// <summary>
    /// Mints a JWT matching the claim shape auth-service's TokenService issues, with the
    /// "domain_id" claim CasinoController.GetCustomerId() reads.
    /// </summary>
    public string GenerateTestJwt(string customerId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevJwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customerId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("login_name", $"customer{customerId}"),
            new("user_type", "Customer"),
            new("domain_id", customerId),
            new(ClaimTypes.Role, "CustomerWeb"),
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
        var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public async Task SeedPlayerAsync(string customerId, string nickname, string externalPlayerId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
        db.CasinoPlayers.Add(new CasinoPlayer
        {
            CustomerId = customerId,
            Nickname = nickname,
            ExternalPlayerId = externalPlayerId,
            CasinoId = 2
        });
        await db.SaveChangesAsync();
    }

    public async Task<int> CountPlayersAsync(string customerId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
        return await db.CasinoPlayers.CountAsync(p => p.CustomerId == customerId);
    }

    public async Task<CasinoTransaction?> GetLatestTransactionAsync(string customerId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
        return await db.CasinoTransactions
            .Include(t => t.Player)
            .Where(t => t.Player.CustomerId == customerId)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>Wires both stubs to complete a deposit/withdraw's full init→confirm flow successfully.</summary>
    public void ConfigureSuccessfulTransfer(decimal remainingAvailable, decimal remainingCasinoBalance)
    {
        Accounts.DocumentNumberToReserve = 1;
        Accounts.AvailableBalance = remainingAvailable;
        Dealer.InitTransferResult = new LiveDealerTransferResult(
            Success: true, TransferReference: "tr-ok", RemoteReference: "rr-ok",
            ErrorCode: null, ErrorDescription: null);
        Dealer.ConfirmTransferResult = new LiveDealerTransferResult(
            Success: true, TransferReference: "tr-ok", RemoteReference: "rr-ok",
            ErrorCode: null, ErrorDescription: null);
        Dealer.BalanceResult = new LiveDealerBalanceResult(
            Success: true, Balance: remainingCasinoBalance, BonusBalance: 0, ErrorCode: null, ErrorDescription: null);
        // AvailableBalance is re-read after the transfer completes — set it to the post-transfer value.
        Accounts.AvailableBalance = remainingAvailable;
    }

    public class StubLiveDealerClient : ILiveDealerClient
    {
        public LiveDealerPlayerResult AddPlayerResult { get; set; } = SuccessPlayer();
        public LiveDealerPlayerResult LoginResult { get; set; } = SuccessPlayer();
        public LiveDealerBalanceResult BalanceResult { get; set; } =
            new(true, 0, 0, null, null);
        public LiveDealerTransferResult InitTransferResult { get; set; } =
            new(true, "tr", "rr", null, null);
        public LiveDealerTransferResult ConfirmTransferResult { get; set; } =
            new(true, "tr", "rr", null, null);

        private static LiveDealerPlayerResult SuccessPlayer() =>
            new(true, "cust", "nick", "ticket", "ext", 0, 0, 0, null, null);

        public void Reset()
        {
            AddPlayerResult = SuccessPlayer();
            LoginResult = SuccessPlayer();
            BalanceResult = new LiveDealerBalanceResult(true, 0, 0, null, null);
            InitTransferResult = new LiveDealerTransferResult(true, "tr", "rr", null, null);
            ConfirmTransferResult = new LiveDealerTransferResult(true, "tr", "rr", null, null);
        }

        public Task<LiveDealerPlayerResult> AddPlayerAsync(
            string custId, string nickname, string countryCode, string ipAddress, string custSource,
            CancellationToken ct = default) => Task.FromResult(AddPlayerResult);

        public Task<LiveDealerPlayerResult> LoginAsync(
            string custId, string nickname, string ipAddress,
            CancellationToken ct = default) => Task.FromResult(LoginResult);

        public Task<LiveDealerBalanceResult> GetBalanceAsync(
            string custId, string nickname,
            CancellationToken ct = default) => Task.FromResult(BalanceResult);

        public Task<LiveDealerTransferResult> InitTransferAsync(
            string custId, decimal amount, string transferAction,
            int transferReference, string description,
            CancellationToken ct = default) => Task.FromResult(InitTransferResult);

        public Task<LiveDealerTransferResult> ConfirmTransferAsync(
            string custId, string transferAction,
            string transferReference, string remoteReference,
            bool includeConfirmation = false,
            CancellationToken ct = default) => Task.FromResult(ConfirmTransferResult);
    }

    public class StubAccountsClient : IAccountsClient
    {
        public decimal AvailableBalance { get; set; } = 1000m;
        public int DocumentNumberToReserve { get; set; } = 1;
        public List<int> RolledBackDocumentNumbers { get; } = [];

        public void Reset()
        {
            AvailableBalance = 1000m;
            DocumentNumberToReserve = 1;
            RolledBackDocumentNumbers.Clear();
        }

        public Task<decimal> GetAvailableBalanceAsync(string customerId, CancellationToken ct = default) =>
            Task.FromResult(AvailableBalance);

        public Task<int> ReserveDocumentNumberAsync(string customerId, CancellationToken ct = default) =>
            Task.FromResult(DocumentNumberToReserve);

        public Task RollbackDocumentAsync(string customerId, int documentNumber, CancellationToken ct = default)
        {
            RolledBackDocumentNumbers.Add(documentNumber);
            return Task.CompletedTask;
        }

        public Task RecordDepositAsync(string customerId, int documentNumber, string transferReference,
            string remoteReference, decimal amount, CancellationToken ct = default) => Task.CompletedTask;

        public Task RecordWithdrawalAsync(string customerId, int documentNumber, string transferReference,
            string remoteReference, decimal amount, CancellationToken ct = default) => Task.CompletedTask;
    }
}
