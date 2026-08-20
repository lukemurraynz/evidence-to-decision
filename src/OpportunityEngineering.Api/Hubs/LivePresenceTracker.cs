using System.Collections.Concurrent;

namespace OpportunityEngineering.Api.Hubs;

/// <summary>
/// Tracks how many participant (not facilitator) connections currently hold a live session's
/// group membership, purely in-process. This deployment runs a single API container app
/// instance, so an in-memory counter is enough; it would need to move to a shared store (e.g.
/// Azure SignalR's own group membership APIs) if the API ever scales out to multiple replicas.
/// </summary>
public sealed class LivePresenceTracker
{
    private readonly ConcurrentDictionary<string, string> connectionToSession = new();
    private readonly ConcurrentDictionary<string, int> sessionCounts = new();

    public int Join(string joinSessionId, string connectionId)
    {
        connectionToSession[connectionId] = joinSessionId;
        return sessionCounts.AddOrUpdate(joinSessionId, 1, (_, count) => count + 1);
    }

    public (string JoinSessionId, int Count)? Leave(string connectionId)
    {
        if (!connectionToSession.TryRemove(connectionId, out var joinSessionId))
        {
            return null;
        }

        var count = sessionCounts.AddOrUpdate(joinSessionId, 0, (_, count) => Math.Max(0, count - 1));
        return (joinSessionId, count);
    }
}
