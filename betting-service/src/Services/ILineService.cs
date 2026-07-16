using BettingService.Models.Requests;
using BettingService.Models.Responses;
using Cog.Domain.Common;

namespace BettingService.Services;

public interface ILineService
{
    Task<Result<LineSetResponse>> GetLinesByGamePeriodAsync(int gamePeriodId, CancellationToken ct);
    Task<Result<LineSetResponse>> SetSpreadAsync(int gamePeriodId, SetSpreadRequest request, CancellationToken ct);
    Task<Result<LineSetResponse>> SetMoneyLineAsync(int gamePeriodId, SetMoneyLineRequest request, CancellationToken ct);
    Task<Result<LineSetResponse>> SetTotalAsync(int gamePeriodId, SetTotalRequest request, CancellationToken ct);
    Task<Result> ApplyShadeAsync(int gamePeriodId, ApplyShadeRequest request, CancellationToken ct);
    Task<Result> RemoveShadeAsync(int gamePeriodId, int agentId, CancellationToken ct);
}
