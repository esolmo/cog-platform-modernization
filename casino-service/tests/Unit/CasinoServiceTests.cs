using Xunit;
using CasinoService.Configuration;
using CasinoService.Data;
using CasinoService.Entities;
using CasinoService.Models.Requests;
using CasinoService.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace CasinoService.Tests.Unit;

public class CasinoServiceTests
{
    private static CasinoDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<CasinoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CasinoDbContext(opts);
    }

    private static IOptions<CasinoOptions> DefaultOptions() =>
        Options.Create(new CasinoOptions { CasinoId = 2 });

    // ──────────────────────────────────────────────────────────────────────
    // RegisterPlayer
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterPlayer_NewPlayer_ReturnsSuccess()
    {
        var db      = CreateDb();
        var dealer  = new Mock<ILiveDealerClient>();
        var accounts = new Mock<IAccountsClient>();

        dealer.Setup(d => d.AddPlayerAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveDealerPlayerResult(
                Success: true, CustId: "C001", Nickname: "Tester",
                Ticket: "tok123", ExternalPlayerId: "ext-001",
                Balance: 0, BonusBalance: 0, Playthrough: 0,
                ErrorCode: null, ErrorDescription: null));

        var svc = new Services.CasinoService(db, dealer.Object, accounts.Object,
            DefaultOptions(), NullLogger<Services.CasinoService>.Instance);

        var result = await svc.RegisterPlayerAsync("C001",
            new RegisterPlayerRequest("Tester", "127.0.0.1"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Nickname.Should().Be("Tester");
        result.Value.ExternalPlayerId.Should().Be("ext-001");
        db.CasinoPlayers.Should().HaveCount(1);
    }

    [Fact]
    public async Task RegisterPlayer_AlreadyRegistered_ReturnsFailure()
    {
        var db = CreateDb();
        db.CasinoPlayers.Add(new CasinoPlayer
        {
            CustomerId = "C001", Nickname = "Existing",
            ExternalPlayerId = "ext-001", CasinoId = 2
        });
        await db.SaveChangesAsync();

        var svc = new Services.CasinoService(db,
            new Mock<ILiveDealerClient>().Object,
            new Mock<IAccountsClient>().Object,
            DefaultOptions(), NullLogger<Services.CasinoService>.Instance);

        var result = await svc.RegisterPlayerAsync("C001",
            new RegisterPlayerRequest("Duplicate", "127.0.0.1"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ALREADY_REGISTERED");
    }

    [Fact]
    public async Task RegisterPlayer_ExternalApiFails_ReturnsFailure()
    {
        var db     = CreateDb();
        var dealer = new Mock<ILiveDealerClient>();

        dealer.Setup(d => d.AddPlayerAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveDealerPlayerResult(
                false, null, null, null, null, 0, 0, 0, "201", "Display name already exists"));

        var svc = new Services.CasinoService(db, dealer.Object,
            new Mock<IAccountsClient>().Object,
            DefaultOptions(), NullLogger<Services.CasinoService>.Instance);

        var result = await svc.RegisterPlayerAsync("C002",
            new RegisterPlayerRequest("Taken", "127.0.0.1"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("201");
        db.CasinoPlayers.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────
    // GetSession
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSession_NotRegistered_ReturnsNotRegistered()
    {
        var db = CreateDb();
        var svc = new Services.CasinoService(db,
            new Mock<ILiveDealerClient>().Object,
            new Mock<IAccountsClient>().Object,
            DefaultOptions(), NullLogger<Services.CasinoService>.Instance);

        var result = await svc.GetSessionAsync("C999", "127.0.0.1");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_REGISTERED");
    }

    [Fact]
    public async Task GetSession_RegisteredPlayer_ReturnsLobbyUrl()
    {
        var db = CreateDb();
        db.CasinoPlayers.Add(new CasinoPlayer
        {
            CustomerId = "C001", Nickname = "Tester",
            ExternalPlayerId = "ext-001", CasinoId = 2
        });
        await db.SaveChangesAsync();

        var dealer = new Mock<ILiveDealerClient>();
        dealer.Setup(d => d.LoginAsync("C001", "Tester", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveDealerPlayerResult(
                true, "C001", "Tester", "ticket-abc", "ext-001",
                100m, 0m, 0m, null, null));

        var opts = Options.Create(new CasinoOptions
        {
            CasinoId = 2,
            LobbyBaseUrl = "https://ittds.newland.cr/entrance/playervalid.php"
        });

        var svc = new Services.CasinoService(db, dealer.Object,
            new Mock<IAccountsClient>().Object,
            opts, NullLogger<Services.CasinoService>.Instance);

        var result = await svc.GetSessionAsync("C001", "127.0.0.1");

        result.IsSuccess.Should().BeTrue();
        result.Value!.LobbyUrl.Should().Contain("ticket-abc");
        result.Value.CasinoBalance.Should().Be(100m);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Deposit
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deposit_InsufficientFunds_ReturnsFailure()
    {
        var db = CreateDb();
        db.CasinoPlayers.Add(new CasinoPlayer
        {
            CustomerId = "C001", Nickname = "Tester",
            ExternalPlayerId = "ext-001", CasinoId = 2
        });
        await db.SaveChangesAsync();

        var accounts = new Mock<IAccountsClient>();
        accounts.Setup(a => a.GetAvailableBalanceAsync("C001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(50m);

        var svc = new Services.CasinoService(db,
            new Mock<ILiveDealerClient>().Object, accounts.Object,
            DefaultOptions(), NullLogger<Services.CasinoService>.Instance);

        var result = await svc.DepositAsync("C001", new TransferFundsRequest(100m));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INSUFFICIENT_FUNDS");
    }
}
