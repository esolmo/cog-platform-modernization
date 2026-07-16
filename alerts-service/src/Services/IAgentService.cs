namespace AlertsService.Services;

public interface IAgentService
{
    /// <summary>
    /// Returns all sub-agent login names for the given agent, including sub-agents of sub-agents.
    /// Replaces GetSubAgents WCF call from COGLib.
    /// </summary>
    Task<IReadOnlyList<string>> GetSubAgentNamesAsync(int agentId, CancellationToken ct = default);

    /// <summary>
    /// Returns agent ID by login name.
    /// </summary>
    Task<int?> GetAgentIdByNameAsync(string loginName, CancellationToken ct = default);
}
