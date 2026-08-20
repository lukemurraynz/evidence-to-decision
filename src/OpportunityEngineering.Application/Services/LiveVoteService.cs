using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>
/// Casts and tallies live votes. Deliberately has no dependency on
/// <see cref="GraphCommandService"/>. This service cannot reach the canonical graph. Votes
/// only become canonical when a facilitator explicitly promotes one via
/// <see cref="GraphCommandService.AddCardShortlistEntryAsync"/>.
/// </summary>
public sealed class LiveVoteService(
    ILiveVoteStore voteStore,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<CardVoteTally>> CastAsync(
        ParticipantContext participant,
        string discoveryCardId,
        string journeyStepId,
        CancellationToken cancellationToken)
    {
        var vote = new LiveVote(
            identifiers.Create(),
            participant.WorkspaceId,
            participant.JoinSessionId,
            participant.ParticipantId,
            discoveryCardId,
            journeyStepId,
            timeProvider.GetUtcNow());

        await voteStore.CastAsync(vote, cancellationToken);
        return await GetTallyAsync(participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<CardVoteTally>> GetTallyAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken)
    {
        var votes = await voteStore.QueryTallyAsync(workspaceId, joinSessionId, cancellationToken);
        return [.. votes
            .GroupBy(vote => (vote.DiscoveryCardId, vote.JourneyStepId))
            .Select(group => new CardVoteTally(group.Key.DiscoveryCardId, group.Key.JourneyStepId, group.Count()))
            .OrderByDescending(tally => tally.Count)];
    }
}
