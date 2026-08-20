using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Api.Contracts;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api.Hubs;

public interface ICollaborationClient
{
    Task VoteTallyUpdated(IReadOnlyList<CardVoteTally> tally);

    /// <summary>Broadcast whenever a participant submits or a facilitator curates an idea
    /// sticky-note in an engagement-wide ideation round. Always the full current note list for
    /// the round, not a delta.</summary>
    Task IdeationBoardUpdated(IReadOnlyList<Domain.LiveIdeationNote> notes);

    /// <summary>Broadcast whenever a participant pins or unpins a catalog card. The aggregate
    /// tally only; each caller's own pinned/unpinned state comes back from their own TogglePin
    /// invocation, not this broadcast (see CollaborationHub.TogglePin).</summary>
    Task PinTallyUpdated(IReadOnlyList<CardPinTally> tally);

    /// <summary>Broadcast from outside the hub (see ApiEndpointHelpers.BroadcastShortlistChangedAsync)
    /// whenever a card is shortlisted for a step with an active live session. Lets a joined
    /// participant's votable card set update live as the facilitator seeds it during the round,
    /// not just from the snapshot they got at join time.</summary>
    Task ShortlistUpdated(IReadOnlyList<string> discoveryCardIds);

    /// <summary>Broadcast when the facilitator explicitly closes a live session. Tells a
    /// joined participant to stop voting immediately rather than only discovering it the next
    /// time they try to cast a vote and get an "expired" error back.</summary>
    Task SessionClosed();

    /// <summary>Broadcast whenever the count of joined participant connections changes. Lets
    /// the facilitator see the room fill up, and gives participants a sense they're not alone.
    /// Never counts the facilitator's own connection (see LivePresenceTracker).</summary>
    Task PresenceUpdated(int participantCount);

    /// <summary>Broadcast whenever any participant or facilitator places or moves a card on the
    /// shared live board. Always the full current board this recipient is allowed to see (see
    /// <c>revealed</c>), matching every other broadcast's full-state-push convention. While a
    /// session's mural is private, a participant's own push contains only their own placements;
    /// facilitators always receive every placement regardless of <c>revealed</c>.</summary>
    Task BoardUpdated(IReadOnlyList<Domain.LiveBoardCard> cards, bool revealed);

    /// <summary>Broadcast whenever a participant or facilitator moves their pointer over the
    /// shared board. Ephemeral, never persisted, and not filtered by a private board's reveal
    /// state (pointer position alone isn't the sensitive content, the placed card is).</summary>
    Task CursorMoved(string participantId, string displayName, double x, double y);
}

