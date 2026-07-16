using AccountsService.Common;
using AccountsService.Data;
using AccountsService.Entities;
using AccountsService.Models.Requests;
using AccountsService.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace AccountsService.Services;

public interface ITransactionService
{
    Task<Result<TransactionResponse>> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken ct);
    Task<BatchTransactionResponse> CreateBatchAsync(BatchCreateTransactionRequest request, CancellationToken ct);
    Task<Result<PagedResult<TransactionResponse>>> GetTransactionsAsync(int customerId, int page, int pageSize, CancellationToken ct);
    Task<Result<TransactionResponse>> GetTransactionByIdAsync(int transactionId, CancellationToken ct);
    Task<Result<TransactionResponse>> VerifyTransactionAsync(int transactionId, string verifiedBy, CancellationToken ct);
}

public class TransactionService(AccountsDbContext db, ILogger<TransactionService> logger) : ITransactionService
{
    public async Task<Result<TransactionResponse>> CreateTransactionAsync(
        CreateTransactionRequest request, CancellationToken ct)
    {
        await using var dbTransaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var customer = await db.Customers
                .Include(c => c.Balance)
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

            if (customer is null)
                return Result<TransactionResponse>.Failure(
                    $"Customer {request.CustomerId} not found.", "NOT_FOUND");

            if (!customer.IsActive)
                return Result<TransactionResponse>.Failure(
                    "Cannot create transaction for inactive customer.", "CUSTOMER_INACTIVE");

            var balanceBefore = customer.Balance.CurrentBalance;
            var balanceAfter  = request.Code == TransactionCode.Credit
                ? balanceBefore + request.Amount
                : balanceBefore - request.Amount;

            // Hard credit limit check for debits
            if (request.Code == TransactionCode.Debit)
            {
                var availableCredit = customer.Balance.AvailableCredit;
                if (request.Amount > availableCredit)
                {
                    return Result<TransactionResponse>.Failure(
                        $"Insufficient credit: available {availableCredit:F2}, requested {request.Amount:F2}.",
                        "INSUFFICIENT_CREDIT");
                }
            }

            customer.Balance.CurrentBalance = balanceAfter;

            var transaction = new CustomerTransaction
            {
                CustomerId      = request.CustomerId,
                AgentId         = customer.AgentId,
                Code            = request.Code,
                Type            = request.Type,
                Amount          = request.Amount,
                BalanceBefore   = balanceBefore,
                BalanceAfter    = balanceAfter,
                Description     = request.Description,
                Reference       = request.Reference,
                PaymentMethod   = request.PaymentMethod,
                EnteredBy       = request.EnteredBy,
                ValueDate       = request.ValueDate,
                TransactionDate = DateTime.UtcNow
            };

            db.CustomerTransactions.Add(transaction);
            await db.SaveChangesAsync(ct);
            await dbTransaction.CommitAsync(ct);

            logger.LogInformation(
                "Transaction {Code} {Amount:F2} for customer {CustomerId}. Balance: {Before:F2} → {After:F2}",
                request.Code, request.Amount, request.CustomerId, balanceBefore, balanceAfter);

            return Result<TransactionResponse>.Success(MapToResponse(transaction));
        }
        catch
        {
            await dbTransaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<BatchTransactionResponse> CreateBatchAsync(
        BatchCreateTransactionRequest request, CancellationToken ct)
    {
        var results = new List<BatchTransactionLineResult>(request.Transactions.Count);
        int succeeded = 0, failed = 0;

        for (int i = 0; i < request.Transactions.Count; i++)
        {
            var lineResult = await CreateTransactionAsync(request.Transactions[i], ct);
            if (lineResult.IsSuccess)
            {
                succeeded++;
                results.Add(new BatchTransactionLineResult
                {
                    Index       = i,
                    IsSuccess   = true,
                    Transaction = lineResult.Value
                });
            }
            else
            {
                failed++;
                results.Add(new BatchTransactionLineResult
                {
                    Index     = i,
                    IsSuccess = false,
                    Error     = lineResult.Error,
                    ErrorCode = lineResult.ErrorCode
                });

                if (request.StopOnFirstError)
                    break;
            }
        }

        logger.LogInformation(
            "Batch transaction: {Succeeded} succeeded, {Failed} failed of {Total} requested",
            succeeded, failed, request.Transactions.Count);

        return new BatchTransactionResponse
        {
            TotalRequested = request.Transactions.Count,
            Succeeded      = succeeded,
            Failed         = failed,
            Results        = results
        };
    }

    public async Task<Result<PagedResult<TransactionResponse>>> GetTransactionsAsync(
        int customerId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.CustomerTransactions
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.TransactionDate);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<TransactionResponse>>.Success(new PagedResult<TransactionResponse>
        {
            Items      = items.Select(MapToResponse).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        });
    }

    public async Task<Result<TransactionResponse>> GetTransactionByIdAsync(
        int transactionId, CancellationToken ct)
    {
        var transaction = await db.CustomerTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        if (transaction is null)
            return Result<TransactionResponse>.Failure(
                $"Transaction {transactionId} not found.", "NOT_FOUND");

        return Result<TransactionResponse>.Success(MapToResponse(transaction));
    }

    public async Task<Result<TransactionResponse>> VerifyTransactionAsync(
        int transactionId, string verifiedBy, CancellationToken ct)
    {
        var transaction = await db.CustomerTransactions
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        if (transaction is null)
            return Result<TransactionResponse>.Failure(
                $"Transaction {transactionId} not found.", "NOT_FOUND");

        if (transaction.IsVerified)
            return Result<TransactionResponse>.Failure(
                "Transaction is already verified.", "ALREADY_VERIFIED");

        transaction.IsVerified  = true;
        transaction.VerifiedAt  = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Transaction {Id} verified by {VerifiedBy}", transactionId, verifiedBy);

        return Result<TransactionResponse>.Success(MapToResponse(transaction));
    }

    private static TransactionResponse MapToResponse(CustomerTransaction t) =>
        new()
        {
            Id              = t.Id,
            CustomerId      = t.CustomerId,
            Code            = t.Code.ToString(),
            Type            = t.Type.ToString(),
            Amount          = t.Amount,
            BalanceBefore   = t.BalanceBefore,
            BalanceAfter    = t.BalanceAfter,
            Description     = t.Description,
            Reference       = t.Reference,
            EnteredBy       = t.EnteredBy,
            IsVerified      = t.IsVerified,
            TransactionDate = t.TransactionDate,
            ValueDate       = t.ValueDate
        };
}
