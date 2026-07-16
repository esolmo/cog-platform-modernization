using LotteryService.Entities;

namespace LotteryService.Models;

public record LotteryGameDto(int Id, string Name, LotteryGameType GameType, bool IsActive);

public record DrawingDetailDto(
    long Id,
    int LotteryGameId,
    string GameName,
    string Name,
    DateTime DrawingDate,
    string TimeZoneId,
    int MinutesToDraw);

public record PickRequest(int Number1, int Number2, int Number3, int Number4, decimal Amount);

public record PurchaseRequest(
    long DrawingDetailId,
    DateTime DateToPlay,
    PickType PickType,
    List<PickRequest> Picks);

public record PickEntryDto(
    int Number1,
    int Number2,
    int Number3,
    int Number4,
    PickType PickType,
    int PlayCount,
    decimal Amount,
    decimal Cost,
    decimal Prize);

public record TicketDto(
    long Id,
    long DrawingDetailId,
    string DrawingName,
    DateTime DateToPlay,
    DateTime EventDate,
    decimal Total,
    string Description,
    DateTime PurchasedAt,
    List<PickEntryDto> Picks);

public record PurchaseSummaryDto(
    string User,
    DateTime StartDate,
    DateTime EndDate,
    int TotalTickets,
    decimal TotalCost,
    decimal TotalPrize);

public record BalanceValidationResult(bool Sufficient, decimal Available, decimal Required);
