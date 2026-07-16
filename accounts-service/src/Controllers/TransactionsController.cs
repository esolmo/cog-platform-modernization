using AccountsService.Models.Requests;
using AccountsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountsService.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        var result = await transactionService.CreateTransactionAsync(request, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"          => NotFound(new { result.Error, result.ErrorCode }),
                "CUSTOMER_INACTIVE"  => UnprocessableEntity(new { result.Error, result.ErrorCode }),
                "INSUFFICIENT_CREDIT" => UnprocessableEntity(new { result.Error, result.ErrorCode }),
                _                    => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return CreatedAtAction(nameof(GetTransaction), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("batch")]
    [Authorize(Roles = "Admin,MasterAgent,Agent")]
    public async Task<IActionResult> CreateBatch(
        [FromBody] BatchCreateTransactionRequest request, CancellationToken ct)
    {
        if (request.Transactions.Count == 0)
            return BadRequest(new { error = "Batch must contain at least one transaction." });

        if (request.Transactions.Count > 200)
            return BadRequest(new { error = "Batch cannot exceed 200 transactions." });

        var result = await transactionService.CreateBatchAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTransaction(int id, CancellationToken ct)
    {
        var result = await transactionService.GetTransactionByIdAsync(id, ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error, result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpGet("by-customer/{customerId:int}")]
    public async Task<IActionResult> GetCustomerTransactions(
        int customerId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct     = default)
    {
        var result = await transactionService.GetTransactionsAsync(customerId, page, pageSize, ct);
        return Ok(result.Value);
    }

    [HttpPost("{id:int}/verify")]
    [Authorize(Roles = "Admin,MasterAgent")]
    public async Task<IActionResult> VerifyTransaction(
        int id, [FromQuery] string verifiedBy, CancellationToken ct)
    {
        var result = await transactionService.VerifyTransactionAsync(id, verifiedBy, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                "NOT_FOUND"        => NotFound(new { result.Error, result.ErrorCode }),
                "ALREADY_VERIFIED" => Conflict(new { result.Error, result.ErrorCode }),
                _                  => BadRequest(new { result.Error, result.ErrorCode })
            };
        }
        return Ok(result.Value);
    }
}
