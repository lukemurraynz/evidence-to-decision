using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>
/// Places and moves shared-board card placements for a live session. Deliberately has no
/// dependency on <see cref="GraphCommandService"/>. A placement is exploratory, not canonical.
/// Anyone connected to the session (facilitator or participant) can place or move any
/// placement, matching a physical sticky-note board; a facilitator still promotes a placement
/// into the shortlist through the same <see cref="GraphCommandService.AddCardShortlistEntryAsync"/>
/// path a voted or pinned card uses.
/// </summary>
public sealed class LiveBoardService(
    ILiveBoardCardStore boardStore,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    private const int MaximumRationaleLength = 500;

    public async Task<IReadOnlyList<LiveBoardCard>> PlaceAsync(
        ParticipantContext participant,
        string? discoveryCardId,
        double x,
        double y,
        string rationale,
        CancellationToken cancellationToken)
    {
        var trimmedRationale = TrimRationale(rationale);
        // A catalog-card placement's rationale is optional context; a sticky note has nothing
        // else to show, so its text is the whole point and can't be blank.
        if (discoveryCardId is null && trimmedRationale.Length == 0)
        {
            throw new DomainException(
                "live_board_card.note_text_required",
                "A sticky note needs some text before it can be placed.");
        }

        var now = timeProvider.GetUtcNow();
        var card = new LiveBoardCard(
            identifiers.Create(),
            participant.WorkspaceId,
            participant.JoinSessionId,
            participant.ParticipantId,
            participant.DisplayName,
            discoveryCardId,
            Clamp(x),
            Clamp(y),
            trimmedRationale,
            now,
            now);
        await boardStore.PlaceAsync(card, cancellationToken);
        return await GetBoardAsync(participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<LiveBoardCard>> MoveAsync(
        ParticipantContext participant,
        string placementId,
        double x,
        double y,
        CancellationToken cancellationToken)
    {
        // Any participant or facilitator can move any placement. Read the existing record
        // first to preserve its owner/rationale/card id (a data-completeness read, not a
        // concurrency-control one; the store itself is still a blind upsert with no ETag check).
        var existing = await boardStore.QueryBySessionAsync(
            participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
        var current = existing.FirstOrDefault(card => card.Id == placementId)
            ?? throw new DomainException(
                "live_board_card.not_found",
                "That card is no longer on the board.");
        var moved = current with { X = Clamp(x), Y = Clamp(y), LastMovedAt = timeProvider.GetUtcNow() };
        await boardStore.MoveAsync(moved, cancellationToken);
        return await GetBoardAsync(participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
    }

    /// <summary>Any participant or facilitator can remove any placement, same shared-ownership
    /// model as <see cref="MoveAsync"/>. Removing a placement that's already gone is a no-op
    /// rather than an error, since two people clicking remove on the same card at once is a
    /// harmless race, not a conflict worth surfacing.</summary>
    public async Task<IReadOnlyList<LiveBoardCard>> RemoveAsync(
        ParticipantContext participant,
        string placementId,
        CancellationToken cancellationToken)
    {
        await boardStore.RemoveAsync(participant.WorkspaceId, placementId, cancellationToken);
        return await GetBoardAsync(participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<LiveBoardCard>> EditAsync(
        ParticipantContext participant,
        string placementId,
        string rationale,
        CancellationToken cancellationToken)
    {
        var existing = await boardStore.QueryBySessionAsync(
            participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
        var current = existing.FirstOrDefault(card => card.Id == placementId)
            ?? throw new DomainException(
                "live_board_card.not_found",
                "That card is no longer on the board.");
        var trimmedRationale = TrimRationale(rationale);
        if (current.DiscoveryCardId is null && trimmedRationale.Length == 0)
        {
            throw new DomainException(
                "live_board_card.note_text_required",
                "A sticky note needs some text before it can be placed.");
        }

        var edited = current with { Rationale = trimmedRationale, LastMovedAt = timeProvider.GetUtcNow() };
        await boardStore.MoveAsync(edited, cancellationToken);
        return await GetBoardAsync(participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
    }

    /// <summary>Removes every placement on the board: a facilitator-only, whole-session reset
    /// (the caller is responsible for the facilitator gate; this service has no actor/role
    /// concept). Workshop-scale placement counts make a query-then-remove loop fine; there's no
    /// bulk-delete infrastructure to reach for here.</summary>
    public async Task<IReadOnlyList<LiveBoardCard>> ClearAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken)
    {
        var existing = await boardStore.QueryBySessionAsync(workspaceId, joinSessionId, cancellationToken);
        foreach (var card in existing)
        {
            await boardStore.RemoveAsync(workspaceId, card.Id, cancellationToken);
        }

        return await GetBoardAsync(workspaceId, joinSessionId, cancellationToken);
    }

    public Task<IReadOnlyList<LiveBoardCard>> GetBoardAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken) =>
        boardStore.QueryBySessionAsync(workspaceId, joinSessionId, cancellationToken);

    // A stray drag can end outside the canvas element's bounds. Clamp rather than reject so a
    // slightly-off drop still lands at the nearest edge instead of failing the whole move.
    private static double Clamp(double value) => Math.Clamp(value, 0.0, 1.0);

    private static string TrimRationale(string rationale)
    {
        var trimmed = rationale.Trim();
        return trimmed.Length > MaximumRationaleLength ? trimmed[..MaximumRationaleLength] : trimmed;
    }
}
