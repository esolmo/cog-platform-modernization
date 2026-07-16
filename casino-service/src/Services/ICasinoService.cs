using Cog.Domain.Common;
using CasinoService.Models.Requests;
using CasinoService.Models.Responses;

namespace CasinoService.Services;

public interface ICasinoService
{
    /// <summary>Register a new Live Dealer player profile.</summary>
    Task<Result<CasinoPlayerResponse>> RegisterPlayerAsync(
        string customerId, RegisterPlayerRequest request, CancellationToken ct = default);

    /// <summary>Log in and return a session with lobby URL + balances.</summary>
    Task<Result<CasinoSessionResponse>> GetSessionAsync(
        string customerId, string ipAddress, CancellationToken ct = default);

    /// <summary>Return COG available balance and Live Dealer casino balance.</summary>
    Task<Result<CasinoBalanceResponse>> GetBalanceAsync(
        string customerId, CancellationToken ct = default);

    /// <summary>Deposit from COG account into Live Dealer.</summary>
    Task<Result<TransferResponse>> DepositAsync(
        string customerId, TransferFundsRequest request, CancellationToken ct = default);

    /// <summary>Withdraw from Live Dealer back into COG account.</summary>
    Task<Result<TransferResponse>> WithdrawAsync(
        string customerId, TransferFundsRequest request, CancellationToken ct = default);
}
