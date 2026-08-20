using System.Security.Cryptography;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>
/// Mints and redeems live workshop sessions. Deliberately has no dependency on
/// <see cref="GraphCommandService"/>. This service cannot mutate the canonical graph.
/// <see cref="IOpportunityGraphStore"/> is read-only here, used only to look up a step's
/// display text for a joining participant; it does not weaken that boundary.
/// </summary>
public sealed class LiveSessionService(
    ILiveSessionStore sessionStore,
    IOpportunityGraphStore graphStore,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    // Excludes visually ambiguous characters (0/O, 1/I/L) since codes are read aloud or
    // typed from a shared screen during a live workshop.
    private const string JoinCodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int JoinCodeLength = 6;
    private const int MaxJoinCodeAttempts = 5;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(4);

    public async Task<LiveSession> CreateAsync(
        ActorContext actor,
        string engagementId,
        string? journeyStepId,
        bool startPrivate,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);

        var now = timeProvider.GetUtcNow();
        var joinCode = await AllocateJoinCodeAsync(cancellationToken);
        var session = new LiveSession(
            Id: identifiers.Create(),
            WorkspaceId: actor.WorkspaceId,
            EngagementId: engagementId,
            JourneyStepId: journeyStepId,
            JoinCode: joinCode,
            CreatedBy: actor.ActorId,
            CreatedAt: now,
            ExpiresAt: now + SessionLifetime,
            Status: "active",
            BoardRevealed: !startPrivate);

        return await sessionStore.CreateAsync(session, cancellationToken);
    }

    /// <summary>Ends a live session before its natural expiry. A facilitator wrapping up a
    /// round shouldn't have to wait out the rest of a 4-hour lease for participants to stop
    /// being able to vote. Idempotent: closing an already-closed session just re-saves the
    /// same status rather than rejecting the request.</summary>
    public async Task<LiveSession> CloseAsync(
        ActorContext actor,
        string sessionId,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);
        var session = await sessionStore.GetAsync(actor.WorkspaceId, sessionId, cancellationToken)
            ?? throw new DomainException("live_session.not_found", "That live session was not found.");
        return await sessionStore.UpdateStatusAsync(session with { Status = "closed" }, cancellationToken);
    }

    /// <summary>
    /// Validates a join code and mints a new participant identity for it. The caller (the
    /// API layer) is responsible for turning the returned participant identity into a
    /// bearer token; this service has no knowledge of tokens. The resolved <see cref="JourneyStep"/>
    /// is null if the step was removed from the graph after the session was minted; callers
    /// should degrade to generic copy rather than fail the join. <c>ShortlistedDiscoveryCardIds</c>
    /// is the distinct set of cards already shortlisted for this step in past rounds: a cheap,
    /// honest "already discussed" signal built from existing history, not a relevance model.
    /// </summary>
    public async Task<(LiveSession Session, string ParticipantId, JourneyStep? Step, IReadOnlyList<string> ShortlistedDiscoveryCardIds)>
        RedeemJoinCodeAsync(string joinCode, CancellationToken cancellationToken)
    {
        var session = await sessionStore.GetByJoinCodeAsync(joinCode, cancellationToken)
            ?? throw new DomainException("live_session.not_found", "That join code was not recognized.");

        if (!string.Equals(session.Status, "active", StringComparison.Ordinal)
            || session.ExpiresAt <= timeProvider.GetUtcNow())
        {
            throw new DomainException("live_session.expired", "This live session has ended.");
        }

        var graph = await graphStore.GetAsync(session.WorkspaceId, session.EngagementId, cancellationToken);
        var step = graph?.JourneyMaps
            .SelectMany(journeyMap => journeyMap.Steps)
            .FirstOrDefault(candidate => candidate.Id == session.JourneyStepId);
        var shortlistedCardIds = graph?.CardShortlist
            .Where(entry => entry.JourneyStepId == session.JourneyStepId)
            .Select(entry => entry.DiscoveryCardId)
            .Distinct()
            .ToList() ?? [];

        return (session, identifiers.Create(), step, shortlistedCardIds);
    }

    /// <summary>Reveals a private mural to everyone in the room, mirroring Mural's own "think
    /// privately, then reveal" semantics. Idempotent: revealing an already-revealed board just
    /// re-saves the same flag. Re-armable (see <see cref="SetBoardPrivateAsync"/>); a
    /// facilitator can start another private round after this without minting a new
    /// session.</summary>
    public async Task<LiveSession> RevealBoardAsync(
        ActorContext actor,
        string sessionId,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);
        var session = await sessionStore.GetAsync(actor.WorkspaceId, sessionId, cancellationToken)
            ?? throw new DomainException("live_session.not_found", "That live session was not found.");
        return await sessionStore.UpdateStatusAsync(session with { BoardRevealed = true }, cancellationToken);
    }

    /// <summary>Starts a new private round on a session that was previously revealed.
    /// Deliberately does not retroactively hide anything already broadcast. SignalR groups are
    /// opaque (you can send to a group, not enumerate its members), so re-filtering every
    /// already-connected participant's current view would need a new connection registry. Only
    /// placements made from this point forward are hidden from other participants until the
    /// next <see cref="RevealBoardAsync"/> call; existing on-screen cards stay visible, matching
    /// how a facilitator would actually use "start a new private round."</summary>
    public async Task<LiveSession> SetBoardPrivateAsync(
        ActorContext actor,
        string sessionId,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);
        var session = await sessionStore.GetAsync(actor.WorkspaceId, sessionId, cancellationToken)
            ?? throw new DomainException("live_session.not_found", "That live session was not found.");
        return await sessionStore.UpdateStatusAsync(session with { BoardRevealed = false }, cancellationToken);
    }

    /// <summary>Finds the currently-active session for a step, if any. Lets a screen that
    /// didn't itself start the session (the dedicated board route, which is a view onto
    /// whichever session Discovery Cards' "Start a live vote" already minted, not a second,
    /// disconnected room) attach to it.</summary>
    public Task<LiveSession?> GetActiveByStepAsync(
        ActorContext actor,
        string engagementId,
        string journeyStepId,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);
        return sessionStore.GetActiveByStepAsync(actor.WorkspaceId, engagementId, journeyStepId, cancellationToken);
    }

    private async Task<string> AllocateJoinCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxJoinCodeAttempts; attempt++)
        {
            var candidate = GenerateJoinCode();
            var existing = await sessionStore.GetByJoinCodeAsync(candidate, cancellationToken);
            if (existing is null || existing.ExpiresAt <= timeProvider.GetUtcNow())
            {
                return candidate;
            }
        }

        throw new DomainException(
            "live_session.join_code_allocation_failed",
            "Could not allocate a unique join code. Try again.");
    }

    private static string GenerateJoinCode()
    {
        Span<char> code = stackalloc char[JoinCodeLength];
        for (var i = 0; i < JoinCodeLength; i++)
        {
            code[i] = JoinCodeAlphabet[RandomNumberGenerator.GetInt32(JoinCodeAlphabet.Length)];
        }

        return new string(code);
    }

    private static void RequireFacilitatorMutation(ActorContext actor)
    {
        if (!actor.Has(ApplicationRole.Facilitator))
        {
            throw new DomainException(
                "authorization.live_session_denied",
                "Reviewers cannot start a live session.");
        }
    }
}
