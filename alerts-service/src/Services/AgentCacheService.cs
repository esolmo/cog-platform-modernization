using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AlertsService.Services;

/// <summary>
/// Retrieves agent hierarchy data, caching results in Redis to reduce DB load
/// during high-frequency alert broadcasts.
/// </summary>
public class AgentCacheService(
    IDistributedCache cache,
    ILogger<AgentCacheService> logger) : IAgentService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<string>> GetSubAgentNamesAsync(int agentId, CancellationToken ct = default)
    {
        var cacheKey = $"subagents:{agentId}";
        var cached = await cache.GetStringAsync(cacheKey, ct);

        if (cached is not null)
        {
            var cachedList = JsonSerializer.Deserialize<List<string>>(cached);
            if (cachedList is not null)
                return cachedList.AsReadOnly();
        }

        // When Redis is unavailable, return empty list so broadcasts fall back to direct delivery
        logger.LogWarning("Agent sub-list cache miss for agent {AgentId} and no DB fallback configured", agentId);
        return [];
    }

    public async Task<int?> GetAgentIdByNameAsync(string loginName, CancellationToken ct = default)
    {
        var cacheKey = $"agentid:{loginName.ToUpperInvariant()}";
        var cached = await cache.GetStringAsync(cacheKey, ct);

        if (cached is not null && int.TryParse(cached, out var cachedId))
            return cachedId;

        return null;
    }

    /// <summary>
    /// Populates the cache entry for an agent's sub-agents.
    /// Called when an agent connects via SignalR or authenticates via the REST endpoint.
    /// </summary>
    public async Task SetSubAgentsAsync(int agentId, IReadOnlyList<string> subAgentNames, CancellationToken ct = default)
    {
        var cacheKey = $"subagents:{agentId}";
        var json = JsonSerializer.Serialize(subAgentNames);
        await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        }, ct);
    }

    public async Task SetAgentIdAsync(string loginName, int agentId, CancellationToken ct = default)
    {
        var cacheKey = $"agentid:{loginName.ToUpperInvariant()}";
        await cache.SetStringAsync(cacheKey, agentId.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        }, ct);
    }
}
