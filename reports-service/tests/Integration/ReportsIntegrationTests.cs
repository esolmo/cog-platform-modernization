using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using ReportsService.Models;
using Testcontainers.MsSql;
using Xunit;

namespace ReportsService.Tests.Integration;

/// <summary>
/// Integration tests for reports-service using a real SQL Server via Testcontainers.
///
/// Unlike every other service, reports-service (see ReportsDataService) issues raw,
/// hand-written SQL and calls legacy stored procedures (dbo.rptBetMakerActivity,
/// ICBBSearchAgent, ICBBSearchCust, dbo.rptPackageTracker, dbo.fn_GetSubAgentHierarchyByID)
/// directly against the ORIGINAL 2014-era COGDB schema — not any of the new EF-migrated
/// per-service databases. That full legacy schema (thousands of tables/procs/functions
/// under the repo's top-level Sps/ folder) is far too large to load into a throwaway test
/// container here. Instead this fixture creates a minimal synthetic schema that exposes
/// the exact same table/proc/function names and columns ReportsDataService's SQL text
/// references, seeded with test rows — enough to exercise the actual ADO.NET code path
/// (parameter binding, stored-proc invocation, ordinal-based column reads, NULL handling)
/// end-to-end, which the existing unit tests (all of which mock IReportsService entirely)
/// never touch at all.
/// </summary>
[Collection("Reports Integration")]
public class ReportsIntegrationTests : IAsyncLifetime
{
    private readonly ReportsApiFactory _factory;
    private HttpClient _client = null!;

