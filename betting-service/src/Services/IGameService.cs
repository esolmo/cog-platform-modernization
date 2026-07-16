using BettingService.Models.Requests;
using BettingService.Models.Responses;
using Cog.Domain.Common;

namespace BettingService.Services;

public interface IGameService
{
    // Queries
    Task<PagedResult<GameResponse>> GetGamesAsync(int? sportTypeId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize, CancellationToken ct);
    Task<Result<GameResponse>> GetGameByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<SportTypeResponse>> GetActiveSportTypesAsync(CancellationToken ct);
    Task<IEnumerable<SportTypeResponse>> GetAllSportTypesAsync(CancellationToken ct);

    // Game mutations
    Task<Result<GameResponse>> CreateGameAsync(CreateGameRequest request, CancellationToken ct);
    Task<Result<GameResponse>> UpdateGameAsync(int id, UpdateGameRequest request, CancellationToken ct);
    Task<Result<GameResponse>> UpdateGameStatusAsync(int id, UpdateGameStatusRequest request, CancellationToken ct);
    Task<Result> DeleteGameAsync(int id, CancellationToken ct);

    // Period mutations
    Task<Result<GamePeriodResponse>> AddPeriodAsync(int gameId, CreateGamePeriodRequest request, CancellationToken ct);
    Task<Result> RemovePeriodAsync(int gameId, int periodId, CancellationToken ct);

    // Sport type mutations
    Task<Result<SportTypeResponse>> CreateSportTypeAsync(CreateSportTypeRequest request, CancellationToken ct);
    Task<Result<SportTypeResponse>> UpdateSportTypeAsync(int id, UpdateSportTypeRequest request, CancellationToken ct);
}
