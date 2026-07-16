using BettingService.Models.Requests;
using BettingService.Models.Responses;
using Cog.Domain.Common;

namespace BettingService.Services;

public interface IWagerService
{
    Task<Result<WagerResponse>> CreateWagerAsync(CreateWagerRequest request, CancellationToken ct);
    Task<Result<WagerResponse>> GetWagerByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<WagerResponse>> GetWagersByCustomerAsync(int customerId, int page, int pageSize, CancellationToken ct);
    Task<PagedResult<WagerResponse>> GetPendingWagersByAgentAsync(int agentId, int page, int pageSize, CancellationToken ct);
    Task<PagedResult<WagerResponse>> GetGradedWagersAsync(int? agentId, int? gameId, int page, int pageSize, CancellationToken ct);
    Task<Result<GradeGameResponse>> GradeGameAsync(int gameId, GradeGameRequest request, CancellationToken ct);
    Task<Result> CancelWagerAsync(int id, CancellationToken ct);
}
