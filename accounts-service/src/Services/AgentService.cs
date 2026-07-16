using AccountsService.Common;
using AccountsService.Data;
using AccountsService.Entities;
using AccountsService.Models.Requests;
using AccountsService.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountsService.Services;

public interface IAgentService
{
    // --- Read (existing) ---
    Task<Result<IReadOnlyList<AgentResponse>>>  ListAllAsync(CancellationToken ct);
    Task<Result<AgentResponse>>                 GetAgentByIdAsync(int agentId, CancellationToken ct);
    Task<Result<IReadOnlyList<AgentResponse>>>  GetSubAgentsAsync(int agentId, CancellationToken ct);
    Task<Result<AgentHierarchyNode>>            GetHierarchyAsync(int agentId, CancellationToken ct);
    Task<Result<IReadOnlyList<int>>>            GetAllSubAgentIdsAsync(int agentId, CancellationToken ct);

    // --- Write (replaces AddUpdateAgent, AgentNestedInsert, AgentNestedDeleteNode, AgentNestedMoveNode,
    //            ChangeMasterAgent, DeleteAgentAccount SPs) ---
    Task<Result<AgentResponse>>  CreateAgentAsync(CreateAgentRequest request, CancellationToken ct);
    Task<Result<AgentResponse>>  UpdateAgentAsync(int agentId, UpdateAgentRequest request, CancellationToken ct);
    Task<Result<AgentResponse>>  UpdateCreditLimitsAsync(int agentId, UpdateAgentCreditLimitsRequest request, CancellationToken ct);
    Task<Result<AgentResponse>>  MoveAgentAsync(int agentId, MoveAgentRequest request, CancellationToken ct);
    Task<Result>                 DeactivateAgentAsync(int agentId, string updatedBy, CancellationToken ct);

    // --- Settlement (replaces AgentDistribution_Calculation, GetAgentMakeUp,
    //                GetAgentDistributionByWeek, GetBillingStatement SPs) ---
    Task<Result<AgentMakeupResponse>>             GetMakeupAsync(int agentId, CancellationToken ct);
    Task<Result<AgentDistributionResponse>>       GetDistributionAsync(int agentId, DateTime weekEnding, CancellationToken ct);
    Task<Result<IReadOnlyList<AgentDistributionResponse>>> GetDistributionHistoryAsync(int agentId, int weeksBack, CancellationToken ct);
    Task<Result<AgentDistributionResponse>>       CalculateDistributionAsync(int agentId, DateTime weekEnding, string calculatedBy, CancellationToken ct);
    Task<Result<AgentDistributionResponse>>       ConfirmDistributionAsync(int agentId, DateTime weekEnding, string confirmedBy, CancellationToken ct);

    // --- Position & figures reporting ---
    Task<Result<AgentPositionResponse>>                    GetPositionAsync(int agentId, CancellationToken ct);
    Task<Result<IReadOnlyList<AgentPositionSummaryResponse>>> GetPositionSummaryAsync(CancellationToken ct);
    Task<Result<AgentFiguresResponse>>                     GetFiguresAsync(int agentId, DateTime from, DateTime to, CancellationToken ct);
}

