using CasinoService.Models.Requests;
using CasinoService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CasinoController(ICasinoService casinoService) : ControllerBase
{
    /// <summary>Register the authenticated customer as a Live Dealer player.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPlayerRequest request, CancellationToken ct)
    {
        var customerId = GetCustomerId();
        if (customerId is null) return Unauthorized();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var result = await casinoService.RegisterPlayerAsync(
            customerId, request with { IpAddress = ipAddress }, ct);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, code = result.ErrorCode });
    }

    /// <summary>Get a Live Dealer session (login + lobby URL + balances).</summary>
    [HttpGet("session")]
    public async Task<IActionResult> GetSession(CancellationToken ct)
    {
        var customerId = GetCustomerId();
        if (customerId is null) return Unauthorized();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        var result = await casinoService.GetSessionAsync(customerId, ipAddress, ct);

        if (!result.IsSuccess && result.ErrorCode == "NOT_REGISTERED")
            return NotFound(new { error = result.Error, code = result.ErrorCode });

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, code = result.ErrorCode });
    }

    /// <summary>Get COG and Live Dealer balances.</summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var customerId = GetCustomerId();
        if (customerId is null) return Unauthorized();

        var result = await casinoService.GetBalanceAsync(customerId, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, code = result.ErrorCode });
    }

    /// <summary>Deposit funds from COG account into Live Dealer.</summary>
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(
        [FromBody] TransferFundsRequest request, CancellationToken ct)
    {
        var customerId = GetCustomerId();
        if (customerId is null) return Unauthorized();

        var result = await casinoService.DepositAsync(customerId, request, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, code = result.ErrorCode });
    }

    /// <summary>Withdraw funds from Live Dealer back into COG account.</summary>
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(
        [FromBody] TransferFundsRequest request, CancellationToken ct)
    {
        var customerId = GetCustomerId();
        if (customerId is null) return Unauthorized();

        var result = await casinoService.WithdrawAsync(customerId, request, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error, code = result.ErrorCode });
    }

    private string? GetCustomerId() =>
        User.FindFirstValue("domain_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
}
