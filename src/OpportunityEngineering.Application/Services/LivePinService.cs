using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>
/// Toggles and tallies personal, catalog-wide card pins for a live session. Deliberately has no
/// dependency on <see cref="GraphCommandService"/>. A pin is a personal exploration signal, not
/// canonical. A facilitator still promotes a pinned card into the shortlist through the same
/// <see cref="GraphCommandService.AddCardShortlistEntryAsync"/> path a voted card uses.
/// </summary>
public sealed class LivePinService(
    ILivePinStore pinStore,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    public async Task<(bool Pinned, IReadOnlyList<CardPinTally> Tally)> ToggleAsync(
        ParticipantContext participant,
        string discoveryCardId,
        string journeyStepId,
        CancellationToken cancellationToken)
    {
        var pin = new LivePin(
            identifiers.Create(),
            participant.WorkspaceId,
            participant.JoinSessionId,
            participant.ParticipantId,
            discoveryCardId,
            journeyStepId,
            timeProvider.GetUtcNow());

        var nowPinned = await pinStore.ToggleAsync(pin, cancellationToken);
        var tally = await GetTallyAsync(participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
        return (nowPinned, tally);
    }

    public async Task<IReadOnlyList<CardPinTally>> GetTallyAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken)
    {
        var pins = await pinStore.QueryTallyAsync(workspaceId, joinSessionId, cancellationToken);
        return [.. pins
            .GroupBy(pin => (pin.DiscoveryCardId, pin.JourneyStepId))
            .Select(group => new CardPinTally(group.Key.DiscoveryCardId, group.Key.JourneyStepId, group.Count()))
            .OrderByDescending(tally => tally.Count)];
    }
}
