using LotteryService.Models;
using LotteryService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LotteryService.Controllers;

[ApiController]
[Route("api/lottery/tickets")]
[Authorize]
public class TicketsController(ILotteryService lotteryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Purchase([FromBody] PurchaseRequest request, CancellationToken ct)
    {
        var customerIdClaim = User.FindFirstValue("customerId");
        var agentIdClaim = User.FindFirstValue("agentId");

        if (!int.TryParse(customerIdClaim, out int customerId) ||
            !int.TryParse(agentIdClaim, out int agentId))
            return Unauthorized("Missing customer or agent identity claims.");

        var result = await lotteryService.PurchaseTicketAsync(customerId, agentId, request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "DRAWING_NOT_FOUND" => NotFound(new { error = result.Error }),
                "DRAWING_EXPIRED" => UnprocessableEntity(new { error = result.Error }),
                "INSUFFICIENT_BALANCE" => UnprocessableEntity(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }

        return CreatedAtAction(nameof(GetTicket), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetTicket(long id, CancellationToken ct)
    {
        var ticket = await lotteryService.GetTicketAsync(id, ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var customerIdClaim = User.FindFirstValue("customerId");
        if (!int.TryParse(customerIdClaim, out int customerId))
            return Unauthorized();

        var tickets = await lotteryService.GetCustomerTicketsAsync(customerId, from, to, ct);
        return Ok(tickets);
    }

    [HttpGet("preview")]
    public IActionResult PreviewPicks([FromBody] PurchaseRequest request)
    {
        var picks = lotteryService.ExpandPicks(request, 100m);
        return Ok(picks);
    }
}
