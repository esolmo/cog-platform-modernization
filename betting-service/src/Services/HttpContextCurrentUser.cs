using Cog.Domain.Common;
using System.Security.Claims;

namespace BettingService.Services;

public class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int AgentId
    {
        get
        {
            var val = User?.FindFirstValue("domain_id");
            return int.TryParse(val, out var id) ? id : 0;
        }
    }

    public string LoginName =>
        User?.FindFirstValue("login_name") ?? string.Empty;

    public IReadOnlyList<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public bool IsInRole(string role) =>
        User?.IsInRole(role) ?? false;
}
