using LotteryService.Common;
using LotteryService.Models;

namespace LotteryService.Services;

public interface ILotteryService
{
    Task<List<DrawingDetailDto>> GetAvailableDrawingsAsync(int gameId, CancellationToken ct = default);
    Task<decimal> GetCustomerBalanceAsync(int customerId, CancellationToken ct = default);
    Task<Result<TicketDto>> PurchaseTicketAsync(int customerId, int agentId, PurchaseRequest request, CancellationToken ct = default);
    Task<List<TicketDto>> GetCustomerTicketsAsync(int customerId, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<TicketDto?> GetTicketAsync(long ticketId, CancellationToken ct = default);
    List<PickEntryDto> ExpandPicks(PurchaseRequest request, decimal prizeMultiplier);
}
