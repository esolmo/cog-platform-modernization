namespace CasinoService.Models.Responses;

public record CasinoPlayerResponse(
    string CustomerId,
    string Nickname,
    string ExternalPlayerId,
    DateTime RegisteredAt);

public record CasinoSessionResponse(
    string CustomerId,
    string Nickname,
    string LobbyUrl,
    decimal CasinoBalance,
    decimal BonusBalance,
    decimal Playthrough);

public record CasinoBalanceResponse(
    decimal AvailableBalance,
    decimal CasinoBalance,
    decimal BonusBalance);

public record TransferResponse(
    bool Success,
    decimal AvailableBalance,
    decimal CasinoBalance,
    string? ErrorMessage = null);
