using AlertsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AlertsService.Hubs;

/// <summary>
/// SignalR hub replacing Socket.IO /al namespace from InstantAction/ac/app.js.
///
/// Client events (server → client):
///   "alert"    — new alert ticket broadcast to agents who own the customer
///   "unAlert"  — alert dismissed (customer ID)
///   "connected" — confirms connection with agent username
///
/// Client → server events:
///   "echo"    — keepalive ping (no response required)
/// </summary>
[Authorize]
#pragma warning disable CS9113 // Primary constructor parameter unused — reserved for future agent-lookup methods
public class AlertHub(
    IAgentService _agentService,
    ILogger<AlertHub> logger) : Hub
#pragma warning restore CS9113
{
    /// <summary>
    /// Called when an agent connects. Joins them to their personal group
    /// so targeted broadcasts can route to them efficiently.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var agentId = GetAgentId();
        var agentName = GetAgentName();

        if (agentId.HasValue)
        {
            // Each agent joins a group keyed by their ID for targeted delivery
            await Groups.AddToGroupAsync(Context.ConnectionId, AgentGroup(agentId.Value));
            logger.LogInformation("Agent {AgentName} (ID {AgentId}) connected via SignalR", agentName, agentId);
        }

        // Confirm connection (replaces socket.emit('au', username) in legacy code)
        await Clients.Caller.SendAsync("connected", agentName);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var agentName = GetAgentName();
        logger.LogInformation("Agent {AgentName} disconnected from SignalR", agentName);
        return base.OnDisconnectedAsync(exception);
    }

    public Task Echo() => Task.CompletedTask; // keepalive, no response needed

    public static string AgentGroup(int agentId) => $"agent:{agentId}";

    private int? GetAgentId()
    {
        var claim = Context.User?.FindFirst("agentId")?.Value
                    ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string GetAgentName() =>
        Context.User?.FindFirst(ClaimTypes.Name)?.Value
        ?? Context.User?.FindFirst("sub")?.Value
        ?? "unknown";
}
