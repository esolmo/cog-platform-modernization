namespace CasinoService.Services;

public record LiveDealerPlayerResult(
    bool Success,
    string? CustId,
    string? Nickname,
    string? Ticket,
    string? ExternalPlayerId,
    decimal Balance,
    decimal BonusBalance,
    decimal Playthrough,
    string? ErrorCode,
    string? ErrorDescription);

public record LiveDealerBalanceResult(
    bool Success,
    decimal Balance,
    decimal BonusBalance,
    string? ErrorCode,
    string? ErrorDescription);

public record LiveDealerTransferResult(
    bool Success,
    string? TransferReference,
    string? RemoteReference,
    string? ErrorCode,
    string? ErrorDescription);

public interface ILiveDealerClient
{
    Task<LiveDealerPlayerResult> AddPlayerAsync(
        string custId, string nickname, string countryCode, string ipAddress, string custSource,
        CancellationToken ct = default);

    Task<LiveDealerPlayerResult> LoginAsync(
        string custId, string nickname, string ipAddress,
        CancellationToken ct = default);

    Task<LiveDealerBalanceResult> GetBalanceAsync(
        string custId, string nickname,
        CancellationToken ct = default);

    Task<LiveDealerTransferResult> InitTransferAsync(
        string custId, decimal amount, string transferAction,
        int transferReference, string description,
        CancellationToken ct = default);

    Task<LiveDealerTransferResult> ConfirmTransferAsync(
        string custId, string transferAction,
        string transferReference, string remoteReference,
        bool includeConfirmation = false,
        CancellationToken ct = default);
}
