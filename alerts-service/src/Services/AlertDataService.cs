using AlertsService.Data;
using AlertsService.Entities;
using AlertsService.Models;
using Microsoft.EntityFrameworkCore;

namespace AlertsService.Services;

/// <summary>
/// Retrieves action alert data from the database.
/// Replaces the GetActionAlertTicket WCF operation from COGLib/ActionAlert/ActionAlert.cs.
/// </summary>
public class AlertDataService(AlertsDbContext db, ILogger<AlertDataService> logger) : IAlertDataService
{
    public async Task<IReadOnlyList<AlertTicketDto>> GetActionAlertsAsync(
        AlertFilterRequest filter,
        CancellationToken ct = default)
    {
        logger.LogDebug("Fetching action alerts for agent {AgentId}, filter amount {Amount}",
            filter.AgentId, filter.FilterAmount);

        var allowedTypes = ParseTypeFilter(filter.TypesFilter);

        var query = db.AlertTickets
            .Include(t => t.Attributes)
            .Include(t => t.Details).ThenInclude(d => d.Attributes)
            .Where(t => t.AgentId == filter.AgentId)
            .Where(t => t.ExpiresAt > DateTime.UtcNow)
            .Where(t => t.WagerNumber > filter.LastWagerNumber)
            .Where(t => t.Amount >= (decimal)filter.FilterAmount);

        if (allowedTypes.Count > 0)
            query = query.Where(t => allowedTypes.Contains((int)t.AlertType));

        if (!filter.ShowSharp)
            query = query.Where(t => !t.IsSharpAction);

        if (!filter.ShowSquare)
            query = query.Where(t => !t.IsSquareAction);

        var tickets = await query
            .OrderByDescending(t => t.InsertedAt)
            .Take(200)
            .ToListAsync(ct);

        return tickets.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<CustomerRefDto>> GetCustomersAsync(int agentId, CancellationToken ct = default)
    {
        // Returns VIP customers currently tracked for this agent.
        // Full customer lists are owned by accounts-service; the alerts service
        // maintains only the subset an agent has opted to monitor.
        var settings = await db.AgentVipSettings
            .Include(v => v.VipCustomers)
            .FirstOrDefaultAsync(v => v.AgentId == agentId, ct);

        if (settings is null)
            return [];

        return settings.VipCustomers
            .Select(c => new CustomerRefDto(c.CustomerId, c.CustomerLoginName))
            .OrderBy(c => c.LoginName)
            .ToList()
            .AsReadOnly();
    }

    public async Task<VipSettingsDto> GetVipSettingsAsync(int agentId, CancellationToken ct = default)
    {
        var settings = await db.AgentVipSettings
            .Include(v => v.VipCustomers)
            .FirstOrDefaultAsync(v => v.AgentId == agentId, ct);

        if (settings is null)
            return new VipSettingsDto([], string.Empty);

        var customers = settings.VipCustomers
            .Select(c => new CustomerRefDto(c.CustomerId, c.CustomerLoginName))
            .ToList()
            .AsReadOnly();

        return new VipSettingsDto(customers, settings.NotificationEmail);
    }

    public async Task UpdateVipSettingsAsync(UpdateVipRequest request, CancellationToken ct = default)
    {
        var settings = await db.AgentVipSettings
            .Include(v => v.VipCustomers)
            .FirstOrDefaultAsync(v => v.AgentId == request.AgentId, ct);

        if (settings is null)
        {
            settings = new AgentVipSettings { AgentId = request.AgentId };
            db.AgentVipSettings.Add(settings);
        }

        settings.NotificationEmail = request.Email;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.VipCustomers.Clear();

        foreach (var customer in request.Customers)
        {
            settings.VipCustomers.Add(new AgentVipCustomer
            {
                CustomerId = customer.Id,
                CustomerLoginName = customer.LoginName
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated VIP settings for agent {AgentId}: {Count} VIPs, email {Email}",
            request.AgentId, request.Customers.Count, request.Email);
    }

    public async Task<string> GetAgentEmailAsync(int agentId, CancellationToken ct = default)
    {
        var settings = await db.AgentVipSettings
            .Where(v => v.AgentId == agentId)
            .Select(v => v.NotificationEmail)
            .FirstOrDefaultAsync(ct);

        return settings ?? string.Empty;
    }

    public async Task<AlertTicketDto> CreateAlertAsync(CreateAlertRequest request, CancellationToken ct = default)
    {
        var ticket = new AlertTicket
        {
            WagerNumber = request.WagerNumber,
            AgentId = request.AgentId,
            CustomerId = request.CustomerId,
            CustomerLoginName = request.CustomerLoginName,
            InetWagerNumber = request.InetWagerNumber,
            WagerType = (WagerType)request.WagerType,
            AlertType = (AlertType)request.AlertType,
            Amount = request.Amount,
            Description = request.Description,
            InsertedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt
        };

        db.AlertTickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created alert {Id} for agent {AgentId}, wager {WagerNumber}",
            ticket.Id, ticket.AgentId, ticket.WagerNumber);

        return MapToDto(ticket);
    }

    public async Task<bool> DismissAlertAsync(int id, CancellationToken ct = default)
    {
        var ticket = await db.AlertTickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null)
            return false;

        db.AlertTickets.Remove(ticket);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static AlertTicketDto MapToDto(AlertTicket ticket) =>
        new(
            ticket.Id,
            ticket.WagerNumber,
            ticket.CustomerLoginName,
            ticket.InetWagerNumber,
            ticket.WagerType.ToString(),
            ticket.Amount,
            ticket.Description,
            ticket.IsVipAlert,
            ticket.IsSharpAction,
            ticket.IsSquareAction,
            ticket.InsertedAt,
            ticket.Attributes
                .Select(a => new AttributeDto(a.Name, a.Value))
                .ToList()
                .AsReadOnly(),
            ticket.Details
                .Select(d => new AlertDetailDto(
                    d.Description,
                    d.SportKey,
                    d.Attributes.Select(a => new AttributeDto(a.Name, a.Value)).ToList().AsReadOnly()))
                .ToList()
                .AsReadOnly()
        );

    private static List<int> ParseTypeFilter(string typesFilter)
    {
        if (string.IsNullOrWhiteSpace(typesFilter))
            return [];

        return typesFilter
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
            .Where(n => n > 0)
            .ToList();
    }

}
