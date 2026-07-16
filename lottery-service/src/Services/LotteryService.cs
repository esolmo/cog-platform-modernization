using LotteryService.Combinatorics;
using LotteryService.Common;
using LotteryService.Data;
using LotteryService.Entities;
using LotteryService.Models;
using Microsoft.EntityFrameworkCore;

namespace LotteryService.Services;

public class LotteryGameService(LotteryDbContext db, IAccountsClient accountsClient, ILogger<LotteryGameService> logger)
    : ILotteryService
{
    public async Task<List<DrawingDetailDto>> GetAvailableDrawingsAsync(int gameId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.DrawingDetails
            .Include(d => d.LotteryGame)
            .Where(d => d.LotteryGameId == gameId && d.IsActive && d.DrawingDate > now)
            .OrderBy(d => d.DrawingDate)
            .Select(d => new DrawingDetailDto(
                d.Id,
                d.LotteryGameId,
                d.LotteryGame.Name,
                d.Name,
                d.DrawingDate,
                d.TimeZoneId,
                d.MinutesToDraw))
            .ToListAsync(ct);
    }

    public async Task<decimal> GetCustomerBalanceAsync(int customerId, CancellationToken ct = default)
    {
        return await accountsClient.GetBalanceAsync(customerId, ct);
    }

    public async Task<Result<TicketDto>> PurchaseTicketAsync(
        int customerId, int agentId, PurchaseRequest request, CancellationToken ct = default)
    {
        var drawing = await db.DrawingDetails
            .Include(d => d.LotteryGame)
            .FirstOrDefaultAsync(d => d.Id == request.DrawingDetailId && d.IsActive, ct);

        if (drawing is null)
            return Result<TicketDto>.Failure("Drawing not found or no longer available.", "DRAWING_NOT_FOUND");

        if (drawing.DrawingDate <= DateTime.UtcNow)
            return Result<TicketDto>.Failure("Drawing has already occurred.", "DRAWING_EXPIRED");

        var prizeMultiplier = 100m; // Default prize multiplier — should come from parameters
        var expandedPicks = ExpandPicks(request, prizeMultiplier);
        var total = expandedPicks.Sum(p => p.Cost);

        var balance = await accountsClient.GetBalanceAsync(customerId, ct);
        if (total > balance)
            return Result<TicketDto>.Failure(
                $"Insufficient balance. Required: {total:C}, Available: {balance:C}",
                "INSUFFICIENT_BALANCE");

        var eventDate = new DateTime(
            request.DateToPlay.Year, request.DateToPlay.Month, request.DateToPlay.Day,
            drawing.DrawingDate.Hour, drawing.DrawingDate.Minute, drawing.DrawingDate.Second);

        var descParts = expandedPicks.Select(p => BuildDescription(p, drawing.Name, request.DateToPlay));
        var description = string.Join("; ", descParts.Distinct());

        var ticket = new LotteryTicket
        {
            DrawingDetailId = drawing.Id,
            CustomerId = customerId,
            AgentId = agentId,
            DateToPlay = request.DateToPlay,
            EventDate = eventDate,
            Total = total,
            Description = description,
            PurchasedAt = DateTime.UtcNow,
            Picks = expandedPicks.Select(p => new LotteryPickEntry
            {
                Number1 = p.Number1,
                Number2 = p.Number2,
                Number3 = p.Number3,
                Number4 = p.Number4,
                PickType = p.PickType,
                LotteryGameType = drawing.LotteryGame.GameType,
                PlayCount = p.PlayCount,
                Amount = p.Amount,
                Cost = p.Cost,
                Prize = p.Prize
            }).ToList()
        };

        db.LotteryTickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Lottery ticket {TicketId} purchased by customer {CustomerId}, total {Total}",
            ticket.Id, customerId, total);

        return Result<TicketDto>.Success(MapToDto(ticket, drawing));
    }

    public async Task<List<TicketDto>> GetCustomerTicketsAsync(
        int customerId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = db.LotteryTickets
            .Include(t => t.DrawingDetail).ThenInclude(d => d.LotteryGame)
            .Include(t => t.Picks)
            .Where(t => t.CustomerId == customerId);

        if (from.HasValue) query = query.Where(t => t.PurchasedAt >= from.Value);
        if (to.HasValue) query = query.Where(t => t.PurchasedAt <= to.Value);

        var tickets = await query.OrderByDescending(t => t.PurchasedAt).ToListAsync(ct);
        return tickets.Select(t => MapToDto(t, t.DrawingDetail)).ToList();
    }

    public async Task<TicketDto?> GetTicketAsync(long ticketId, CancellationToken ct = default)
    {
        var ticket = await db.LotteryTickets
            .Include(t => t.DrawingDetail).ThenInclude(d => d.LotteryGame)
            .Include(t => t.Picks)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        return ticket is null ? null : MapToDto(ticket, ticket.DrawingDetail);
    }

    /// <summary>
    /// Expands user-selected picks into all permutations for Boxed type.
    /// For Straight picks, one entry per pick is returned unchanged.
    /// This preserves the original LotteryBusiness combinatorial math.
    /// </summary>
    public List<PickEntryDto> ExpandPicks(PurchaseRequest request, decimal prizeMultiplier)
    {
        var result = new List<PickEntryDto>();

        foreach (var pick in request.Picks)
        {
            if (request.PickType == PickType.Straight)
            {
                var prize = prizeMultiplier * pick.Amount;
                result.Add(new PickEntryDto(
                    pick.Number1, pick.Number2, pick.Number3, pick.Number4,
                    PickType.Straight, 1, pick.Amount, pick.Amount, prize));
            }
            else // Boxed — generate all permutations
            {
                var numbers = new List<int> { pick.Number1, pick.Number2, pick.Number3 };
                if (pick.Number4 > 0) numbers.Add(pick.Number4);

                var perms = new Permutations<int>(numbers, GenerateOption.WithoutRepetition);
                int permCount = (int)perms.Count;
                decimal splitAmount = pick.Amount / permCount;
                decimal prize = prizeMultiplier * splitAmount;

                foreach (var p in perms)
                {
                    result.Add(new PickEntryDto(
                        p[0], p[1], p[2], p.Count > 3 ? p[3] : 0,
                        PickType.Boxed, permCount, splitAmount, splitAmount, prize));
                }
            }
        }

        return result;
    }

    private static string BuildDescription(PickEntryDto pick, string drawingName, DateTime dateToPlay)
    {
        var pickStr = pick.Number4 > 0
            ? $"{pick.Number1}-{pick.Number2}-{pick.Number3}-{pick.Number4}"
            : $"{pick.Number1}-{pick.Number2}-{pick.Number3}";
        var typeStr = pick.PickType == PickType.Boxed ? "BOX" : "STR";
        return $"{dateToPlay:MMM-dd} {drawingName} {typeStr} {pickStr}";
    }

    private static TicketDto MapToDto(LotteryTicket ticket, DrawingDetail drawing) =>
        new(
            ticket.Id,
            ticket.DrawingDetailId,
            drawing.Name,
            ticket.DateToPlay,
            ticket.EventDate,
            ticket.Total,
            ticket.Description,
            ticket.PurchasedAt,
            ticket.Picks.Select(p => new PickEntryDto(
                p.Number1, p.Number2, p.Number3, p.Number4,
                p.PickType, p.PlayCount, p.Amount, p.Cost, p.Prize)).ToList());
}
