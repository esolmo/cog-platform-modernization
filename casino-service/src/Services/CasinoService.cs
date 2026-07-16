using Cog.Domain.Common;
using CasinoService.Configuration;
using CasinoService.Data;
using CasinoService.Entities;
using CasinoService.Models.Requests;
using CasinoService.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CasinoService.Services;

public class CasinoService(
    CasinoDbContext db,
    ILiveDealerClient liveDealerClient,
    IAccountsClient accountsClient,
    IOptions<CasinoOptions> options,
    ILogger<CasinoService> logger) : ICasinoService
{
    private readonly CasinoOptions _opts = options.Value;

    public async Task<Result<CasinoPlayerResponse>> RegisterPlayerAsync(
        string customerId, RegisterPlayerRequest request, CancellationToken ct = default)
    {
        var existing = await db.CasinoPlayers
            .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.CasinoId == _opts.CasinoId, ct);

        if (existing is not null)
            return Result<CasinoPlayerResponse>.Failure("Player already registered", "ALREADY_REGISTERED");

        var result = await liveDealerClient.AddPlayerAsync(
            customerId, request.Nickname, _opts.DefaultCountryCode,
            request.IpAddress, _opts.CustomerSource, ct);

        if (!result.Success)
        {
            logger.LogWarning("[Casino] AddPlayer failed for {CustomerId}: {Code} — {Desc}",
                customerId, result.ErrorCode, result.ErrorDescription);
            return Result<CasinoPlayerResponse>.Failure(result.ErrorDescription!, result.ErrorCode!);
        }

        var player = new CasinoPlayer
        {
            CustomerId       = customerId,
            Nickname         = result.Nickname ?? request.Nickname,
            ExternalPlayerId = result.ExternalPlayerId ?? string.Empty,
            CasinoId         = _opts.CasinoId
        };

        db.CasinoPlayers.Add(player);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("[Casino] Player registered: customerId={Id}, external={Ext}",
            customerId, player.ExternalPlayerId);

        return Result<CasinoPlayerResponse>.Success(
            new CasinoPlayerResponse(player.CustomerId, player.Nickname, player.ExternalPlayerId, player.RegisteredAt));
    }

    public async Task<Result<CasinoSessionResponse>> GetSessionAsync(
        string customerId, string ipAddress, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(customerId, ct);
        if (player is null)
            return Result<CasinoSessionResponse>.Failure("Player not registered", "NOT_REGISTERED");

        var loginResult = await liveDealerClient.LoginAsync(customerId, player.Nickname, ipAddress, ct);
        if (!loginResult.Success)
            return Result<CasinoSessionResponse>.Failure(loginResult.ErrorDescription!, loginResult.ErrorCode!);

        var lobbyUrl = $"{_opts.LobbyBaseUrl}?ticket={loginResult.Ticket}";
        return Result<CasinoSessionResponse>.Success(new CasinoSessionResponse(
            CustomerId:    customerId,
            Nickname:      player.Nickname,
            LobbyUrl:      lobbyUrl,
            CasinoBalance: loginResult.Balance,
            BonusBalance:  loginResult.BonusBalance,
            Playthrough:   loginResult.Playthrough));
    }

    public async Task<Result<CasinoBalanceResponse>> GetBalanceAsync(
        string customerId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(customerId, ct);
        if (player is null)
            return Result<CasinoBalanceResponse>.Failure("Player not registered", "NOT_REGISTERED");

        var available    = await accountsClient.GetAvailableBalanceAsync(customerId, ct);
        var casinoResult = await liveDealerClient.GetBalanceAsync(customerId, player.Nickname, ct);

        if (!casinoResult.Success)
            return Result<CasinoBalanceResponse>.Failure(casinoResult.ErrorDescription!, casinoResult.ErrorCode!);

        return Result<CasinoBalanceResponse>.Success(
            new CasinoBalanceResponse(available, casinoResult.Balance, casinoResult.BonusBalance));
    }

    public async Task<Result<TransferResponse>> DepositAsync(
        string customerId, TransferFundsRequest request, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(customerId, ct);
        if (player is null)
            return Result<TransferResponse>.Failure("Player not registered", "NOT_REGISTERED");

        var available = await accountsClient.GetAvailableBalanceAsync(customerId, ct);
        if (available < request.Amount)
            return Result<TransferResponse>.Failure("Insufficient funds", "INSUFFICIENT_FUNDS");

        // Reserve document number
        var docNum = await accountsClient.ReserveDocumentNumberAsync(customerId, ct);
        if (docNum <= 0)
            return Result<TransferResponse>.Failure("Could not reserve transaction", "TRANSACTION_ERROR");

        // Init transfer (BUY = move money into casino)
        var initResult = await liveDealerClient.InitTransferAsync(
            customerId, request.Amount, "BUY", docNum, "chip buy", ct);

        if (!initResult.Success)
        {
            await accountsClient.RollbackDocumentAsync(customerId, docNum, ct);
            return Result<TransferResponse>.Failure(initResult.ErrorDescription!, initResult.ErrorCode!);
        }

        // Confirm transfer
        var confirmResult = await liveDealerClient.ConfirmTransferAsync(
            customerId, "BUY", initResult.TransferReference!, initResult.RemoteReference!,
            includeConfirmation: true, ct);

        if (!confirmResult.Success)
        {
            await accountsClient.RollbackDocumentAsync(customerId, docNum, ct);
            return Result<TransferResponse>.Failure(confirmResult.ErrorDescription!, confirmResult.ErrorCode!);
        }

        // Persist transaction record
        var tx = new CasinoTransaction
        {
            CasinoPlayerId    = player.Id,
            TransactionType   = CasinoTransactionType.Deposit,
            Amount            = request.Amount,
            DocumentNumber    = docNum,
            TransferReference = confirmResult.TransferReference!,
            RemoteReference   = confirmResult.RemoteReference!,
            Status            = CasinoTransactionStatus.Completed,
            CompletedAt       = DateTime.UtcNow
        };
        db.CasinoTransactions.Add(tx);
        await db.SaveChangesAsync(ct);

        // Debit COG account
        await accountsClient.RecordDepositAsync(customerId, docNum, confirmResult.TransferReference!,
            confirmResult.RemoteReference!, request.Amount, ct);

        var newAvailable = await accountsClient.GetAvailableBalanceAsync(customerId, ct);
        var casinoBalance = await liveDealerClient.GetBalanceAsync(customerId, player.Nickname, ct);

        logger.LogInformation("[Casino] Deposit {Amount} for {CustomerId}, doc={Doc}", request.Amount, customerId, docNum);

        return Result<TransferResponse>.Success(
            new TransferResponse(true, newAvailable, casinoBalance.Balance));
    }

    public async Task<Result<TransferResponse>> WithdrawAsync(
        string customerId, TransferFundsRequest request, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(customerId, ct);
        if (player is null)
            return Result<TransferResponse>.Failure("Player not registered", "NOT_REGISTERED");

        var docNum = await accountsClient.ReserveDocumentNumberAsync(customerId, ct);
        if (docNum <= 0)
            return Result<TransferResponse>.Failure("Could not reserve transaction", "TRANSACTION_ERROR");

        // Init transfer (SALE = move money out of casino)
        var initResult = await liveDealerClient.InitTransferAsync(
            customerId, request.Amount, "SALE", docNum, "chip sale", ct);

        if (!initResult.Success)
        {
            await accountsClient.RollbackDocumentAsync(customerId, docNum, ct);
            return Result<TransferResponse>.Failure(initResult.ErrorDescription!, initResult.ErrorCode!);
        }

        var confirmResult = await liveDealerClient.ConfirmTransferAsync(
            customerId, "SALE", initResult.TransferReference!, initResult.RemoteReference!, ct: ct);

        if (!confirmResult.Success)
        {
            await accountsClient.RollbackDocumentAsync(customerId, docNum, ct);
            return Result<TransferResponse>.Failure(confirmResult.ErrorDescription!, confirmResult.ErrorCode!);
        }

        var tx = new CasinoTransaction
        {
            CasinoPlayerId    = player.Id,
            TransactionType   = CasinoTransactionType.Withdrawal,
            Amount            = request.Amount,
            DocumentNumber    = docNum,
            TransferReference = confirmResult.TransferReference!,
            RemoteReference   = confirmResult.RemoteReference!,
            Status            = CasinoTransactionStatus.Completed,
            CompletedAt       = DateTime.UtcNow
        };
        db.CasinoTransactions.Add(tx);
        await db.SaveChangesAsync(ct);

        await accountsClient.RecordWithdrawalAsync(customerId, docNum, confirmResult.TransferReference!,
            confirmResult.RemoteReference!, request.Amount, ct);

        var newAvailable = await accountsClient.GetAvailableBalanceAsync(customerId, ct);
        var casinoBalance = await liveDealerClient.GetBalanceAsync(customerId, player.Nickname, ct);

        logger.LogInformation("[Casino] Withdraw {Amount} for {CustomerId}, doc={Doc}", request.Amount, customerId, docNum);

        return Result<TransferResponse>.Success(
            new TransferResponse(true, newAvailable, casinoBalance.Balance));
    }

    private Task<CasinoPlayer?> GetPlayerAsync(string customerId, CancellationToken ct) =>
        db.CasinoPlayers.FirstOrDefaultAsync(
            p => p.CustomerId == customerId && p.CasinoId == _opts.CasinoId, ct);
}
