using FluentAssertions;
using LotteryService.Data;
using LotteryService.Entities;
using LotteryService.Models;
using LotteryService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LotteryService.Tests.Unit;

public class LotteryServiceTests : IDisposable
{
    private readonly LotteryDbContext _db;
    private readonly Mock<IAccountsClient> _accountsClientMock;
    private readonly LotteryGameService _service;

    public LotteryServiceTests()
    {
        var options = new DbContextOptionsBuilder<LotteryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new LotteryDbContext(options);
        _db.Database.EnsureCreated();

        _accountsClientMock = new Mock<IAccountsClient>();
        _service = new LotteryGameService(_db, _accountsClientMock.Object, NullLogger<LotteryGameService>.Instance);

        SeedTestData();
    }

    [Fact]
    public async Task GetAvailableDrawings_ReturnsOnlyFutureActiveDrawings()
    {
        var drawings = await _service.GetAvailableDrawingsAsync(1);

        drawings.Should().HaveCount(2);
        drawings.Should().AllSatisfy(d => d.DrawingDate.Should().BeAfter(DateTime.UtcNow));
    }

    [Fact]
    public async Task GetAvailableDrawings_ExcludesPastDrawings()
    {
        var drawings = await _service.GetAvailableDrawingsAsync(1);

        drawings.Should().NotContain(d => d.Name.Contains("Past"));
    }

    [Fact]
    public async Task PurchaseTicket_WithSufficientBalance_Succeeds()
    {
        _accountsClientMock.Setup(c => c.GetBalanceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500m);

        var request = new PurchaseRequest(
            DrawingDetailId: 1,
            DateToPlay: DateTime.UtcNow.Date.AddDays(1),
            PickType: PickType.Straight,
            Picks: [new PickRequest(1, 2, 3, 0, 5m)]);

        var result = await _service.PurchaseTicketAsync(1, 10, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(5m);
    }

    [Fact]
    public async Task PurchaseTicket_WithInsufficientBalance_ReturnsFailure()
    {
        _accountsClientMock.Setup(c => c.GetBalanceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1m);

        var request = new PurchaseRequest(
            DrawingDetailId: 1,
            DateToPlay: DateTime.UtcNow.Date.AddDays(1),
            PickType: PickType.Straight,
            Picks: [new PickRequest(1, 2, 3, 0, 50m)]);

        var result = await _service.PurchaseTicketAsync(1, 10, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INSUFFICIENT_BALANCE");
    }

    [Fact]
    public async Task PurchaseTicket_WithExpiredDrawing_ReturnsFailure()
    {
        _accountsClientMock.Setup(c => c.GetBalanceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(500m);

        var request = new PurchaseRequest(
            DrawingDetailId: 99, // expired drawing id
            DateToPlay: DateTime.UtcNow.Date.AddDays(1),
            PickType: PickType.Straight,
            Picks: [new PickRequest(1, 2, 3, 0, 5m)]);

        var result = await _service.PurchaseTicketAsync(1, 10, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DRAWING_NOT_FOUND");
    }

    [Fact]
    public void ExpandPicks_StraightPick3_ReturnsSingleEntry()
    {
        var request = new PurchaseRequest(
            DrawingDetailId: 1,
            DateToPlay: DateTime.Today,
            PickType: PickType.Straight,
            Picks: [new PickRequest(4, 5, 6, 0, 10m)]);

        var picks = _service.ExpandPicks(request, 100m);

        picks.Should().HaveCount(1);
        picks[0].Cost.Should().Be(10m);
        picks[0].Prize.Should().Be(1000m);
        picks[0].PlayCount.Should().Be(1);
    }

    [Fact]
    public void ExpandPicks_BoxedPick3_ExpandsToSixPermutations()
    {
        // {1,2,3} boxed → 3! = 6 permutations
        var request = new PurchaseRequest(
            DrawingDetailId: 1,
            DateToPlay: DateTime.Today,
            PickType: PickType.Boxed,
            Picks: [new PickRequest(1, 2, 3, 0, 6m)]);

        var picks = _service.ExpandPicks(request, 100m);

        picks.Should().HaveCount(6);
        picks.Should().AllSatisfy(p => p.PickType.Should().Be(PickType.Boxed));
        picks.Sum(p => p.Cost).Should().Be(6m); // total cost preserved
    }

    [Fact]
    public void ExpandPicks_BoxedPick4_Expands24Permutations()
    {
        // {1,2,3,4} boxed → 4! = 24 permutations
        var request = new PurchaseRequest(
            DrawingDetailId: 1,
            DateToPlay: DateTime.Today,
            PickType: PickType.Boxed,
            Picks: [new PickRequest(1, 2, 3, 4, 24m)]);

        var picks = _service.ExpandPicks(request, 100m);

        picks.Should().HaveCount(24);
        picks.Sum(p => p.Cost).Should().BeApproximately(24m, 0.01m);
    }

    [Fact]
    public async Task GetCustomerTickets_ReturnsOnlyCustomerTickets()
    {
        _accountsClientMock.Setup(c => c.GetBalanceAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000m);

        var request = new PurchaseRequest(1, DateTime.UtcNow.Date.AddDays(1), PickType.Straight,
            [new PickRequest(1, 2, 3, 0, 5m)]);
        await _service.PurchaseTicketAsync(1, 10, request);
        await _service.PurchaseTicketAsync(2, 10, request); // different customer

        var customer1Tickets = await _service.GetCustomerTicketsAsync(1, null, null);
        customer1Tickets.Should().AllSatisfy(t => t.Id.Should().BePositive());
    }

    private void SeedTestData()
    {
        // LotteryGame Id=1 (Pick3) / Id=2 (Pick4) are already seeded via HasData in
        // LotteryDbContext.OnModelCreating, applied by EnsureCreated() above.
        _db.DrawingDetails.AddRange(
            new DrawingDetail
            {
                Id = 1, LotteryGameId = 1, Name = "Morning Draw",
                DrawingDate = DateTime.UtcNow.AddHours(6), TimeZoneId = "UTC", MinutesToDraw = 5, IsActive = true
            },
            new DrawingDetail
            {
                Id = 2, LotteryGameId = 1, Name = "Evening Draw",
                DrawingDate = DateTime.UtcNow.AddHours(12), TimeZoneId = "UTC", MinutesToDraw = 5, IsActive = true
            },
            new DrawingDetail
            {
                Id = 3, LotteryGameId = 1, Name = "Past Draw",
                DrawingDate = DateTime.UtcNow.AddHours(-2), TimeZoneId = "UTC", MinutesToDraw = 5, IsActive = true
            }
        );
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();
}
