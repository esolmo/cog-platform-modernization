namespace Cog.Domain.Common;

public interface ICurrentUser
{
    int AgentId { get; }
    string LoginName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