/// <summary>
/// Live vote broadcast hub. Accepts both the Entra scheme (a facilitator watching the tally
/// from their own browser tab) and the Participant scheme (someone who joined via a code);
/// only a Participant-scheme connection carries the claims needed to actually cast a vote.
/// </summary>
public sealed class CollaborationHub(
    LiveVoteService votes,
    LiveIdeationService ideation,
    LivePinService pins,
    LiveBoardService board,
    ParticipantContextResolver participants,
    LivePresenceTracker presence,
    FacilitatorConnectionTracker facilitatorConnections,
    ILiveSessionStore sessions)
    : Hub<ICollaborationClient>
{
    /// <summary>A facilitator's own connection passes workspaceId/engagementId (the participant
    /// scheme's token already carries these, so only a facilitator connection needs them here) so
    /// the same participant-shaped hub methods (PlaceBoardCard, MoveBoardCard, RemoveBoardCard,
    /// EditBoardCard) work from the facilitator's own screen, not just a joined
    /// participant's.</summary>
    public async Task JoinSession(string joinSessionId, string? workspaceId = null, string? engagementId = null)
    {
        var resolvedId = ResolveJoinSessionId(joinSessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(resolvedId));

        // Only a genuine participant connection counts toward the room. A facilitator also
        // calls JoinSession (to watch their own leaderboard), and their own presence isn't
        // what "how many people have joined" is meant to answer.
        string? resolvedWorkspaceId;
        if (IsParticipantConnection())
        {
            resolvedWorkspaceId = Context.User?.FindFirstValue("wid");
            var count = presence.Join(resolvedId, Context.ConnectionId);
            await Clients.Group(GroupName(resolvedId)).PresenceUpdated(count);
        }
        else if (workspaceId is not null && engagementId is not null)
        {
            resolvedWorkspaceId = workspaceId;
            var actorId = Context.User?.FindFirstValue("oid")
                ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Context.ConnectionId;
            var displayName = Context.User?.FindFirstValue("name") ?? "Facilitator";
            facilitatorConnections.Set(
                Context.ConnectionId,
                new ParticipantContext(actorId, workspaceId, engagementId, resolvedId, displayName, Context.ConnectionId));
            // Joined alongside the session group so a private mural can still reach every
            // facilitator connection with the full board while participants get a filtered view.
            await Groups.AddToGroupAsync(Context.ConnectionId, FacilitatorGroupName(resolvedId));
        }
        else
        {
            resolvedWorkspaceId = null;
        }

        // Catches this connection up on cards already placed by others before it joined. Without
        // this, only future PlaceBoardCard/MoveBoardCard broadcasts would ever reach it, so a
        // shared mural someone opens mid-session would look empty until the next edit.
        if (resolvedWorkspaceId is not null)
        {
            var participant = ResolveParticipant();
            var boardState = await board.GetBoardAsync(resolvedWorkspaceId, resolvedId, Context.ConnectionAborted);
            var revealed = await IsBoardRevealedAsync(resolvedWorkspaceId, resolvedId);
            var isFacilitatorViewer = !IsParticipantConnection();
            var viewerBoard = revealed || isFacilitatorViewer
                ? boardState
                : FilterForViewer(boardState, participant.ParticipantId);
            await Clients.Caller.BoardUpdated(viewerBoard, revealed);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        facilitatorConnections.Remove(Context.ConnectionId);
        var left = presence.Leave(Context.ConnectionId);
        if (left is { } value)
        {
            await Clients.Group(GroupName(value.JoinSessionId)).PresenceUpdated(value.Count);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Resolves the caller's <see cref="ParticipantContext"/> regardless of connection
    /// scheme: a genuine Participant-scheme token via <see cref="ParticipantContextResolver"/>,
    /// or a facilitator's own Entra connection via the context JoinSession recorded for it.</summary>
    private ParticipantContext ResolveParticipant() =>
        IsParticipantConnection()
            ? participants.Resolve(Context.User ?? new(), Context.ConnectionId)
            : facilitatorConnections.Get(Context.ConnectionId)
                ?? throw new DomainException(
                    "authorization.participant_access_denied",
                    "This participant session is not valid for the requested engagement.");

    public async Task CastVote(string discoveryCardId, string journeyStepId)
    {
        var participant = ResolveParticipant();
        var tally = await votes.CastAsync(
            participant,
            discoveryCardId,
            journeyStepId,
            Context.ConnectionAborted);
        await Clients.Group(GroupName(participant.JoinSessionId)).VoteTallyUpdated(tally);
    }

    public async Task SubmitIdea(string text)
    {
        var participant = ResolveParticipant();
        var sanitized = DisplayNameModeration.Sanitize(text);
        var notes = await ideation.SubmitAsync(participant, sanitized, Context.ConnectionAborted);
        await Clients.Group(GroupName(participant.JoinSessionId)).IdeationBoardUpdated(notes);
    }

    /// <summary>Unlike CastVote, this returns its result directly to the caller. A
    /// participant's own "is this pinned" state must come from their own invoke resolving, the
    /// same reason JoinView's vote confirmation doesn't rely on the broadcast echoing back to
    /// the caster.</summary>
    public async Task<PinToggleResult> TogglePin(string discoveryCardId, string journeyStepId)
    {
        var participant = ResolveParticipant();
        var (pinned, tally) = await pins.ToggleAsync(
            participant,
            discoveryCardId,
            journeyStepId,
            Context.ConnectionAborted);
        await Clients.Group(GroupName(participant.JoinSessionId)).PinTallyUpdated(tally);
        return new PinToggleResult(pinned, tally);
    }

    /// <summary>Creates a new placement even when an identical (participant, card) placement
    /// already exists; that's the duplicate feature. A null discoveryCardId places a freeform
    /// sticky note instead of a catalog card reference. Returns the board directly to the
    /// caller as well as broadcasting it, so the placer can attribute "this is mine" without a
    /// race against the broadcast landing first. See <see cref="BroadcastBoardAsync"/> for why
    /// the returned board and the broadcast board can differ while the mural is private.</summary>
    public async Task<IReadOnlyList<Domain.LiveBoardCard>> PlaceBoardCard(
        string? discoveryCardId, double x, double y, string rationale)
    {
        var participant = ResolveParticipant();
        var boardState = await board.PlaceAsync(
            participant, discoveryCardId, x, y, rationale, Context.ConnectionAborted);
        return await BroadcastBoardAsync(participant, !IsParticipantConnection(), boardState);
    }

    /// <summary>Any participant or facilitator can move any placement, not just their own.
    /// True shared whiteboard, same as a physical sticky-note session.</summary>
    public async Task<IReadOnlyList<Domain.LiveBoardCard>> MoveBoardCard(string placementId, double x, double y)
    {
        var participant = ResolveParticipant();
        var boardState = await board.MoveAsync(participant, placementId, x, y, Context.ConnectionAborted);
        return await BroadcastBoardAsync(participant, !IsParticipantConnection(), boardState);
    }

    /// <summary>Any participant or facilitator can remove any placement, same shared-ownership
    /// model as MoveBoardCard.</summary>
    public async Task<IReadOnlyList<Domain.LiveBoardCard>> RemoveBoardCard(string placementId)
    {
        var participant = ResolveParticipant();
        var boardState = await board.RemoveAsync(participant, placementId, Context.ConnectionAborted);
        return await BroadcastBoardAsync(participant, !IsParticipantConnection(), boardState);
    }

    /// <summary>Any participant or facilitator can edit any placement's rationale text.</summary>
    public async Task<IReadOnlyList<Domain.LiveBoardCard>> EditBoardCard(string placementId, string rationale)
    {
        var participant = ResolveParticipant();
        var boardState = await board.EditAsync(participant, placementId, rationale, Context.ConnectionAborted);
        return await BroadcastBoardAsync(participant, !IsParticipantConnection(), boardState);
    }

    /// <summary>Ephemeral pointer-position broadcast for live cursors. Never persisted, never
    /// sent back to the caller, and not filtered by a private board's reveal state (see
    /// <see cref="ICollaborationClient.CursorMoved"/>).</summary>
    public async Task MoveCursor(double x, double y)
    {
        var participant = ResolveParticipant();
        await Clients.OthersInGroup(GroupName(participant.JoinSessionId))
            .CursorMoved(participant.ParticipantId, participant.DisplayName, x, y);
    }

    /// <summary>Pushes a board update after any mutation and returns the board view the calling
    /// connection itself should see. While the session's mural is revealed (the default and
    /// common case), this is one unfiltered group broadcast, matching every other broadcast's
    /// full-state-push convention. While private, a mutation is invisible to every participant
    /// except the one who made it, so only two payloads go out: the caller's own filtered view,
    /// and the full board to every other facilitator connection. Never a full group
    /// broadcast, and never the unfiltered board back to a participant caller (the RPC return
    /// value is as much a leak path as the broadcast itself).</summary>
    private async Task<IReadOnlyList<Domain.LiveBoardCard>> BroadcastBoardAsync(
        ParticipantContext participant, bool isFacilitatorViewer, IReadOnlyList<Domain.LiveBoardCard> boardState)
    {
        var revealed = await IsBoardRevealedAsync(participant.WorkspaceId, participant.JoinSessionId);
        if (revealed)
        {
            await Clients.Group(GroupName(participant.JoinSessionId)).BoardUpdated(boardState, revealed: true);
            return boardState;
        }

        var callerView = isFacilitatorViewer ? boardState : FilterForViewer(boardState, participant.ParticipantId);
        await Clients.Caller.BoardUpdated(callerView, revealed: false);
        await Clients.GroupExcept(FacilitatorGroupName(participant.JoinSessionId), [Context.ConnectionId])
            .BoardUpdated(boardState, revealed: false);
        return callerView;
    }

    private async Task<bool> IsBoardRevealedAsync(string workspaceId, string joinSessionId)
    {
        var session = await sessions.GetAsync(workspaceId, joinSessionId, Context.ConnectionAborted);
        return session?.BoardRevealed ?? true;
    }

    private static IReadOnlyList<Domain.LiveBoardCard> FilterForViewer(
        IReadOnlyList<Domain.LiveBoardCard> boardState, string participantId) =>
        [.. boardState.Where(card => card.PlacedByParticipantId == participantId)];

    private static string FacilitatorGroupName(string joinSessionId) => $"session:{joinSessionId}:facilitators";

    private string ResolveJoinSessionId(string requestedJoinSessionId)
    {
        // Only trust the token's own "sid" claim for a genuine Participant-scheme token
        // (identified by its "scope" claim). Entra access tokens carry their own unrelated
        // standard "sid" (the AAD session id), which would otherwise hijack a facilitator's
        // own connection into the wrong hub group instead of the live-vote session they asked
        // to watch.
        var user = Context.User;
        return user?.FindFirst("scope")?.Value == "participant"
            ? user.FindFirst("sid")?.Value ?? requestedJoinSessionId
            : requestedJoinSessionId;
    }

    private bool IsParticipantConnection() => Context.User?.FindFirst("scope")?.Value == "participant";

    private static string GroupName(string joinSessionId) => $"session:{joinSessionId}";
}
