using BettingService.Data;
using BettingService.Models.Requests;
using Cog.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BettingService.Controllers;

[ApiController]
[Route("api/internal")]
[Authorize(Roles = "Admin")]
public class InternalController(BettingDbContext db, ILogger<InternalController> logger) : ControllerBase
{
    [HttpPost("agents")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProvisionAgent(
        [FromBody] ProvisionAgentRequest request, CancellationToken ct)
    {
        if (await db.Agents.AnyAsync(a => a.LoginName == request.LoginName, ct))
        {
            logger.LogInformation("Agent {LoginName} already exists in betting-service — skipping.", request.LoginName);
            return Ok(new { message = "already exists" });
        }

        Agent? parent = null;
        if (request.ParentAgentId.HasValue)
        {
            // ParentAgentId from accounts-service doesn't match betting-service IDs.
            // The parent must already be provisioned; look it up by its accounts-service-provided login
            // indirectly via the Agent record that was provisioned earlier.
            // We store a no-op if the parent isn't found yet — provisioning order matters.
            parent = await db.Agents.FindAsync([request.ParentAgentId.Value], ct);
        }

        var agent = new Agent
        {
            LoginName    = request.LoginName,
            PasswordHash = "*",
            Name         = request.Name ?? request.LoginName,
            ParentAgentId = parent?.Id,
            AgentType    = request.AgentType,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow,
        };

        db.Agents.Add(agent);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Provisioned agent {LoginName} (Id={Id}) in betting-service.", agent.LoginName, agent.Id);
        return Ok(new { agent.Id });
    }

    [HttpPost("customers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProvisionCustomer(
        [FromBody] ProvisionCustomerRequest request, CancellationToken ct)
    {
        if (await db.Customers.AnyAsync(c => c.LoginName == request.LoginName, ct))
        {
            logger.LogInformation("Customer {LoginName} already exists in betting-service — skipping.", request.LoginName);
            return Ok(new { message = "already exists" });
        }

        var agent = await db.Agents.FirstOrDefaultAsync(a => a.LoginName == request.AgentLoginName, ct);
        if (agent is null)
        {
            logger.LogWarning("Cannot provision customer {LoginName}: agent '{AgentLogin}' not found in betting-service.",
                request.LoginName, request.AgentLoginName);
            return BadRequest(new { error = $"Agent '{request.AgentLoginName}' not provisioned yet." });
        }

        var customer = new Customer
        {
            LoginName    = request.LoginName,
            PasswordHash = "*",
            FirstName    = request.LoginName,
            LastName     = request.LoginName,
            Email        = request.Email,
            Phone        = request.Phone,
            AgentId      = agent.Id,
            Status       = CustomerStatus.Active,
            CreatedAt    = DateTime.UtcNow,
            Balance = new CustomerBalance
            {
                CreditLimit  = request.CreditLimit,
                LastUpdated  = DateTime.UtcNow,
            },
            Limits = new CustomerLimits
            {
                MaxWagerStraight = request.MaxStraightWager,
                MaxWagerParlay   = request.MaxParlayWager,
                MaxWagerTeaser   = request.MaxTeaserWager,
                MaxWagerIfBet    = request.MaxIfBetWager,
                MinWager         = request.MinWager,
            },
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Provisioned customer {LoginName} (Id={Id}) under agent {AgentLogin} in betting-service.",
            customer.LoginName, customer.Id, agent.LoginName);
        return Ok(new { customer.Id });
    }
}
