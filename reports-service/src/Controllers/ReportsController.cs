using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportsService.Models;
using ReportsService.Services;

namespace ReportsService.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(IReportsService reportsService) : ControllerBase
{
    /// <summary>
    /// Wager activity for a ticket writer login over a date range.
    /// Replaces WebReports betMakerActivity.aspx + DbAccess.GetBetmakerActivity().
    /// </summary>
    [HttpGet("wagers")]
    [Authorize(Policy = "CanViewReports")]
    public async Task<IActionResult> GetWagerActivity(
        [FromQuery] string loginId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(loginId))
            return BadRequest("loginId is required.");

        var rows = await reportsService.GetWagerActivityAsync(new WagerActivityRequest(loginId, from, to), ct);
        return Ok(rows);
    }

    /// <summary>
    /// Changed/adjusted customer transactions for an agent hierarchy or specific customer.
    /// Replaces WebReports rptChangedTransac.aspx + DbAccess.GetChangedTransactionByPlayers().
    /// </summary>
    [HttpGet("transactions")]
    [Authorize(Policy = "CanViewReports")]
    public async Task<IActionResult> GetChangedTransactions(
        [FromQuery] int agentId,
        [FromQuery] int customerId = 0,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] bool includeAgentTransactions = false,
        CancellationToken ct = default)
    {
        var request = new ChangedTransactionsRequest(
            agentId,
            customerId,
            from ?? DateTime.UtcNow.AddDays(-7),
            to ?? DateTime.UtcNow,
            includeAgentTransactions);

        var rows = await reportsService.GetChangedTransactionsAsync(request, ct);
        return Ok(rows);
    }

    /// <summary>
    /// Search agents under a parent agent. Replaces WebReports agent search.
    /// </summary>
    [HttpGet("agents")]
    [Authorize(Policy = "CanViewReports")]
    public async Task<IActionResult> SearchAgents(
        [FromQuery] int agentId,
        [FromQuery] string search = "",
        CancellationToken ct = default)
    {
        var rows = await reportsService.SearchAgentsAsync(new AgentSearchRequest(agentId, search), ct);
        return Ok(rows);
    }

    /// <summary>
    /// Search customers under an agent hierarchy. Replaces WebReports customer search.
    /// </summary>
    [HttpGet("customers")]
    [Authorize(Policy = "CanViewReports")]
    public async Task<IActionResult> SearchCustomers(
        [FromQuery] int agentId,
        [FromQuery] string search = "",
        CancellationToken ct = default)
    {
        var rows = await reportsService.SearchCustomersAsync(new AgentSearchRequest(agentId, search), ct);
        return Ok(rows);
    }

    /// <summary>
    /// Package tracker report. Replaces WebReports rptPackageTracker.aspx + DAReports.PackageTracker().
    /// </summary>
    [HttpGet("packages")]
    [Authorize(Policy = "CanViewReports")]
    public async Task<IActionResult> GetPackageTracker(
        [FromQuery] int viewDepartment = 0,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int agentDestination = 0,
        CancellationToken ct = default)
    {
        var request = new PackageTrackerRequest(
            viewDepartment,
            from ?? DateTime.UtcNow.AddDays(-7),
            to ?? DateTime.UtcNow,
            agentDestination);

        var rows = await reportsService.GetPackageTrackerAsync(request, ct);
        return Ok(rows);
    }
}
