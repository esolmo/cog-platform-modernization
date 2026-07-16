using BettingService.Data;
using BettingService.Models.Requests;
using BettingService.Services;
using Cog.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BettingService.Tests.Services;

public class GameServiceTests : IDisposable
{
    private readonly BettingDbContext _db;
    private readonly GameService _sut;

    public GameServiceTests()
    {
        var options = new DbContextOptionsBuilder<BettingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BettingDbContext(options);

        var mapper = new AutoMapper.MapperConfiguration(
            c => c.AddProfile<BettingService.Mapping.BettingMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        ).CreateMapper();

        _sut = new GameService(_db, mapper, NullLogger<GameService>.Instance);

        SeedData();
    }

    private void SeedData()
    {
        var sport = new SportType { Id = 1, Name = "Football", Code = "NFL", IsActive = true, DisplayOrder = 1 };
        _db.SportTypes.Add(sport);

        var game = new Game
        {
            Id = 1, SportTypeId = 1, HomeTeam = "Chiefs", AwayTeam = "Raiders",
            GameDate = DateTime.UtcNow.AddDays(3), Status = GameStatus.Upcoming
        };
        _db.Games.Add(game);

        var period = new GamePeriod { Id = 1, GameId = 1, PeriodDescription = "Full Game", PeriodNumber = 0 };
        _db.GamePeriods.Add(period);

        var finalGame = new Game
        {
            Id = 2, SportTypeId = 1, HomeTeam = "Packers", AwayTeam = "Bears",
            GameDate = DateTime.UtcNow.AddDays(-1), Status = GameStatus.Final
        };
        _db.Games.Add(finalGame);

        _db.SaveChanges();
    }

    // ── CreateGame ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGame_ValidRequest_CreatesGameWithPeriods()
    {
        var request = new CreateGameRequest
        {
            SportTypeId = 1,
            HomeTeam    = "Eagles",
            AwayTeam    = "Cowboys",
            GameDate    = DateTime.UtcNow.AddDays(7),
            Periods =
            [
                new CreateGamePeriodRequest { PeriodDescription = "Full Game", PeriodNumber = 0 },
                new CreateGamePeriodRequest { PeriodDescription = "1st Half",  PeriodNumber = 1 },
            ]
        };

        var result = await _sut.CreateGameAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HomeTeam.Should().Be("Eagles");
        result.Value.Periods.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateGame_SportNotFound_ReturnsFailure()
    {
        var request = new CreateGameRequest
        {
            SportTypeId = 999,
            HomeTeam    = "Team A",
            AwayTeam    = "Team B",
            GameDate    = DateTime.UtcNow.AddDays(5),
        };

        var result = await _sut.CreateGameAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    // ── UpdateGame ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateGame_ValidRequest_UpdatesFields()
    {
        var request = new UpdateGameRequest
        {
            HomeTeam       = "Chiefs Updated",
            AwayTeam       = "Raiders Updated",
            GameDate       = DateTime.UtcNow.AddDays(4),
            RotationNumber = "101",
        };

        var result = await _sut.UpdateGameAsync(1, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HomeTeam.Should().Be("Chiefs Updated");
        result.Value.RotationNumber.Should().Be("101");
    }

    [Fact]
    public async Task UpdateGame_FinalGame_ReturnsInvalidStatus()
    {
        var request = new UpdateGameRequest
        {
            HomeTeam = "X", AwayTeam = "Y", GameDate = DateTime.UtcNow
        };

        var result = await _sut.UpdateGameAsync(2, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_STATUS");
    }

    [Fact]
    public async Task UpdateGame_NotFound_ReturnsFailure()
    {
        var request = new UpdateGameRequest
        {
            HomeTeam = "X", AwayTeam = "Y", GameDate = DateTime.UtcNow
        };

        var result = await _sut.UpdateGameAsync(999, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    // ── UpdateGameStatus ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateGameStatus_ValidTransition_ReturnsUpdatedGame()
    {
        var result = await _sut.UpdateGameStatusAsync(
            1, new UpdateGameStatusRequest { Status = GameStatus.InProgress }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(GameStatus.InProgress);
    }

    // ── DeleteGame ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteGame_NoWagers_Succeeds()
    {
        var result = await _sut.DeleteGameAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _db.Games.Any(g => g.Id == 1).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteGame_WithWagers_ReturnsHasWagers()
    {
        _db.WagerItems.Add(new WagerItem
        {
            WagerId         = 0,
            GamePeriodId    = 1,
            ItemType        = WagerItemType.Spread,
            Side            = WagerSide.Home,
            LineAtTimeOfWager = -110,
        });
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteGameAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("HAS_WAGERS");
    }

    // ── AddPeriod ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddPeriod_ValidGame_AddsPeriod()
    {
        var request = new CreateGamePeriodRequest
        {
            PeriodDescription = "2nd Half",
            PeriodNumber      = 2,
        };

        var result = await _sut.AddPeriodAsync(1, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PeriodDescription.Should().Be("2nd Half");
    }

    [Fact]
    public async Task AddPeriod_FinalGame_ReturnsInvalidStatus()
    {
        var request = new CreateGamePeriodRequest
        {
            PeriodDescription = "Overtime", PeriodNumber = 5
        };

        var result = await _sut.AddPeriodAsync(2, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_STATUS");
    }

    // ── RemovePeriod ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RemovePeriod_NoWagers_RemovesPeriod()
    {
        var result = await _sut.RemovePeriodAsync(1, 1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _db.GamePeriods.Any(p => p.Id == 1).Should().BeFalse();
    }

    [Fact]
    public async Task RemovePeriod_WrongGame_ReturnsNotFound()
    {
        var result = await _sut.RemovePeriodAsync(2, 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    // ── CreateSportType ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSportType_NewCode_Succeeds()
    {
        var request = new CreateSportTypeRequest
        {
            Name = "Basketball", Code = "NBA", DisplayOrder = 2
        };

        var result = await _sut.CreateSportTypeAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("NBA");
        result.Value.Name.Should().Be("Basketball");
    }

    [Fact]
    public async Task CreateSportType_DuplicateCode_ReturnsConflict()
    {
        var request = new CreateSportTypeRequest
        {
            Name = "Football 2", Code = "NFL", DisplayOrder = 99
        };

        var result = await _sut.CreateSportTypeAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("DUPLICATE_CODE");
    }

    // ── UpdateSportType ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSportType_ValidRequest_UpdatesFields()
    {
        var request = new UpdateSportTypeRequest
        {
            Name = "American Football", Code = "NFL", IsActive = true, DisplayOrder = 1
        };

        var result = await _sut.UpdateSportTypeAsync(1, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("American Football");
    }

    [Fact]
    public async Task UpdateSportType_NotFound_ReturnsFailure()
    {
        var request = new UpdateSportTypeRequest
        {
            Name = "X", Code = "XYZ", IsActive = true, DisplayOrder = 1
        };

        var result = await _sut.UpdateSportTypeAsync(999, request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    public void Dispose() => _db.Dispose();
}
