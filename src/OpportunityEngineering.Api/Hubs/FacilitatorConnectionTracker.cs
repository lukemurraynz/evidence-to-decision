using System.Collections.Concurrent;
using OpportunityEngineering.Application.Contracts;

namespace OpportunityEngineering.Api.Hubs;

/// <summary>
/// Lets a facilitator's own Entra-authenticated hub connection use the same participant-shaped
/// hub methods (PlaceBoardCard, MoveBoardCard, SendCardChatMessage) as a joined participant.
/// <see cref="Authorization.ParticipantContextResolver"/> only understands Participant-scheme JWT
/// claims, which a facilitator's connection never carries, so JoinSession records a synthetic
/// <see cref="ParticipantContext"/> here for the duration of that connection. In-process only,
/// same single-instance-only constraint as <see cref="LivePresenceTracker"/>.
/// </summary>
public sealed class FacilitatorConnectionTracker
{
    private readonly ConcurrentDictionary<string, ParticipantContext> byConnectionId = new();

    public void Set(string connectionId, ParticipantContext context) => byConnectionId[connectionId] = context;

    public ParticipantContext? Get(string connectionId) =>
        byConnectionId.TryGetValue(connectionId, out var context) ? context : null;

    public void Remove(string connectionId) => byConnectionId.TryRemove(connectionId, out _);
}