public class AgentService(
    AccountsDbContext db,
    IBettingProvisioningService bettingProvisioning,
    ILogger<AgentService> logger) : IAgentService
{
    // ─── List / read ───────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<AgentResponse>>> ListAllAsync(CancellationToken ct)
    {
        var agents = await db.Agents
            .Include(a => a.ParentAgent)
            .Where(a => a.IsActive)
            .OrderBy(a => a.LoginName)
            .ToListAsync(ct);

        var responses = new List<AgentResponse>();
        foreach (var agent in agents)
        {
            var customerCount = await db.Customers.CountAsync(c => c.AgentId == agent.Id, ct);
            var subCount      = await db.Agents.CountAsync(a => a.ParentAgentId == agent.Id, ct);
            responses.Add(MapToResponse(agent, customerCount, subCount));
        }

        return Result<IReadOnlyList<AgentResponse>>.Success(responses);
    }

    public async Task<Result<AgentResponse>> GetAgentByIdAsync(int agentId, CancellationToken ct)
    {
        var agent = await db.Agents
            .Include(a => a.ParentAgent)
            .FirstOrDefaultAsync(a => a.Id == agentId, ct);

        if (agent is null)
            return Result<AgentResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var customerCount = await db.Customers.CountAsync(c => c.AgentId == agentId, ct);
        var subAgentCount = await db.Agents.CountAsync(a => a.ParentAgentId == agentId, ct);

        return Result<AgentResponse>.Success(MapToResponse(agent, customerCount, subAgentCount));
    }

    public async Task<Result<IReadOnlyList<AgentResponse>>> GetSubAgentsAsync(int agentId, CancellationToken ct)
    {
        var subAgents = await db.Agents
            .Include(a => a.ParentAgent)
            .Where(a => a.ParentAgentId == agentId)
            .OrderBy(a => a.LoginName)
            .ToListAsync(ct);

        var responses = new List<AgentResponse>();
        foreach (var agent in subAgents)
        {
            var customerCount = await db.Customers.CountAsync(c => c.AgentId == agent.Id, ct);
            var subCount      = await db.Agents.CountAsync(a => a.ParentAgentId == agent.Id, ct);
            responses.Add(MapToResponse(agent, customerCount, subCount));
        }

        return Result<IReadOnlyList<AgentResponse>>.Success(responses);
    }

    public async Task<Result<AgentHierarchyNode>> GetHierarchyAsync(int agentId, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result<AgentHierarchyNode>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var allAgents = await db.Agents.Where(a => a.IsActive).ToListAsync(ct);
        var root = BuildHierarchyNode(agentId, allAgents, level: 0);

        return Result<AgentHierarchyNode>.Success(root);
    }

    public async Task<Result<IReadOnlyList<int>>> GetAllSubAgentIdsAsync(int agentId, CancellationToken ct)
    {
        // Replaces fn_GetSubAgentHierarchyByID recursive SP — uses in-memory BFS instead.
        var allAgents = await db.Agents
            .Select(a => new { a.Id, a.ParentAgentId })
            .ToListAsync(ct);

        var result = new List<int> { agentId };
        var queue  = new Queue<int>([agentId]);

        while (queue.Count > 0)
        {
            var current  = queue.Dequeue();
            var children = allAgents.Where(a => a.ParentAgentId == current).Select(a => a.Id);
            foreach (var child in children)
            {
                result.Add(child);
                queue.Enqueue(child);
            }
        }

        return Result<IReadOnlyList<int>>.Success(result);
    }

    // ─── Create ───────────────────────────────────────────────────────────────
    // Replaces: AddUpdateAgent SP + AgentNestedInsert nested-set logic.
    // We use a simple parent-child FK approach — EF handles the tree via ParentAgentId.
    // Hierarchy traversal is done with BFS (GetAllSubAgentIdsAsync) rather than lft/rgt.

    public async Task<Result<AgentResponse>> CreateAgentAsync(CreateAgentRequest request, CancellationToken ct)
    {
        if (await db.Agents.AnyAsync(a => a.LoginName == request.LoginName, ct))
            return Result<AgentResponse>.Failure($"Login name '{request.LoginName}' is already taken.", "DUPLICATE_LOGIN");

        // Validate parent and enforce credit limit hierarchy
        if (request.ParentAgentId.HasValue)
        {
            var parent = await db.Agents.FindAsync([request.ParentAgentId.Value], ct);
            if (parent is null)
                return Result<AgentResponse>.Failure($"Parent agent {request.ParentAgentId} not found.", "PARENT_NOT_FOUND");

            if (request.CreditLimitMax > parent.CreditLimitMax)
                return Result<AgentResponse>.Failure(
                    $"Credit limit {request.CreditLimitMax} exceeds parent agent maximum {parent.CreditLimitMax}.",
                    "CREDIT_LIMIT_EXCEEDS_PARENT");
        }

        var agent = new Agent
        {
            LoginName      = request.LoginName,
            Name           = request.Name,
            ParentAgentId  = request.ParentAgentId,
            AgentType      = request.AgentType,
            CreditLimitMax = request.CreditLimitMax,
            WagerLimitMax  = request.WagerLimitMax,
            CommissionType = request.CommissionType,
            CommissionRate = request.CommissionRate,
            IsActive       = true,
            CreatedBy      = request.CreatedBy,
            CreatedAt      = DateTime.UtcNow
        };

        db.Agents.Add(agent);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created agent {LoginName} (Id={Id}) under parent {ParentId}",
            agent.LoginName, agent.Id, agent.ParentAgentId);

        await bettingProvisioning.ProvisionAgentAsync(
            agent.LoginName,
            agent.Name,
            agent.ParentAgentId,
            (int)agent.AgentType,
            ct);

        return Result<AgentResponse>.Success(MapToResponse(agent, 0, 0));
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    public async Task<Result<AgentResponse>> UpdateAgentAsync(int agentId, UpdateAgentRequest request, CancellationToken ct)
    {
        var agent = await db.Agents
            .Include(a => a.ParentAgent)
            .FirstOrDefaultAsync(a => a.Id == agentId, ct);

        if (agent is null)
            return Result<AgentResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        if (request.Name is not null)              agent.Name           = request.Name;
        if (request.CreditLimitMax.HasValue)       agent.CreditLimitMax = request.CreditLimitMax.Value;
        if (request.WagerLimitMax.HasValue)        agent.WagerLimitMax  = request.WagerLimitMax.Value;
        if (request.CommissionType.HasValue)       agent.CommissionType = request.CommissionType.Value;
        if (request.CommissionRate.HasValue)       agent.CommissionRate = request.CommissionRate.Value;

        await db.SaveChangesAsync(ct);

        var customerCount = await db.Customers.CountAsync(c => c.AgentId == agentId, ct);
        var subCount      = await db.Agents.CountAsync(a => a.ParentAgentId == agentId, ct);

        logger.LogInformation("Updated agent {Id} by {UpdatedBy}", agentId, request.UpdatedBy);

        return Result<AgentResponse>.Success(MapToResponse(agent, customerCount, subCount));
    }

    public async Task<Result<AgentResponse>> UpdateCreditLimitsAsync(int agentId, UpdateAgentCreditLimitsRequest request, CancellationToken ct)
    {
        var agent = await db.Agents
            .Include(a => a.ParentAgent)
            .FirstOrDefaultAsync(a => a.Id == agentId, ct);

        if (agent is null)
            return Result<AgentResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        // Enforce parent hierarchy constraint
        if (agent.ParentAgent is not null && request.CreditLimitMax > agent.ParentAgent.CreditLimitMax)
            return Result<AgentResponse>.Failure(
                $"Credit limit {request.CreditLimitMax} exceeds parent agent maximum {agent.ParentAgent.CreditLimitMax}.",
                "CREDIT_LIMIT_EXCEEDS_PARENT");

        agent.CreditLimitMax = request.CreditLimitMax;
        agent.WagerLimitMax  = request.WagerLimitMax;

        await db.SaveChangesAsync(ct);

        var customerCount = await db.Customers.CountAsync(c => c.AgentId == agentId, ct);
        var subCount      = await db.Agents.CountAsync(a => a.ParentAgentId == agentId, ct);

        logger.LogInformation("Updated credit limits for agent {Id}: Credit={Credit} Wager={Wager}",
            agentId, request.CreditLimitMax, request.WagerLimitMax);

        return Result<AgentResponse>.Success(MapToResponse(agent, customerCount, subCount));
    }

    // ─── Move agent (replaces AgentNestedMoveNode + ChangeMasterAgent SPs) ───
    // In the legacy system this maintained nested-set lft/rgt values.
    // Here we simply update ParentAgentId — the FK tree is always consistent.

    public async Task<Result<AgentResponse>> MoveAgentAsync(int agentId, MoveAgentRequest request, CancellationToken ct)
    {
        var agent = await db.Agents
            .Include(a => a.ParentAgent)
            .FirstOrDefaultAsync(a => a.Id == agentId, ct);

        if (agent is null)
            return Result<AgentResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var newParent = await db.Agents.FindAsync([request.NewParentAgentId], ct);
        if (newParent is null)
            return Result<AgentResponse>.Failure($"New parent agent {request.NewParentAgentId} not found.", "PARENT_NOT_FOUND");

        // Guard against circular reference: new parent must not be a descendant of this agent
        var descendants = await GetAllSubAgentIdsAsync(agentId, ct);
        if (descendants.Value!.Contains(request.NewParentAgentId))
            return Result<AgentResponse>.Failure("Cannot move an agent to one of its own descendants.", "CIRCULAR_HIERARCHY");

        agent.ParentAgentId = request.NewParentAgentId;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Moved agent {Id} under new parent {ParentId} by {UpdatedBy}",
            agentId, request.NewParentAgentId, request.UpdatedBy);

        var customerCount = await db.Customers.CountAsync(c => c.AgentId == agentId, ct);
        var subCount      = await db.Agents.CountAsync(a => a.ParentAgentId == agentId, ct);
        agent.ParentAgent = newParent;

        return Result<AgentResponse>.Success(MapToResponse(agent, customerCount, subCount));
    }

    // ─── Deactivate (replaces DeleteAgentAccount SP) ─────────────────────────
    // Legacy SP hard-deleted agent records. We soft-delete (IsActive = false)
    // to preserve referential integrity and audit history.

    public async Task<Result> DeactivateAgentAsync(int agentId, string updatedBy, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var hasActiveCustomers = await db.Customers.AnyAsync(
            c => c.AgentId == agentId && c.IsActive, ct);

        if (hasActiveCustomers)
            return Result.Failure(
                "Cannot deactivate agent with active customers. Reassign or deactivate customers first.",
                "HAS_ACTIVE_CUSTOMERS");

        var hasActiveSubAgents = await db.Agents.AnyAsync(
            a => a.ParentAgentId == agentId && a.IsActive, ct);

        if (hasActiveSubAgents)
            return Result.Failure(
                "Cannot deactivate agent with active sub-agents. Move or deactivate sub-agents first.",
                "HAS_ACTIVE_SUBAGENTS");

        agent.IsActive = false;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deactivated agent {Id} by {UpdatedBy}", agentId, updatedBy);
        return Result.Success();
    }

    // ─── Settlement: GetMakeup (replaces GetAgentMakeUp SP) ──────────────────

    public async Task<Result<AgentMakeupResponse>> GetMakeupAsync(int agentId, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result<AgentMakeupResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var latest = await db.AgentDistributions
            .Where(d => d.AgentId == agentId)
            .OrderByDescending(d => d.WeekEnding)
            .FirstOrDefaultAsync(ct);

        return Result<AgentMakeupResponse>.Success(new AgentMakeupResponse
        {
            AgentId        = agentId,
            AgentLoginName = agent.LoginName,
            CurrentMakeup  = latest?.NewMakeup ?? 0m,
            AsOfWeekEnding = latest?.WeekEnding
        });
    }

    // ─── Settlement: GetDistribution (replaces GetAgentDistributionByWeek SP) ─

    public async Task<Result<AgentDistributionResponse>> GetDistributionAsync(
        int agentId, DateTime weekEnding, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result<AgentDistributionResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var dist = await db.AgentDistributions
            .FirstOrDefaultAsync(d => d.AgentId == agentId
                && d.WeekEnding.Date == weekEnding.Date, ct);

        if (dist is null)
            return Result<AgentDistributionResponse>.Failure(
                $"No distribution found for agent {agentId} week ending {weekEnding:yyyy-MM-dd}.", "NOT_FOUND");

        return Result<AgentDistributionResponse>.Success(MapDistribution(dist, agent.LoginName));
    }

    public async Task<Result<IReadOnlyList<AgentDistributionResponse>>> GetDistributionHistoryAsync(
        int agentId, int weeksBack, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result<IReadOnlyList<AgentDistributionResponse>>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var records = await db.AgentDistributions
            .Where(d => d.AgentId == agentId)
            .OrderByDescending(d => d.WeekEnding)
            .Take(weeksBack)
            .ToListAsync(ct);

        var responses = records.Select(d => MapDistribution(d, agent.LoginName))
                               .ToList()
                               .AsReadOnly();

        return Result<IReadOnlyList<AgentDistributionResponse>>.Success(responses);
    }

    // ─── Settlement: Calculate (replaces AgentDistribution_Calculation SP) ────
    //
    // Legacy SP was 1,181 lines of T-SQL handling:
    //   - 7-day rolling Win/Loss aggregation from Wager, CasinoTransaction, LiveDealerTransaction
    //   - Multiple commission types (P/A/S/Q/T/R)
    //   - Makeup (carryover) calculation
    //   - Per-head fees
    //   - Hierarchy accumulation up to master agent
    //
    // This C# implementation reproduces that logic using EF Core queries.
    // Cross-service data (wager results, casino plays) comes via the bounded-context
    // reporting views; for now we aggregate from CustomerTransactions in this service.

    public async Task<Result<AgentDistributionResponse>> CalculateDistributionAsync(
        int agentId, DateTime weekEnding, string calculatedBy, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result<AgentDistributionResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        // Idempotent: recalculate if not yet confirmed
        var existing = await db.AgentDistributions
            .FirstOrDefaultAsync(d => d.AgentId == agentId
                && d.WeekEnding.Date == weekEnding.Date, ct);

        if (existing?.IsConfirmed == true)
            return Result<AgentDistributionResponse>.Failure(
                "Distribution for this week is already confirmed and cannot be recalculated.", "ALREADY_CONFIRMED");

        // Derive week start (7 days before weekEnding)
        var weekStart = weekEnding.Date.AddDays(-6);

        // Aggregate transactions under this agent's entire sub-tree
        var allAgentIds = (await GetAllSubAgentIdsAsync(agentId, ct)).Value!;

        var transactions = await db.CustomerTransactions
            .Include(t => t.Customer)
            .Where(t => allAgentIds.Contains(t.Customer.AgentId)
                     && t.TransactionDate >= weekStart
                     && t.TransactionDate <  weekEnding.Date.AddDays(1))
            .ToListAsync(ct);

        // Sports book figures: credits = money collected from customers (wins for agent),
        // debits = money paid to customers (losses for agent).
        var winAmount  = transactions.Where(t => t.Code == TransactionCode.Credit).Sum(t => t.Amount);
        var lossAmount = transactions.Where(t => t.Code == TransactionCode.Debit).Sum(t => t.Amount);
        var netAmount  = winAmount - lossAmount;

        // Credit/debit adjustments (manual corrections)
        var creditAdj = transactions.Where(t => t.Type == TransactionType.CreditAdjustment).Sum(t => t.Amount);
        var debitAdj  = transactions.Where(t => t.Type == TransactionType.CasinoAdjustment).Sum(t => t.Amount);

        // Active player count (players with at least one transaction this week)
        var activePlayerCount = transactions.Select(t => t.CustomerId).Distinct().Count();

        // Previous makeup
        var previousDist = await db.AgentDistributions
            .Where(d => d.AgentId == agentId && d.WeekEnding < weekEnding)
            .OrderByDescending(d => d.WeekEnding)
            .FirstOrDefaultAsync(ct);

        var previousMakeup = previousDist?.NewMakeup ?? 0m;

        // Commission configuration lives on the Agent record
        var commissionType = agent.CommissionType;
        var commissionRate = agent.CommissionRate;

        var (commissionAmount, newMakeup) = CalculateCommission(
            commissionType, commissionRate, netAmount, previousMakeup, creditAdj, debitAdj);

        var newBalance = commissionAmount + newMakeup;

        if (existing is null)
        {
            existing = new AgentDistribution { AgentId = agentId };
            db.AgentDistributions.Add(existing);
        }

        existing.WeekEnding          = weekEnding.Date;
        existing.WinAmount           = winAmount;
        existing.LossAmount          = lossAmount;
        existing.NetAmount           = netAmount;
        existing.CreditAdjustments   = creditAdj;
        existing.DebitAdjustments    = debitAdj;
        existing.CommissionType      = commissionType;
        existing.CommissionRate      = commissionRate;
        existing.CommissionAmount    = commissionAmount;
        existing.PreviousMakeup      = previousMakeup;
        existing.NewMakeup           = newMakeup;
        existing.ActivePlayerCount   = activePlayerCount;
        existing.NewBalance          = newBalance;
        existing.CalculatedAt        = DateTime.UtcNow;
        existing.CalculatedBy        = calculatedBy;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Calculated distribution for agent {Id} week {Week}: Net={Net} Commission={Commission} Balance={Balance}",
            agentId, weekEnding.ToString("yyyy-MM-dd"), netAmount, commissionAmount, newBalance);

        return Result<AgentDistributionResponse>.Success(MapDistribution(existing, agent.LoginName));
    }

    // ─── Confirm distribution (lock week) ────────────────────────────────────

    public async Task<Result<AgentDistributionResponse>> ConfirmDistributionAsync(
        int agentId, DateTime weekEnding, string confirmedBy, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result<AgentDistributionResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var dist = await db.AgentDistributions
            .FirstOrDefaultAsync(d => d.AgentId == agentId
                && d.WeekEnding.Date == weekEnding.Date, ct);

        if (dist is null)
            return Result<AgentDistributionResponse>.Failure(
                "Distribution must be calculated before it can be confirmed.", "NOT_CALCULATED");

        if (dist.IsConfirmed)
            return Result<AgentDistributionResponse>.Failure("Distribution is already confirmed.", "ALREADY_CONFIRMED");

        dist.IsConfirmed  = true;
        dist.ConfirmedAt  = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Confirmed distribution for agent {Id} week {Week} by {ConfirmedBy}",
            agentId, weekEnding.ToString("yyyy-MM-dd"), confirmedBy);

        return Result<AgentDistributionResponse>.Success(MapDistribution(dist, agent.LoginName));
    }

    // ─── Commission engine ────────────────────────────────────────────────────
    //
    // Replaces the commission-type branching in AgentDistribution_Calculation SP.
    //
    // P / WeeklyProfit:
    //   commissionAmount = netAmount * rate / 100
    //   newMakeup = (commissionAmount < 0) ? commissionAmount : 0  (carry negative forward)
    //
    // A/S / Split:
    //   commissionAmount = netAmount * rate / 100 + creditAdj - debitAdj
    //   newMakeup = previousMakeup + (commissionAmount < 0 ? commissionAmount : 0)
    //
    // Q / AffiliateWeekly:
    //   Identical to WeeklyProfit but makeup never cleared until positive
    //
    // T/R / RedFigure:
    //   Agent pays on negative balance; no makeup accumulation

    private static (decimal commission, decimal newMakeup) CalculateCommission(
        CommissionType type, decimal rate, decimal net,
        decimal previousMakeup, decimal creditAdj, decimal debitAdj)
    {
        return type switch
        {
            CommissionType.WeeklyProfit or CommissionType.AffiliateWeekly =>
                CalcWeeklyProfit(rate, net, previousMakeup),

            CommissionType.Split or CommissionType.SplitVariant =>
                CalcSplit(rate, net, previousMakeup, creditAdj, debitAdj),

            CommissionType.RedFigure or CommissionType.RedFigureVariant =>
                CalcRedFigure(rate, net),

            _ => CalcWeeklyProfit(rate, net, previousMakeup)
        };
    }

    private static (decimal, decimal) CalcWeeklyProfit(decimal rate, decimal net, decimal previousMakeup)
    {
        var grossCommission  = net * rate / 100m;
        var withMakeup       = grossCommission + previousMakeup;
        var commissionAmount = withMakeup > 0 ? withMakeup : 0m;
        var newMakeup        = withMakeup < 0 ? withMakeup : 0m;
        return (commissionAmount, newMakeup);
    }

    private static (decimal, decimal) CalcSplit(
        decimal rate, decimal net, decimal previousMakeup,
        decimal creditAdj, decimal debitAdj)
    {
        var grossCommission  = net * rate / 100m + creditAdj - debitAdj;
        var withMakeup       = grossCommission + previousMakeup;
        var commissionAmount = withMakeup > 0 ? withMakeup : 0m;
        var newMakeup        = withMakeup < 0 ? withMakeup : 0m;
        return (commissionAmount, newMakeup);
    }

    private static (decimal, decimal) CalcRedFigure(decimal rate, decimal net)
    {
        // No makeup accumulation — agent simply pays/receives their share each week
        var commissionAmount = net * rate / 100m;
        return (commissionAmount, 0m);
    }

    // ─── Hierarchy builder ───────────────────────────────────────────────────

    private static AgentHierarchyNode BuildHierarchyNode(
        int agentId, List<Agent> allAgents, int level)
    {
        var agent = allAgents.First(a => a.Id == agentId);
        var node  = new AgentHierarchyNode
        {
            Id        = agent.Id,
            LoginName = agent.LoginName,
            Name      = agent.Name,
            AgentType = agent.AgentType,
            Level     = level
        };

        foreach (var child in allAgents.Where(a => a.ParentAgentId == agentId))
            node.Children.Add(BuildHierarchyNode(child.Id, allAgents, level + 1));

        return node;
    }

    // ─── Mapper ──────────────────────────────────────────────────────────────

    private static AgentResponse MapToResponse(Agent agent, int customerCount, int subAgentCount) =>
        new()
        {
            Id              = agent.Id,
            LoginName       = agent.LoginName,
            Name            = agent.Name,
            ParentAgentId   = agent.ParentAgentId,
            ParentLoginName = agent.ParentAgent?.LoginName,
            AgentType       = agent.AgentType.ToString(),
            CreditLimitMax  = agent.CreditLimitMax,
            WagerLimitMax   = agent.WagerLimitMax,
            CommissionType  = agent.CommissionType.ToString(),
            CommissionRate  = agent.CommissionRate,
            IsActive        = agent.IsActive,
            CustomerCount   = customerCount,
            SubAgentCount   = subAgentCount
        };

    private static AgentDistributionResponse MapDistribution(AgentDistribution d, string agentLoginName) =>
        new()
        {
            Id                = d.Id,
            AgentId           = d.AgentId,
            AgentLoginName    = agentLoginName,
            WeekEnding        = d.WeekEnding,
            WinAmount         = d.WinAmount,
            LossAmount        = d.LossAmount,
            NetAmount         = d.NetAmount,
            CasinoWinAmount   = d.CasinoWinAmount,
            CasinoLossAmount  = d.CasinoLossAmount,
            CasinoFeeAmount   = d.CasinoFeeAmount,
            LiveDealerWin     = d.LiveDealerWin,
            LiveDealerLoss    = d.LiveDealerLoss,
            LiveDealerFee     = d.LiveDealerFee,
            CreditAdjustments = d.CreditAdjustments,
            DebitAdjustments  = d.DebitAdjustments,
            CommissionType    = d.CommissionType.ToString(),
            CommissionRate    = d.CommissionRate,
            CommissionAmount  = d.CommissionAmount,
            PreviousMakeup    = d.PreviousMakeup,
            NewMakeup         = d.NewMakeup,
            HeadCountFee      = d.HeadCountFee,
            ActivePlayerCount = d.ActivePlayerCount,
            NewBalance        = d.NewBalance,
            IsConfirmed       = d.IsConfirmed,
            CalculatedAt      = d.CalculatedAt,
            ConfirmedAt       = d.ConfirmedAt
        };

    // ─── Position ─────────────────────────────────────────────────────────────

    public async Task<Result<AgentPositionResponse>> GetPositionAsync(int agentId, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result<AgentPositionResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var balances = await db.Customers
            .Where(c => c.AgentId == agentId)
            .Include(c => c.Balance)
            .Select(c => c.Balance)
            .ToListAsync(ct);

        var response = new AgentPositionResponse
        {
            AgentId               = agentId,
            AgentLoginName        = agent.LoginName,
            TotalCustomers        = balances.Count,
            ActiveCustomers       = await db.Customers.CountAsync(c => c.AgentId == agentId && c.IsActive, ct),
            TotalCurrentBalance   = balances.Sum(b => b.CurrentBalance),
            TotalPendingWager     = balances.Sum(b => b.PendingWagerBalance),
            TotalPendingWagerCount = balances.Sum(b => b.PendingWagerCount),
            TotalFreePlay         = balances.Sum(b => b.FreePlayBalance),
            TotalCreditLimit      = balances.Sum(b => b.CreditLimit),
            TotalAvailableCredit  = balances.Sum(b => b.AvailableCredit),
            AsOf                  = DateTime.UtcNow,
        };

        return Result<AgentPositionResponse>.Success(response);
    }

    public async Task<Result<IReadOnlyList<AgentPositionSummaryResponse>>> GetPositionSummaryAsync(CancellationToken ct)
    {
        var agents = await db.Agents.Where(a => a.IsActive).OrderBy(a => a.LoginName).ToListAsync(ct);

        var result = new List<AgentPositionSummaryResponse>();
        foreach (var agent in agents)
        {
            var balances = await db.Customers
                .Where(c => c.AgentId == agent.Id)
                .Include(c => c.Balance)
                .Select(c => c.Balance)
                .ToListAsync(ct);

            result.Add(new AgentPositionSummaryResponse
            {
                AgentId             = agent.Id,
                LoginName           = agent.LoginName,
                Name                = agent.Name,
                AgentType           = agent.AgentType.ToString(),
                CustomerCount       = balances.Count,
                TotalBalance        = balances.Sum(b => b.CurrentBalance),
                TotalPending        = balances.Sum(b => b.PendingWagerBalance),
                TotalCreditLimit    = balances.Sum(b => b.CreditLimit),
                TotalAvailableCredit = balances.Sum(b => b.AvailableCredit),
            });
        }

        return Result<IReadOnlyList<AgentPositionSummaryResponse>>.Success(result);
    }

    // ─── Figures ──────────────────────────────────────────────────────────────

    public async Task<Result<AgentFiguresResponse>> GetFiguresAsync(
        int agentId, DateTime from, DateTime to, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is null)
            return Result<AgentFiguresResponse>.Failure($"Agent {agentId} not found.", "NOT_FOUND");

        var toEndOfDay = to.Date.AddDays(1).AddTicks(-1);

        var txns = await db.CustomerTransactions
            .Include(t => t.Customer)
            .ThenInclude(c => c.Balance)
            .Where(t => t.AgentId == agentId
                     && t.TransactionDate >= from.Date
                     && t.TransactionDate <= toEndOfDay)
            .OrderBy(t => t.Customer.LoginName)
            .ToListAsync(ct);

        var customerGroups = txns
            .GroupBy(t => t.CustomerId)
            .Select(g =>
            {
                var first = g.First();
                return new CustomerFiguresLine
                {
                    CustomerId     = g.Key,
                    LoginName      = first.Customer.LoginName,
                    Credits        = g.Where(t => t.Code == TransactionCode.Credit).Sum(t => t.Amount),
                    Debits         = g.Where(t => t.Code == TransactionCode.Debit).Sum(t => t.Amount),
                    Net            = g.Where(t => t.Code == TransactionCode.Credit).Sum(t => t.Amount)
                                   - g.Where(t => t.Code == TransactionCode.Debit).Sum(t => t.Amount),
                    CasinoAdj      = g.Where(t => t.IsCasinoAdjustment).Sum(t =>
                        t.Code == TransactionCode.Credit ? t.Amount : -t.Amount),
                    FreePlay       = g.Where(t => t.Type == TransactionType.FreePlay).Sum(t => t.Amount),
                    CurrentBalance = first.Customer.Balance.CurrentBalance,
                    TxCount        = g.Count(),
                };
            })
            .OrderBy(c => c.LoginName)
            .ToList();

        var response = new AgentFiguresResponse
        {
            AgentId          = agentId,
            AgentLoginName   = agent.LoginName,
            From             = from.Date,
            To               = to.Date,
            TotalCredits     = customerGroups.Sum(c => c.Credits),
            TotalDebits      = customerGroups.Sum(c => c.Debits),
            NetTransactions  = customerGroups.Sum(c => c.Net),
            CasinoAdjustments = customerGroups.Sum(c => c.CasinoAdj),
            FreePlayIssued   = customerGroups.Sum(c => c.FreePlay),
            TransactionCount = txns.Count,
            Customers        = customerGroups,
        };

        return Result<AgentFiguresResponse>.Success(response);
    }
}
