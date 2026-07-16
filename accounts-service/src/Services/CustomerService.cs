using AccountsService.Common;
using AccountsService.Data;
using AccountsService.Entities;
using AccountsService.Models.Requests;
using AccountsService.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountsService.Services;

public interface ICustomerService
{
    Task<Result<CustomerResponse>>               CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct);
    Task<Result<CustomerResponse>>               GetCustomerByIdAsync(int customerId, CancellationToken ct);
    Task<Result<CustomerBalanceResponse>>        GetBalanceAsync(int customerId, CancellationToken ct);
    Task<Result<CustomerResponse>>               GetCustomerByLoginAsync(string loginName, CancellationToken ct);
    Task<Result<PagedResult<CustomerResponse>>>  GetCustomersByAgentAsync(int agentId, int page, int pageSize, CancellationToken ct);
    Task<Result<CustomerResponse>>               UpdateCustomerAsync(int customerId, UpdateCustomerRequest request, CancellationToken ct);
    Task<Result<CustomerResponse>>               UpdateCreditLimitsAsync(int customerId, UpdateCreditLimitRequest request, CancellationToken ct);
    Task<Result>                                 SuspendCustomerAsync(int customerId, string updatedBy, CancellationToken ct);
    Task<Result>                                 ActivateCustomerAsync(int customerId, string updatedBy, CancellationToken ct);

    // Extended: permissions, limits, settle figure
    Task<Result<CustomerPermissionsResponse>>    GetPermissionsAsync(int customerId, CancellationToken ct);
    Task<Result<CustomerPermissionsResponse>>    UpdatePermissionsAsync(int customerId, UpdateCustomerPermissionsRequest request, CancellationToken ct);
    Task<Result<CustomerWagerLimitsResponse>>    UpdateWagerLimitsAsync(int customerId, UpdateCustomerWagerLimitsRequest request, CancellationToken ct);
    Task<Result<CustomerCasinoLimitsResponse>>   UpdateCasinoLimitsAsync(int customerId, UpdateCustomerCasinoLimitsRequest request, CancellationToken ct);
    Task<Result>                                 SetSettleFigureAsync(int customerId, UpdateCustomerSettleFigureRequest request, CancellationToken ct);

    // Extended: agent reassignment
    Task<Result<CustomerResponse>>               ReassignAgentAsync(int customerId, ReassignCustomerAgentRequest request, CancellationToken ct);

    // Extended: free play
    Task<Result<CustomerFreePlayResponse>>       AwardFreePlayAsync(int customerId, AwardFreePlayRequest request, CancellationToken ct);
    Task<Result<List<CustomerFreePlayResponse>>> GetFreePlayHistoryAsync(int customerId, CancellationToken ct);

    // Extended: comments
    Task<Result<CustomerCommentResponse>>        AddCommentAsync(int customerId, AddCustomerCommentRequest request, CancellationToken ct);
    Task<Result<List<CustomerCommentResponse>>>  GetCommentsAsync(int customerId, bool includeCustomerVisible, CancellationToken ct);

    // Extended: batch transactions
    Task<Result<int>>                            BatchTransactionAsync(BatchTransactionRequest request, CancellationToken ct);
}

