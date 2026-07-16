using BettingService.Data;
using BettingService.Models.Requests;
using BettingService.Services;
using Cog.Domain.Common;
using Cog.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BettingService.Tests.Services;

public class WagerServiceTests : IDisposable
{
    private readonly BettingDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly WagerService _sut;

    public WagerServiceTests()
    {
        var options = new DbContextOptionsBuilder<BettingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BettingDbContext(options);

        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.AgentId.Returns(1);
        _currentUser.LoginName.Returns("testagent");

        var mapper = new AutoMapper.MapperConfiguration(
            c => c.AddProfile<BettingService.Mapping.BettingMappingProfile>(),
            NullLoggerFactory.Instance
        ).CreateMapper();
        _sut = new WagerService(_db, mapper, _currentUser, NullLogger<WagerService>.Instance);

        SeedTestData();
    }

    [Fact]
    public async Task CreateWager_StraightBet_ReturnsSuccess()
    {
        var request = new CreateWagerRequest
        {
            CustomerId = 1,
            WagerType = WagerType.Straight,
            RiskAmount = 110m,
            Items = [new WagerItemRequest { GamePeriodId = 1, ItemType = WagerItemType.Spread, Side = WagerSide.Home }]
        };

        var result = await _sut.CreateWagerAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RiskAmount.Should().Be(110m);
        result.Value.WinAmount.Should().Be(100m); // -110 line = win 100
        result.Value.WagerType.Should().Be(WagerType.Straight);
    }

    [Fact]
    public async Task CreateWager_InactiveCustomer_ReturnsFailure()
    {
        var request = new CreateWagerRequest
        {
            CustomerId = 2, // suspended customer
            WagerType = WagerType.Straight,
            RiskAmount = 100m,
            Items = [new WagerItemRequest { GamePeriodId = 1, ItemType = WagerItemType.Spread, Side = WagerSide.Home }]
        };

        var result = await _sut.CreateWagerAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("CUSTOMER_INACTIVE");
    }

    [Fact]
    public async Task CreateWager_IdempotentKey_ReturnsSameWager()
    {
        var key = Guid.NewGuid().ToString();
        var request = new CreateWagerRequest
        {
            CustomerId = 1,
            WagerType = WagerType.Straight,
            RiskAmount = 110m,
            IdempotencyKey = key,
            Items = [new WagerItemRequest { GamePeriodId = 1, ItemType = WagerItemType.Spread, Side = WagerSide.Home }]
        };

        var first = await _sut.CreateWagerAsync(request, CancellationToken.None);
        var second = await _sut.CreateWagerAsync(request, CancellationToken.None);

        first.Value!.Id.Should().Be(second.Value!.Id);
    }

    [Fact]
    public async Task CancelWager_PendingWager_ReturnsSuccess()
    {
        var request = new CreateWagerRequest
        {
            CustomerId = 1,
            WagerType = WagerType.Straight,
            RiskAmount = 50m,
            Items = [new WagerItemRequest { GamePeriodId = 1, ItemType = WagerItemType.MoneyLine, Side = WagerSide.Away }]
        };

        var created = await _sut.CreateWagerAsync(request, CancellationToken.None);
        var cancelled = await _sut.CancelWagerAsync(created.Value!.Id, CancellationToken.None);

        cancelled.IsSuccess.Should().BeTrue();
        var wager = await _db.Wagers.FindAsync(created.Value.Id);
        wager!.Status.Should().Be(WagerStatus.Cancelled);
    }

    [Fact]
    public async Task CancelWager_AlreadyCancelled_ReturnsFailure()
    {
        var request = new CreateWagerRequest
        {
            CustomerId = 1,
            WagerType = WagerType.Straight,
            RiskAmount = 50m,
            Items = [new WagerItemRequest { GamePeriodId = 1, ItemType = WagerItemType.Spread, Side = WagerSide.Home }]
        };

        var created = await _sut.CreateWagerAsync(request, CancellationToken.None);
        await _sut.CancelWagerAsync(created.Value!.Id, CancellationToken.None);
        var result = await _sut.CancelWagerAsync(created.Value.Id, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_STATE");
    }

    private void SeedTestData()
    {
        _db.SportTypes.Add(new SportType { Id = 1, Name = "Football", Code = "FB", DisplayOrder = 1 });

        _db.Games.Add(new Game
        {
            Id = 1, SportTypeId = 1,
            HomeTeam = "Team A", AwayTeam = "Team B",
            GameDate = DateTime.UtcNow.AddDays(1),
            Status = GameStatus.Upcoming
        });

        _db.GamePeriods.Add(new GamePeriod
        {
            Id = 1, GameId = 1,
            PeriodDescription = "Full Game", PeriodNumber = 0
        });

        _db.LineSets.Add(new LineSet
        {
            Id = 1, GamePeriodId = 1,
            Spread = -3.5m, SpreadJuice = -110m,
            HomeMoneyLine = -150m, AwayMoneyLine = 130m,
            Total = 47.5m, OverJuice = -110m, UnderJuice = -110m,
            OfferingSpread = true, OfferingMoneyLine = true, OfferingTotal = true
        });

        _db.Agents.Add(new Agent { Id = 1, LoginName = "agent1", PasswordHash = "hash", Name = "Agent One" });

        _db.Customers.Add(new Customer
        {
            Id = 1, LoginName = "cust1", PasswordHash = "hash",
            FirstName = "John", LastName = "Doe", AgentId = 1,
            Status = CustomerStatus.Active
        });

        _db.Customers.Add(new Customer
        {
            Id = 2, LoginName = "cust2", PasswordHash = "hash",
            FirstName = "Jane", LastName = "Doe", AgentId = 1,
            Status = CustomerStatus.Suspended
        });

        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();
}
