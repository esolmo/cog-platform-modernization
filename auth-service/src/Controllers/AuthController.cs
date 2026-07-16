using AuthService.Models.Requests;
using AuthService.Models.Responses;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        logger.LogInformation("Login attempt for user {LoginName} from {IpAddress}", request.LoginName, ipAddress);

        var result = await authService.LoginAsync(request, ipAddress, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning("Login failed for user {LoginName} from {IpAddress}", request.LoginName, ipAddress);
            return Unauthorized(new ProblemDetails { Title = "Authentication failed", Detail = result.Error });
        }

        logger.LogInformation("Login succeeded for user {LoginName}", request.LoginName);
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result    = await authService.RefreshAsync(request.RefreshToken, ipAddress, ct);

        if (!result.IsSuccess)
            return Unauthorized(new ProblemDetails { Title = "Token refresh failed", Detail = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await authService.GetCurrentUserAsync(userId, ct);
        if (!result.IsSuccess) return Unauthorized();

        return Ok(result.Value);
    }
}