    public ReportsIntegrationTests(ReportsApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetSeedDataAsync();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.GenerateTestJwt());
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
    public async Task GetWagerActivity_NoBearerToken_Returns401()
    {
        using var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync("/api/reports/wagers?loginId=agent1&from=2026-01-01&to=2026-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWagerActivity_TokenWithoutReportsPermission_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.GenerateTestJwt(permissions: []));

        var response = await client.GetAsync("/api/reports/wagers?loginId=agent1&from=2026-01-01&to=2026-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/reports/wagers ─────────────────────────────────────────────────

    [Fact]
    public async Task GetWagerActivity_ReturnsRowsWithinDateRangeOnly()
    {
        var inRange  = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var outOfRange = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        await _factory.SeedWagerActivityAsync("DOC-1", "agent1", inRange, "Straight", 50m, "Pick 3 ticket", 0);
        await _factory.SeedWagerActivityAsync("DOC-2", "agent1", inRange.AddHours(2), "Parlay", 100m, null, 1);
        await _factory.SeedWagerActivityAsync("DOC-3", "agent1", outOfRange, "Straight", 25m, "Outside range", 0);
        await _factory.SeedWagerActivityAsync("DOC-4", "otheragent", inRange, "Straight", 999m, "Different login", 0);

        var response = await _client.GetAsync("/api/reports/wagers?loginId=agent1&from=2026-03-01&to=2026-03-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = (await response.Content.ReadFromJsonAsync<List<WagerActivityRow>>())!;
        rows.Should().HaveCount(2);
        rows.Select(r => r.DocumentNumber).Should().BeEquivalentTo(["DOC-1", "DOC-2"]);
        rows.Single(r => r.DocumentNumber == "DOC-2").Description.Should().BeEmpty("NULL Description should map to empty string");
    }

    [Fact]
    public async Task GetWagerActivity_MissingLoginId_Returns400()
    {
        var response = await _client.GetAsync("/api/reports/wagers?loginId=&from=2026-01-01&to=2026-01-31");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWagerActivity_LoginIdIsCaseInsensitive()
    {
        // ReportsDataService lower-cases @LoginId before the SP call.
        var when = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        await _factory.SeedWagerActivityAsync("DOC-5", "agentmixedcase", when, "Straight", 10m, null, 0);

        var response = await _client.GetAsync("/api/reports/wagers?loginId=AgentMixedCase&from=2026-03-01&to=2026-03-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await response.Content.ReadFromJsonAsync<List<WagerActivityRow>>();
        rows!.Should().ContainSingle(r => r.DocumentNumber == "DOC-5");
    }

    // ── GET /api/reports/transactions ───────────────────────────────────────────

    [Fact]
    public async Task GetChangedTransactions_AgentScoped_ReturnsTransactionsForCustomersUnderAgent()
    {
        await _factory.SeedCustomerAsync(101, "player1", agentId: 5);
        await _factory.SeedCustomerAsync(102, "player2", agentId: 5);
        await _factory.SeedCustomerAsync(103, "player3", agentId: 6); // different agent

        var when = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        await _factory.SeedChangedTransactionAsync(101, when, "Adjustment", -25m, "Manual credit", "REF-001");
        await _factory.SeedChangedTransactionAsync(102, when, "Adjustment", 50m, "Deposit correction", "REF-002");
        await _factory.SeedChangedTransactionAsync(103, when, "Adjustment", 10m, "Should not appear", "REF-003");

        var response = await _client.GetAsync(
            $"/api/reports/transactions?agentId=5&from={when.AddDays(-1):yyyy-MM-dd}&to={when.AddDays(1):yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = (await response.Content.ReadFromJsonAsync<List<ChangedTransactionRow>>())!;
        rows.Should().HaveCount(2);
        rows.Select(r => r.LoginName).Should().BeEquivalentTo(["player1", "player2"]);
    }

    [Fact]
    public async Task GetChangedTransactions_CustomerScoped_ReturnsOnlyThatCustomer()
    {
        await _factory.SeedCustomerAsync(201, "specific", agentId: 9);
        await _factory.SeedCustomerAsync(202, "other", agentId: 9);

        var when = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        await _factory.SeedChangedTransactionAsync(201, when, "Adjustment", 15m, "For specific customer", "REF-010");
        await _factory.SeedChangedTransactionAsync(202, when, "Adjustment", 20m, "For other customer", "REF-011");

        var response = await _client.GetAsync(
            $"/api/reports/transactions?agentId=9&customerId=201&from={when.AddDays(-1):yyyy-MM-dd}&to={when.AddDays(1):yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await response.Content.ReadFromJsonAsync<List<ChangedTransactionRow>>();
        rows!.Should().ContainSingle();
        rows![0].LoginName.Should().Be("specific");
    }

    // ── GET /api/reports/agents ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchAgents_ReturnsMatchingRows()
    {
        await _factory.SeedAgentSearchRowAsync(1, "agentAlpha", "Agent Alpha", "alpha@cog.local", true);
        await _factory.SeedAgentSearchRowAsync(2, "agentBeta", "Agent Beta", "beta@cog.local", true);
        await _factory.SeedAgentSearchRowAsync(3, "someoneElse", "Someone Else", "else@cog.local", false);

        var response = await _client.GetAsync("/api/reports/agents?agentId=1&search=agent");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = (await response.Content.ReadFromJsonAsync<List<AgentRow>>())!;
        rows.Should().HaveCount(2);
        rows.Select(r => r.LoginName).Should().BeEquivalentTo(["agentAlpha", "agentBeta"]);
    }

    // ── GET /api/reports/customers ───────────────────────────────────────────────

    [Fact]
    public async Task SearchCustomers_ReturnsMatchingRowsWithBalance()
    {
        await _factory.SeedCustomerSearchRowAsync(301, "custMatch", "Customer Match", agentId: 5, balance: 250m);
        await _factory.SeedCustomerSearchRowAsync(302, "noMatch", "No Match", agentId: 5, balance: 0m);

        var response = await _client.GetAsync("/api/reports/customers?agentId=5&search=custMatch");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await response.Content.ReadFromJsonAsync<List<CustomerRow>>();
        rows!.Should().ContainSingle();
        rows![0].Balance.Should().Be(250m);
    }

    // ── GET /api/reports/packages ────────────────────────────────────────────────

    [Fact]
    public async Task GetPackageTracker_ReturnsRowsWithinDateRange()
    {
        var inRange = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc);
        var outOfRange = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await _factory.SeedPackageTrackerRowAsync(5001, inRange, 200m, "Delivered", "Agent X", "PKG-REF-1", "FedEx");
        await _factory.SeedPackageTrackerRowAsync(5002, outOfRange, 300m, "Delivered", "Agent Y", "PKG-REF-2", "UPS");

        var response = await _client.GetAsync(
            $"/api/reports/packages?from={inRange.AddDays(-1):yyyy-MM-dd}&to={inRange.AddDays(1):yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await response.Content.ReadFromJsonAsync<List<PackageTrackerRow>>();
        rows!.Should().ContainSingle();
        rows![0].DocumentNumber.Should().Be(5001);
    }
}

[CollectionDefinition("Reports Integration")]
public class ReportsIntegrationCollection : ICollectionFixture<ReportsApiFactory> { }

public class ReportsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DevJwtSecretKey = "6UZhMYYN1EqSmi7rtNvpRBS46WsNbVuYZwXzNft7h8c=";
    private const string TestDatabaseName = "ReportsServiceTest";

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss1")
        .Build();

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        await CreateDatabaseAsync();

        // ReportsDataService reads IConfiguration.GetConnectionString("DefaultConnection") fresh
        // on every call (no DbContext to swap via ConfigureServices) — override via environment
        // variable *before* WebApplicationFactory ever builds the host, so the standard
        // ASP.NET Core env-var configuration provider picks it up reliably regardless of any
        // WebApplicationFactory/minimal-hosting ConfigureAppConfiguration ordering quirk.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", GetTestConnectionString());

        await CreateLegacyCompatSchemaAsync();
    }

    public new async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        await _sql.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    private string GetTestConnectionString()
    {
        var b = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = TestDatabaseName };
        return b.ConnectionString;
    }

    private async Task CreateDatabaseAsync()
    {
        await using var conn = new SqlConnection(_sql.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand($"IF DB_ID('{TestDatabaseName}') IS NULL CREATE DATABASE [{TestDatabaseName}]", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ExecAsync(string sql)
    {
        await using var conn = new SqlConnection(GetTestConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Minimal synthetic schema exposing exactly the table/proc/function names and columns
    /// ReportsDataService's raw SQL references — see the class-level doc comment on
    /// ReportsIntegrationTests for why this isn't the real legacy COGDB schema.
    /// </summary>
    private async Task CreateLegacyCompatSchemaAsync()
    {
        await ExecAsync("""
            IF OBJECT_ID('dbo.Customer') IS NULL
            CREATE TABLE dbo.Customer (
                idCustomer INT NOT NULL PRIMARY KEY,
                LoginName  VARCHAR(20) NOT NULL,
                IdAgent    INT NOT NULL
            )
            """);

        await ExecAsync("""
            IF OBJECT_ID('dbo.UpdatedCustomerTransaction') IS NULL
            CREATE TABLE dbo.UpdatedCustomerTransaction (
                Id              INT IDENTITY PRIMARY KEY,
                idCustomer      INT NOT NULL,
                UpdatedDateTime DATETIME NOT NULL,
                TranType        VARCHAR(50) NULL,
                Amount          DECIMAL(18,2) NULL,
                Description     VARCHAR(200) NULL,
                Reference       VARCHAR(50) NULL
            )
            """);

        await ExecAsync("""
            IF OBJECT_ID('dbo.WagerActivitySeed') IS NULL
            CREATE TABLE dbo.WagerActivitySeed (
                DocumentNumber VARCHAR(20) NOT NULL,
                LoginId        VARCHAR(20) NOT NULL,
                TranDateTime   DATETIME NOT NULL,
                TranType       VARCHAR(50) NOT NULL,
                Amount         DECIMAL(18,2) NOT NULL,
                Description    VARCHAR(200) NULL,
                GradeNum       INT NULL
            )
            """);

        await ExecAsync("""
            IF OBJECT_ID('dbo.AgentSearchSeed') IS NULL
            CREATE TABLE dbo.AgentSearchSeed (
                IdAgent        INT NOT NULL,
                AgentLoginName VARCHAR(20) NOT NULL,
                FullName       VARCHAR(100) NULL,
                Email          VARCHAR(100) NULL,
                IsActive       BIT NULL
            )
            """);

        await ExecAsync("""
            IF OBJECT_ID('dbo.CustomerSearchSeed') IS NULL
            CREATE TABLE dbo.CustomerSearchSeed (
                idCustomer INT NOT NULL,
                LoginName  VARCHAR(20) NOT NULL,
                FullName   VARCHAR(100) NULL,
                IdAgent    INT NOT NULL,
                Balance    DECIMAL(18,2) NULL
            )
            """);

        await ExecAsync("""
            IF OBJECT_ID('dbo.PackageTrackerSeed') IS NULL
            CREATE TABLE dbo.PackageTrackerSeed (
                DocumentNumber INT NOT NULL,
                TranDate       DATETIME NOT NULL,
                Amount         DECIMAL(18,2) NULL,
                Status         VARCHAR(50) NULL,
                PackageTo      VARCHAR(100) NULL,
                Reference      VARCHAR(50) NULL,
                PackageService VARCHAR(50) NULL
            )
            """);

        // Minimal stand-in: returns just the input agent (no recursive descendant lookup —
        // the real hierarchy-walk is accounts-service's AgentService.GetAllSubAgentIdsAsync,
        // already covered there). Good enough to exercise the JOIN this SQL performs.
        await ExecAsync("""
            IF OBJECT_ID('dbo.fn_GetSubAgentHierarchyByID') IS NOT NULL DROP FUNCTION dbo.fn_GetSubAgentHierarchyByID
            """);
        await ExecAsync("""
            CREATE FUNCTION dbo.fn_GetSubAgentHierarchyByID(@IdAgent INT)
            RETURNS TABLE
            AS
            RETURN (SELECT @IdAgent AS idAgent)
            """);

        await ExecAsync("IF OBJECT_ID('dbo.rptBetMakerActivity') IS NOT NULL DROP PROCEDURE dbo.rptBetMakerActivity");
        await ExecAsync("""
            CREATE PROCEDURE dbo.rptBetMakerActivity
                @LoginId VARCHAR(20), @from DATETIME, @to DATETIME
            AS
            BEGIN
                SELECT DocumentNumber, TranDateTime, TranType, Amount, Description, GradeNum
                FROM dbo.WagerActivitySeed
                WHERE LoginId = @LoginId AND TranDateTime >= @from AND TranDateTime <= @to
            END
            """);

        await ExecAsync("IF OBJECT_ID('dbo.ICBBSearchAgent') IS NOT NULL DROP PROCEDURE dbo.ICBBSearchAgent");
        await ExecAsync("""
            CREATE PROCEDURE dbo.ICBBSearchAgent
                @IdAgent INT, @Search VARCHAR(10)
            AS
            BEGIN
                SELECT IdAgent, AgentLoginName, FullName, Email, IsActive
                FROM dbo.AgentSearchSeed
                WHERE AgentLoginName LIKE '%' + @Search + '%'
            END
            """);

        await ExecAsync("IF OBJECT_ID('dbo.ICBBSearchCust') IS NOT NULL DROP PROCEDURE dbo.ICBBSearchCust");
        await ExecAsync("""
            CREATE PROCEDURE dbo.ICBBSearchCust
                @IdAgent INT, @Search VARCHAR(10)
            AS
            BEGIN
                SELECT idCustomer, LoginName, FullName, IdAgent, Balance
                FROM dbo.CustomerSearchSeed
                WHERE LoginName LIKE '%' + @Search + '%'
            END
            """);

        await ExecAsync("IF OBJECT_ID('dbo.rptPackageTracker') IS NOT NULL DROP PROCEDURE dbo.rptPackageTracker");
        await ExecAsync("""
            CREATE PROCEDURE dbo.rptPackageTracker
                @viewDep INT, @fromDate DATETIME, @toDate DATETIME, @agentDes INT
            AS
            BEGIN
                SELECT DocumentNumber, TranDate, Amount, Status, PackageTo, Reference, PackageService
                FROM dbo.PackageTrackerSeed
                WHERE TranDate >= @fromDate AND TranDate <= @toDate
            END
            """);
    }

    public async Task ResetSeedDataAsync()
    {
        foreach (var table in new[]
        {
            "WagerActivitySeed", "Customer", "UpdatedCustomerTransaction",
            "AgentSearchSeed", "CustomerSearchSeed", "PackageTrackerSeed"
        })
        {
            await ExecAsync($"DELETE FROM dbo.{table}");
        }
    }

    public async Task SeedWagerActivityAsync(
        string documentNumber, string loginId, DateTime tranDateTime, string tranType,
        decimal amount, string? description, int gradeNum)
    {
        await using var conn = new SqlConnection(GetTestConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            INSERT INTO dbo.WagerActivitySeed (DocumentNumber, LoginId, TranDateTime, TranType, Amount, Description, GradeNum)
            VALUES (@doc, @login, @when, @type, @amount, @desc, @grade)
            """, conn);
        cmd.Parameters.AddWithValue("@doc", documentNumber);
        cmd.Parameters.AddWithValue("@login", loginId);
        cmd.Parameters.AddWithValue("@when", tranDateTime);
        cmd.Parameters.AddWithValue("@type", tranType);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@desc", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@grade", gradeNum);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SeedCustomerAsync(int customerId, string loginName, int agentId)
    {
        await using var conn = new SqlConnection(GetTestConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            INSERT INTO dbo.Customer (idCustomer, LoginName, IdAgent) VALUES (@id, @login, @agent)
            """, conn);
        cmd.Parameters.AddWithValue("@id", customerId);
        cmd.Parameters.AddWithValue("@login", loginName);
        cmd.Parameters.AddWithValue("@agent", agentId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SeedChangedTransactionAsync(
        int customerId, DateTime updatedDateTime, string tranType, decimal amount, string? description, string? reference)
    {
        await using var conn = new SqlConnection(GetTestConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            INSERT INTO dbo.UpdatedCustomerTransaction (idCustomer, UpdatedDateTime, TranType, Amount, Description, Reference)
            VALUES (@id, @when, @type, @amount, @desc, @ref)
            """, conn);
        cmd.Parameters.AddWithValue("@id", customerId);
        cmd.Parameters.AddWithValue("@when", updatedDateTime);
        cmd.Parameters.AddWithValue("@type", tranType);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@desc", (object?)description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ref", (object?)reference ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SeedAgentSearchRowAsync(int idAgent, string loginName, string? fullName, string? email, bool isActive)
    {
        await using var conn = new SqlConnection(GetTestConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            INSERT INTO dbo.AgentSearchSeed (IdAgent, AgentLoginName, FullName, Email, IsActive)
            VALUES (@id, @login, @name, @email, @active)
            """, conn);
        cmd.Parameters.AddWithValue("@id", idAgent);
        cmd.Parameters.AddWithValue("@login", loginName);
        cmd.Parameters.AddWithValue("@name", (object?)fullName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@email", (object?)email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@active", isActive);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SeedCustomerSearchRowAsync(int idCustomer, string loginName, string? fullName, int agentId, decimal balance)
    {
        await using var conn = new SqlConnection(GetTestConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            INSERT INTO dbo.CustomerSearchSeed (idCustomer, LoginName, FullName, IdAgent, Balance)
            VALUES (@id, @login, @name, @agent, @balance)
            """, conn);
        cmd.Parameters.AddWithValue("@id", idCustomer);
        cmd.Parameters.AddWithValue("@login", loginName);
        cmd.Parameters.AddWithValue("@name", (object?)fullName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@agent", agentId);
        cmd.Parameters.AddWithValue("@balance", balance);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SeedPackageTrackerRowAsync(
        int documentNumber, DateTime tranDate, decimal amount, string status,
        string packageTo, string reference, string packageService)
    {
        await using var conn = new SqlConnection(GetTestConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            INSERT INTO dbo.PackageTrackerSeed (DocumentNumber, TranDate, Amount, Status, PackageTo, Reference, PackageService)
            VALUES (@doc, @when, @amount, @status, @to, @ref, @service)
            """, conn);
        cmd.Parameters.AddWithValue("@doc", documentNumber);
        cmd.Parameters.AddWithValue("@when", tranDate);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@to", packageTo);
        cmd.Parameters.AddWithValue("@ref", reference);
        cmd.Parameters.AddWithValue("@service", packageService);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Mints a JWT matching auth-service's claim shape, with the "permission" claims
    /// ReportsController's "CanViewReports" policy checks (RequireClaim("permission",
    /// "Reports.View", "Reports.Export")). Pass an empty permissions array to test the
    /// 403 (authenticated but unauthorized) path.
    /// </summary>
    public string GenerateTestJwt(string[]? permissions = null)
    {
        permissions ??= ["Reports.View", "Reports.Export"];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevJwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "1"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("login_name", "reportsviewer"),
            new("user_type", "Employee"),
            new(ClaimTypes.Role, "ReportsViewer"),
        };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

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