public class CustomerService(
    AccountsDbContext db,
    IBettingProvisioningService bettingProvisioning,
    ILogger<CustomerService> logger) : ICustomerService
{
    // -------------------------------------------------------------------------
    // Core CRUD
    // -------------------------------------------------------------------------

    public async Task<Result<CustomerResponse>> CreateCustomerAsync(
        CreateCustomerRequest request, CancellationToken ct)
    {
        if (await db.Customers.AnyAsync(c => c.LoginName == request.LoginName, ct))
        {
            return Result<CustomerResponse>.Failure(
                $"Login name '{request.LoginName}' is already taken.",
                "DUPLICATE_LOGIN");
        }

        var agent = await db.Agents.FindAsync([request.AgentId], ct);
        if (agent is null)
        {
            return Result<CustomerResponse>.Failure(
                $"Agent {request.AgentId} not found.",
                "AGENT_NOT_FOUND");
        }

        if (request.CreditLimit > agent.CreditLimitMax)
        {
            return Result<CustomerResponse>.Failure(
                $"Credit limit {request.CreditLimit} exceeds agent maximum {agent.CreditLimitMax}.",
                "CREDIT_LIMIT_EXCEEDS_AGENT_MAX");
        }

        var customer = new Customer
        {
            LoginName          = request.LoginName,
            AlternateLoginName = request.AlternateLoginName,
            AgentId            = request.AgentId,
            Email              = request.Email,
            Phone              = request.Phone,
            OddsFormat         = request.OddsFormat,
            CreatedBy          = request.CreatedBy,
            Balance = new CustomerBalance
            {
                CreditLimit = request.CreditLimit,
                WagerLimit  = request.WagerLimit
            },
            Limits = new CustomerLimits
            {
                MaxStraightWager = request.MaxStraightWager,
                MaxParlayWager   = request.MaxParlayWager,
                MaxParlayPayout  = request.MaxParlayPayout,
                MaxTeaserWager   = request.MaxTeaserWager,
                MaxIfBetWager    = request.MaxIfBetWager,
                HardCreditLimit  = request.HardCreditLimit
            },
            // Default permissions: all products on
            Permissions = new CustomerPermissions
            {
                UpdatedBy = request.CreatedBy,
                UpdatedAt = DateTime.UtcNow
            }
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created customer {LoginName} (Id={Id}) under agent {AgentId}",
            customer.LoginName, customer.Id, customer.AgentId);

        await bettingProvisioning.ProvisionCustomerAsync(
            customer.LoginName,
            agent.LoginName,
            customer.Email,
            customer.Phone,
            request.MaxStraightWager,
            request.MaxParlayWager,
            request.MaxTeaserWager,
            request.MaxIfBetWager,
            request.CreditLimit,
            ct);

        return Result<CustomerResponse>.Success(MapToResponse(customer, agent.LoginName));
    }

    public async Task<Result<CustomerResponse>> GetCustomerByIdAsync(int customerId, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Balance)
            .Include(c => c.Limits)
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result<CustomerResponse>.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        return Result<CustomerResponse>.Success(MapToResponse(customer, customer.Agent.LoginName));
    }

    public async Task<Result<CustomerBalanceResponse>> GetBalanceAsync(int customerId, CancellationToken ct)
    {
        var balance = await db.CustomerBalances
            .FirstOrDefaultAsync(b => b.CustomerId == customerId, ct);

        if (balance is null)
            return Result<CustomerBalanceResponse>.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        return Result<CustomerBalanceResponse>.Success(
            new CustomerBalanceResponse(balance.CreditLimit, balance.CurrentBalance, balance.AvailableCredit));
    }

    public async Task<Result<CustomerResponse>> GetCustomerByLoginAsync(string loginName, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Balance)
            .Include(c => c.Limits)
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.LoginName == loginName, ct);

        if (customer is null)
            return Result<CustomerResponse>.Failure($"Customer '{loginName}' not found.", "NOT_FOUND");

        return Result<CustomerResponse>.Success(MapToResponse(customer, customer.Agent.LoginName));
    }

    public async Task<Result<PagedResult<CustomerResponse>>> GetCustomersByAgentAsync(
        int agentId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Customers
            .Include(c => c.Balance)
            .Include(c => c.Agent)
            .Where(c => c.AgentId == agentId)
            .OrderBy(c => c.LoginName);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var responses = items.Select(c => MapToResponse(c, c.Agent.LoginName)).ToList();

        return Result<PagedResult<CustomerResponse>>.Success(new PagedResult<CustomerResponse>
        {
            Items      = responses,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        });
    }

    public async Task<Result<CustomerResponse>> UpdateCustomerAsync(
        int customerId, UpdateCustomerRequest request, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Balance)
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result<CustomerResponse>.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        if (request.AlternateLoginName is not null)
            customer.AlternateLoginName = request.AlternateLoginName;

        if (request.Email is not null)
            customer.Email = request.Email;

        if (request.Phone is not null)
            customer.Phone = request.Phone;

        if (request.OddsFormat.HasValue)
            customer.OddsFormat = request.OddsFormat.Value;

        if (request.InstantActionEnabled.HasValue)
            customer.InstantActionEnabled = request.InstantActionEnabled.Value;

        customer.UpdatedBy = request.UpdatedBy;
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated customer {Id} by {UpdatedBy}", customerId, request.UpdatedBy);

        return Result<CustomerResponse>.Success(MapToResponse(customer, customer.Agent.LoginName));
    }

    public async Task<Result<CustomerResponse>> UpdateCreditLimitsAsync(
        int customerId, UpdateCreditLimitRequest request, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Balance)
            .Include(c => c.Limits)
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result<CustomerResponse>.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        if (request.CreditLimit > customer.Agent.CreditLimitMax)
        {
            return Result<CustomerResponse>.Failure(
                $"Credit limit {request.CreditLimit} exceeds agent maximum {customer.Agent.CreditLimitMax}.",
                "CREDIT_LIMIT_EXCEEDS_AGENT_MAX");
        }

        customer.Balance.CreditLimit    = request.CreditLimit;
        customer.Balance.WagerLimit     = request.WagerLimit;
        customer.Limits.HardCreditLimit = request.HardCreditLimit;
        customer.UpdatedBy              = request.UpdatedBy;
        customer.UpdatedAt              = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated credit limits for customer {Id}: CreditLimit={CreditLimit}",
            customerId, request.CreditLimit);

        return Result<CustomerResponse>.Success(MapToResponse(customer, customer.Agent.LoginName));
    }

    public async Task<Result> SuspendCustomerAsync(int customerId, string updatedBy, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([customerId], ct);
        if (customer is null)
            return Result.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        customer.Status    = CustomerStatus.Suspended;
        customer.IsActive  = false;
        customer.UpdatedBy = updatedBy;
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Suspended customer {Id} by {UpdatedBy}", customerId, updatedBy);
        return Result.Success();
    }

    public async Task<Result> ActivateCustomerAsync(int customerId, string updatedBy, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([customerId], ct);
        if (customer is null)
            return Result.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        customer.Status    = CustomerStatus.Active;
        customer.IsActive  = true;
        customer.UpdatedBy = updatedBy;
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Activated customer {Id} by {UpdatedBy}", customerId, updatedBy);
        return Result.Success();
    }

    // -------------------------------------------------------------------------
    // Permissions (replaces BitPermission bitmask SPs)
    // -------------------------------------------------------------------------

    public async Task<Result<CustomerPermissionsResponse>> GetPermissionsAsync(
        int customerId, CancellationToken ct)
    {
        var perms = await db.CustomerPermissions
            .FirstOrDefaultAsync(p => p.CustomerId == customerId, ct);

        if (perms is null)
            return Result<CustomerPermissionsResponse>.Failure(
                $"Permissions for customer {customerId} not found.", "NOT_FOUND");

        return Result<CustomerPermissionsResponse>.Success(MapPermissions(perms));
    }

    public async Task<Result<CustomerPermissionsResponse>> UpdatePermissionsAsync(
        int customerId, UpdateCustomerPermissionsRequest request, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Permissions)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result<CustomerPermissionsResponse>.Failure(
                $"Customer {customerId} not found.", "NOT_FOUND");

        // Upsert: create default permissions if they don't exist yet
        var perms = customer.Permissions ?? new CustomerPermissions { CustomerId = customerId };
        var isNew = customer.Permissions is null;

        if (request.WebSportsEnabled.HasValue)  perms.WebSportsEnabled  = request.WebSportsEnabled.Value;
        if (request.CallInEnabled.HasValue)      perms.CallInEnabled     = request.CallInEnabled.Value;
        if (request.InternetEnabled.HasValue)    perms.InternetEnabled   = request.InternetEnabled.Value;
        if (request.RacebookEnabled.HasValue)    perms.RacebookEnabled   = request.RacebookEnabled.Value;
        if (request.CasinoEnabled.HasValue)      perms.CasinoEnabled     = request.CasinoEnabled.Value;
        if (request.LotteryEnabled.HasValue)     perms.LotteryEnabled    = request.LotteryEnabled.Value;
        if (request.LiveDealerEnabled.HasValue)  perms.LiveDealerEnabled = request.LiveDealerEnabled.Value;
        if (request.HorseEnabled.HasValue)       perms.HorseEnabled      = request.HorseEnabled.Value;
        if (request.ParlayEnabled.HasValue)      perms.ParlayEnabled     = request.ParlayEnabled.Value;
        if (request.TeaserEnabled.HasValue)      perms.TeaserEnabled     = request.TeaserEnabled.Value;
        if (request.IfBetEnabled.HasValue)       perms.IfBetEnabled      = request.IfBetEnabled.Value;
        if (request.ReverseEnabled.HasValue)     perms.ReverseEnabled    = request.ReverseEnabled.Value;
        if (request.AccountLocked.HasValue)      perms.AccountLocked     = request.AccountLocked.Value;
        if (request.ReceiveAlerts.HasValue)      perms.ReceiveAlerts     = request.ReceiveAlerts.Value;

        perms.UpdatedBy = request.UpdatedBy;
        perms.UpdatedAt = DateTime.UtcNow;

        if (isNew)
            db.CustomerPermissions.Add(perms);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated permissions for customer {Id} by {UpdatedBy}", customerId, request.UpdatedBy);
        return Result<CustomerPermissionsResponse>.Success(MapPermissions(perms));
    }

    // -------------------------------------------------------------------------
    // Wager and casino limits (replaces legacy SP UpdateCustomerLimits)
    // -------------------------------------------------------------------------

    public async Task<Result<CustomerWagerLimitsResponse>> UpdateWagerLimitsAsync(
        int customerId, UpdateCustomerWagerLimitsRequest request, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Limits)
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result<CustomerWagerLimitsResponse>.Failure(
                $"Customer {customerId} not found.", "NOT_FOUND");

        var limits = customer.Limits;

        if (request.MaxStraightWager.HasValue) limits.MaxStraightWager = request.MaxStraightWager.Value;
        if (request.MaxParlayWager.HasValue)   limits.MaxParlayWager   = request.MaxParlayWager.Value;
        if (request.MaxParlayPayout.HasValue)  limits.MaxParlayPayout  = request.MaxParlayPayout.Value;
        if (request.MaxTeaserWager.HasValue)   limits.MaxTeaserWager   = request.MaxTeaserWager.Value;
        if (request.MaxIfBetWager.HasValue)    limits.MaxIfBetWager    = request.MaxIfBetWager.Value;
        if (request.MaxLotteryPick3.HasValue)  limits.MaxLotteryPick3  = request.MaxLotteryPick3.Value;
        if (request.MaxLotteryPick4.HasValue)  limits.MaxLotteryPick4  = request.MaxLotteryPick4.Value;
        if (request.MaxParlayLegs.HasValue)    limits.MaxParlayLegs    = request.MaxParlayLegs.Value;
        if (request.MinimumWager.HasValue)     limits.MinimumWager     = request.MinimumWager.Value;

        customer.UpdatedBy = request.UpdatedBy;
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated wager limits for customer {Id} by {UpdatedBy}", customerId, request.UpdatedBy);

        return Result<CustomerWagerLimitsResponse>.Success(new CustomerWagerLimitsResponse
        {
            CustomerId       = customerId,
            MaxStraightWager = limits.MaxStraightWager,
            MaxParlayWager   = limits.MaxParlayWager,
            MaxParlayPayout  = limits.MaxParlayPayout,
            MaxTeaserWager   = limits.MaxTeaserWager,
            MaxIfBetWager    = limits.MaxIfBetWager,
            MaxLotteryPick3  = limits.MaxLotteryPick3,
            MaxLotteryPick4  = limits.MaxLotteryPick4,
            MaxParlayLegs    = limits.MaxParlayLegs,
            MinimumWager     = limits.MinimumWager
        });
    }

    public async Task<Result<CustomerCasinoLimitsResponse>> UpdateCasinoLimitsAsync(
        int customerId, UpdateCustomerCasinoLimitsRequest request, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Limits)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result<CustomerCasinoLimitsResponse>.Failure(
                $"Customer {customerId} not found.", "NOT_FOUND");

        var limits = customer.Limits;

        if (request.CasinoWagerLimit.HasValue)  limits.CasinoWagerLimit  = request.CasinoWagerLimit.Value;
        if (request.CasinoCreditLimit.HasValue) limits.CasinoCreditLimit = request.CasinoCreditLimit.Value;

        customer.UpdatedBy = request.UpdatedBy;
        customer.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated casino limits for customer {Id} by {UpdatedBy}", customerId, request.UpdatedBy);

        return Result<CustomerCasinoLimitsResponse>.Success(new CustomerCasinoLimitsResponse
        {
            CustomerId       = customerId,
            CasinoWagerLimit  = limits.CasinoWagerLimit,
            CasinoCreditLimit = limits.CasinoCreditLimit
        });
    }

    // -------------------------------------------------------------------------
    // Settle figure (replaces legacy spSetSettleFigure)
    // -------------------------------------------------------------------------

    public async Task<Result> SetSettleFigureAsync(
        int customerId, UpdateCustomerSettleFigureRequest request, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Limits)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        customer.Limits.SettleFigure = request.SettleFigure;
        customer.UpdatedBy           = request.UpdatedBy;
        customer.UpdatedAt           = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Set settle figure {Amount} for customer {Id} by {UpdatedBy}",
            request.SettleFigure, customerId, request.UpdatedBy);

        return Result.Success();
    }

    // -------------------------------------------------------------------------
    // Agent reassignment (replaces ChangeCustomerAgent / ChangeCustomerAgentInherited SPs)
    // -------------------------------------------------------------------------

    public async Task<Result<CustomerResponse>> ReassignAgentAsync(
        int customerId, ReassignCustomerAgentRequest request, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Balance)
            .Include(c => c.Limits)
            .Include(c => c.Permissions)
            .Include(c => c.Agent)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result<CustomerResponse>.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        var newAgent = await db.Agents.FindAsync([request.NewAgentId], ct);
        if (newAgent is null)
            return Result<CustomerResponse>.Failure(
                $"Target agent {request.NewAgentId} not found.", "AGENT_NOT_FOUND");

        // Validate customer's credit limit still fits under the new agent
        if (customer.Balance.CreditLimit > newAgent.CreditLimitMax)
        {
            return Result<CustomerResponse>.Failure(
                $"Customer credit limit {customer.Balance.CreditLimit} exceeds new agent maximum {newAgent.CreditLimitMax}.",
                "CREDIT_LIMIT_EXCEEDS_AGENT_MAX");
        }

        var previousAgentId = customer.AgentId;
        customer.AgentId   = request.NewAgentId;
        customer.UpdatedBy = request.UpdatedBy;
        customer.UpdatedAt = DateTime.UtcNow;

        // InheritAgentSettings: inherit max limits from the new agent
        // (equivalent to ChangeCustomerAgentInherited which copies limit/wager settings)
        if (request.InheritAgentSettings)
        {
            customer.Balance.CreditLimit = newAgent.CreditLimitMax;
            customer.Balance.WagerLimit  = newAgent.WagerLimitMax;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Reassigned customer {Id} from agent {OldAgentId} to agent {NewAgentId} by {UpdatedBy}",
            customerId, previousAgentId, request.NewAgentId, request.UpdatedBy);

        return Result<CustomerResponse>.Success(MapToResponse(customer, newAgent.LoginName));
    }

    // -------------------------------------------------------------------------
    // Free play (replaces AgCustFreePlayFrame.asp logic)
    // -------------------------------------------------------------------------

    public async Task<Result<CustomerFreePlayResponse>> AwardFreePlayAsync(
        int customerId, AwardFreePlayRequest request, CancellationToken ct)
    {
        var customer = await db.Customers
            .Include(c => c.Balance)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer is null)
            return Result<CustomerFreePlayResponse>.Failure($"Customer {customerId} not found.", "NOT_FOUND");

        var freePlay = new CustomerFreePlay
        {
            CustomerId  = customerId,
            Amount      = request.Amount,
            Description = request.Description,
            IssuedBy    = request.IssuedBy,
            ExpiresAt   = request.ExpiresAt,
            IssuedAt    = DateTime.UtcNow
        };

        db.CustomerFreePlays.Add(freePlay);

        // Update the customer's free play balance
        customer.Balance.FreePlayBalance += request.Amount;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Awarded free play ${Amount} to customer {Id} by {IssuedBy}",
            request.Amount, customerId, request.IssuedBy);

        return Result<CustomerFreePlayResponse>.Success(MapFreePlay(freePlay));
    }

    public async Task<Result<List<CustomerFreePlayResponse>>> GetFreePlayHistoryAsync(
        int customerId, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            return Result<List<CustomerFreePlayResponse>>.Failure(
                $"Customer {customerId} not found.", "NOT_FOUND");

        var items = await db.CustomerFreePlays
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.IssuedAt)
            .ToListAsync(ct);

        return Result<List<CustomerFreePlayResponse>>.Success(items.Select(MapFreePlay).ToList());
    }

    // -------------------------------------------------------------------------
    // Comments (replaces CommentsForTW / CommentsForCustomer)
    // -------------------------------------------------------------------------

    public async Task<Result<CustomerCommentResponse>> AddCommentAsync(
        int customerId, AddCustomerCommentRequest request, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            return Result<CustomerCommentResponse>.Failure(
                $"Customer {customerId} not found.", "NOT_FOUND");

        var comment = new CustomerComment
        {
            CustomerId        = customerId,
            Body              = request.Body,
            VisibleToCustomer = request.VisibleToCustomer,
            VisibleToAgent    = request.VisibleToAgent,
            CreatedBy         = request.CreatedBy,
            CreatedAt         = DateTime.UtcNow
        };

        db.CustomerComments.Add(comment);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Added comment to customer {Id} by {CreatedBy}", customerId, request.CreatedBy);
        return Result<CustomerCommentResponse>.Success(MapComment(comment));
    }

    public async Task<Result<List<CustomerCommentResponse>>> GetCommentsAsync(
        int customerId, bool includeCustomerVisible, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            return Result<List<CustomerCommentResponse>>.Failure(
                $"Customer {customerId} not found.", "NOT_FOUND");

        var query = db.CustomerComments.Where(c => c.CustomerId == customerId);

        if (!includeCustomerVisible)
            query = query.Where(c => c.VisibleToAgent);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return Result<List<CustomerCommentResponse>>.Success(items.Select(MapComment).ToList());
    }

    // -------------------------------------------------------------------------
    // Batch transactions (replaces AgICBatchTransFrame.asp / spBatchTransactions SP)
    // -------------------------------------------------------------------------

    public async Task<Result<int>> BatchTransactionAsync(
        BatchTransactionRequest request, CancellationToken ct)
    {
        if (request.Transactions.Count == 0)
            return Result<int>.Failure("No transactions provided.", "EMPTY_BATCH");

        var customerIds = request.Transactions.Select(t => t.CustomerId).Distinct().ToList();

        var customers = await db.Customers
            .Include(c => c.Balance)
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var missingIds = customerIds.Except(customers.Keys).ToList();
        if (missingIds.Count > 0)
        {
            return Result<int>.Failure(
                $"Customers not found: {string.Join(", ", missingIds)}",
                "CUSTOMERS_NOT_FOUND");
        }

        var transactions = new List<CustomerTransaction>();

        foreach (var item in request.Transactions)
        {
            var customer = customers[item.CustomerId];
            var type     = item.Type.ToUpperInvariant();

            if (type != "CREDIT" && type != "DEBIT")
            {
                return Result<int>.Failure(
                    $"Invalid transaction type '{item.Type}' for customer {item.CustomerId}. Must be Credit or Debit.",
                    "INVALID_TYPE");
            }

            if (type == "CREDIT")
                customer.Balance.CurrentBalance -= item.Amount;  // credit reduces amount owed
            else
                customer.Balance.CurrentBalance += item.Amount;  // debit increases amount owed

            transactions.Add(new CustomerTransaction
            {
                CustomerId      = item.CustomerId,
                AgentId         = customer.AgentId,
                Code            = type == "CREDIT" ? TransactionCode.Credit : TransactionCode.Debit,
                Type            = TransactionType.ManualCorrection,
                Amount          = item.Amount,
                BalanceBefore   = type == "CREDIT"
                    ? customer.Balance.CurrentBalance + item.Amount
                    : customer.Balance.CurrentBalance - item.Amount,
                BalanceAfter    = customer.Balance.CurrentBalance,
                Description     = item.Note,
                EnteredBy       = request.CreatedBy,
                TransactionDate = DateTime.UtcNow
            });
        }

        db.CustomerTransactions.AddRange(transactions);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Processed batch of {Count} transactions by {CreatedBy}",
            transactions.Count, request.CreatedBy);

        return Result<int>.Success(transactions.Count);
    }

    // -------------------------------------------------------------------------
    // Mapping helpers
    // -------------------------------------------------------------------------

    private static CustomerResponse MapToResponse(Customer customer, string agentLoginName) =>
        new()
        {
            Id                   = customer.Id,
            LoginName            = customer.LoginName,
            AlternateLoginName   = customer.AlternateLoginName,
            AgentId              = customer.AgentId,
            AgentLoginName       = agentLoginName,
            Status               = customer.Status.ToString(),
            OddsFormat           = customer.OddsFormat.ToString(),
            InstantActionEnabled = customer.InstantActionEnabled,
            Email                = customer.Email,
            Phone                = customer.Phone,
            CreatedAt            = customer.CreatedAt,
            Balance = customer.Balance is null ? new() : new BalanceSummary
            {
                CreditLimit         = customer.Balance.CreditLimit,
                WagerLimit          = customer.Balance.WagerLimit,
                CurrentBalance      = customer.Balance.CurrentBalance,
                AvailableCredit     = customer.Balance.AvailableCredit,
                PendingWagerBalance = customer.Balance.PendingWagerBalance,
                PendingWagerCount   = customer.Balance.PendingWagerCount,
                FreePlayBalance     = customer.Balance.FreePlayBalance
            }
        };

    private static CustomerPermissionsResponse MapPermissions(CustomerPermissions p) =>
        new()
        {
            CustomerId       = p.CustomerId,
            WebSportsEnabled = p.WebSportsEnabled,
            CallInEnabled    = p.CallInEnabled,
            InternetEnabled  = p.InternetEnabled,
            RacebookEnabled  = p.RacebookEnabled,
            CasinoEnabled    = p.CasinoEnabled,
            LotteryEnabled   = p.LotteryEnabled,
            LiveDealerEnabled = p.LiveDealerEnabled,
            HorseEnabled     = p.HorseEnabled,
            ParlayEnabled    = p.ParlayEnabled,
            TeaserEnabled    = p.TeaserEnabled,
            IfBetEnabled     = p.IfBetEnabled,
            ReverseEnabled   = p.ReverseEnabled,
            AccountLocked    = p.AccountLocked,
            ReceiveAlerts    = p.ReceiveAlerts,
            UpdatedAt        = p.UpdatedAt,
            UpdatedBy        = p.UpdatedBy
        };

    private static CustomerFreePlayResponse MapFreePlay(CustomerFreePlay f) =>
        new()
        {
            Id            = f.Id,
            CustomerId    = f.CustomerId,
            Amount        = f.Amount,
            Description   = f.Description,
            IssuedAt      = f.IssuedAt,
            IssuedBy      = f.IssuedBy,
            ExpiresAt     = f.ExpiresAt,
            IsRedeemed    = f.IsRedeemed,
            RedeemedAmount = f.RedeemedAmount
        };

    private static CustomerCommentResponse MapComment(CustomerComment c) =>
        new()
        {
            Id               = c.Id,
            CustomerId       = c.CustomerId,
            Body             = c.Body,
            VisibleToCustomer = c.VisibleToCustomer,
            VisibleToAgent   = c.VisibleToAgent,
            CreatedAt        = c.CreatedAt,
            CreatedBy        = c.CreatedBy
        };
}
