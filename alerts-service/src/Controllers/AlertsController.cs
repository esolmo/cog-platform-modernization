using AlertsService.Hubs;
using AlertsService.Models;
using AlertsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AlertsService.Controllers;

/// <summary>
/// REST endpoints for alert data and internal broadcast trigger.
/// Replaces POST /gd, /gc, /ac, /un, /upvip, /gcvip, /getEmail in InstantAction/ac/app.js.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertsController(
    IAlertDataService alertDataService,
    IHubContext<AlertHub> hubContext,
    IEmailService emailService,
    ILogger<AlertsController> logger) : ControllerBase
{
    /// <summary>
    /// GET /api/alerts?agentId=1&amp;lastWagerNumber=0&amp;...
    /// Replaces POST /gd — returns action alert tickets for the agent.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertTicketDto>>> GetAlerts(
        [FromQuery] int agentId,
        [FromQuery] int lastWagerNumber = 0,
        [FromQuery] string typesFilter = "",
        [FromQuery] bool showSharp = true,
        [FromQuery] bool showSquare = true,
        [FromQuery] string sportsFilter = "",
        [FromQuery] decimal filterAmount = 0,
        [FromQuery] bool refreshVip = false,
        [FromQuery] string? emailAddress = null,
        CancellationToken ct = default)
    {
        var filter = new AlertFilterRequest(
            agentId, lastWagerNumber, typesFilter, showSharp, showSquare,
            sportsFilter, filterAmount, refreshVip, emailAddress);

        var tickets = await alertDataService.GetActionAlertsAsync(filter, ct);

        // Send VIP notification emails if requested (mirrors legacy /gd email logic)
        if (!string.IsNullOrEmpty(emailAddress) && tickets.Count > 0)
        {
            foreach (var ticket in tickets.Where(t => t.IsVipAlert))
            {
                await emailService.SendVipAlertEmailAsync(
                    emailAddress,
                    $"BetTicker: VIP Customer Notification — {ticket.CustomerLoginName}",
                    ticket,
                    ct);
            }
        }

        return Ok(tickets);
    }

    /// <summary>
    /// POST /api/alerts
    /// Persists a new action alert ticket.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AlertTicketDto>> CreateAlert(
        [FromBody] CreateAlertRequest request,
        CancellationToken ct = default)
    {
        var ticket = await alertDataService.CreateAlertAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, ticket);
    }

    /// <summary>
    /// DELETE /api/alerts/{id}
    /// Dismisses (removes) an alert ticket.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DismissAlert(int id, CancellationToken ct = default)
    {
        var dismissed = await alertDataService.DismissAlertAsync(id, ct);
        return dismissed ? NoContent() : NotFound();
    }

    /// <summary>
    /// POST /api/alerts/broadcast
    /// Internal endpoint called by betting-service when a new wager is placed.
    /// Broadcasts to all connected SignalR agents that have the customer in their hierarchy.
    /// Replaces POST /ac in legacy Node.js app.
    /// </summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> BroadcastAlert(
        [FromBody] BroadcastAlertRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Broadcasting alert for agent {Agent}, wager {WagerNumber}",
            request.AgentLoginName, request.Alert.WagerNumber);

        // Broadcast to all agents — clients filter by their own hierarchy
        await hubContext.Clients.All.SendAsync("alert", request.Alert, ct);

        return Ok();
    }

    /// <summary>
    /// POST /api/alerts/unalert/{customerId}
    /// Replaces POST /un — removes an alert for a customer.
    /// </summary>
    [HttpPost("unalert/{customerId:int}")]
    public async Task<IActionResult> UnAlert(int customerId, CancellationToken ct = default)
    {
        await hubContext.Clients.All.SendAsync("unAlert", customerId, ct);
        return Ok();
    }

    /// <summary>
    /// GET /api/alerts/customers/{agentId}
    /// Replaces POST /gc — returns customer list for the agent.
    /// </summary>
    [HttpGet("customers/{agentId:int}")]
    public async Task<ActionResult<IReadOnlyList<CustomerRefDto>>> GetCustomers(
        int agentId,
        CancellationToken ct = default)
    {
        var customers = await alertDataService.GetCustomersAsync(agentId, ct);
        return Ok(customers);
    }

    /// <summary>
    /// GET /api/alerts/vip/{agentId}
    /// Replaces POST /gcvip — returns VIP customer list and notification email for the agent.
    /// </summary>
    [HttpGet("vip/{agentId:int}")]
    public async Task<ActionResult<VipSettingsDto>> GetVipSettings(
        int agentId,
        CancellationToken ct = default)
    {
        var settings = await alertDataService.GetVipSettingsAsync(agentId, ct);
        return Ok(settings);
    }

    /// <summary>
    /// PUT /api/alerts/vip
    /// Replaces POST /upvip — updates VIP customer list and notification email.
    /// </summary>
    [HttpPut("vip")]
    public async Task<IActionResult> UpdateVipSettings(
        [FromBody] UpdateVipRequest request,
        CancellationToken ct = default)
    {
        await alertDataService.UpdateVipSettingsAsync(request, ct);
        return NoContent();
    }

    /// <summary>
    /// GET /api/alerts/email/{agentId}
    /// Replaces POST /getEmail — returns the agent's notification email address.
    /// </summary>
    [HttpGet("email/{agentId:int}")]
    public async Task<ActionResult<string>> GetEmail(
        int agentId,
        CancellationToken ct = default)
    {
        var email = await alertDataService.GetAgentEmailAsync(agentId, ct);
        return Ok(new { email });
    }
}
