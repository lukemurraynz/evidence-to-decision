using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Ports;

public interface IOpportunityGraphStore
{
    Task<OpportunityGraph?> GetAsync(
        string workspaceId,
        string engagementId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OpportunityGraph>> QueryWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OpportunityGraph>> QueryWorkspaceInWindowAsync(
        string workspaceId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken);

    Task CreateAsync(
        OpportunityGraph graph,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken);

    Task ReplaceAsync(
        OpportunityGraph graph,
        long expectedVersion,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        OpportunityGraph graph,
        long expectedVersion,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken);
}

public interface IDurableOperationStore
{
    Task<DurableOperation> CreateAsync(
        DurableOperation operation,
        RecommendationWorkItem workItem,
        CancellationToken cancellationToken);

    Task<DurableOperation?> GetAsync(
        string workspaceId,
        string operationId,
        CancellationToken cancellationToken);

    Task<DurableOperation?> GetByIdempotencyKeyAsync(
        string workspaceId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task UpdateAsync(DurableOperation operation, CancellationToken cancellationToken);
}

public interface IProjectionStore
{
    Task SaveRecommendationAsync(
        string workspaceId,
        OpportunityRecommendation recommendation,
        CancellationToken cancellationToken);

    Task<OpportunityRecommendation?> GetRecommendationAsync(
        string workspaceId,
        string recommendationId,
        CancellationToken cancellationToken);

    Task SaveArtifactAsync(
        string workspaceId,
        ArtifactEnvelope artifact,
        CancellationToken cancellationToken);

    Task<ArtifactEnvelope?> GetArtifactAsync(
        string workspaceId,
        string artifactId,
        CancellationToken cancellationToken);

    Task SaveAnalyticsAsync(
        string workspaceId,
        PortfolioAnalyticsProjection projection,
        CancellationToken cancellationToken);

    Task SaveReviewAsync(
        string workspaceId,
        OpportunityReviewProjection review,
        CancellationToken cancellationToken);

    Task SaveReviewerNotificationAsync(
        string workspaceId,
        ReviewerNotification notification,
        CancellationToken cancellationToken);

    Task<ReviewerNotificationsPage> QueryReviewerNotificationsAsync(
        string workspaceId,
        int pageSize,
        string? continuationToken,
        CancellationToken cancellationToken);

    Task DeleteReviewsByEngagementAsync(
        string workspaceId,
        string engagementId,
        CancellationToken cancellationToken);
}

public interface IIdentifierFactory
{
    string Create();
}

public interface IAppendOnlyAuditSink
{
    Task AppendAsync(PolicyAuditRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyList<PolicyAuditRecord>> QueryAsync(
        string workspaceId,
        string correlationId,
        CancellationToken cancellationToken);
}

public interface IActivityAuditSink
{
    Task AppendAsync(AuditRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditRecord>> QueryAsync(
        string workspaceId,
        string correlationId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Eventually-consistent, ETag-free staging store for live workshop sessions and votes,
/// deliberately not the whole-graph <see cref="IOpportunityGraphStore"/> concurrency model.
/// </summary>
public interface ILiveSessionStore
{
    Task<LiveSession> CreateAsync(LiveSession session, CancellationToken cancellationToken);

    /// <summary>Persists a session with an updated <see cref="LiveSession.Status"/> (e.g. a
    /// facilitator-initiated close). The store is ETag-free, so this is a plain upsert: the
    /// caller already loaded and mutated the session, there is nothing to reconcile.</summary>
    Task<LiveSession> UpdateStatusAsync(LiveSession session, CancellationToken cancellationToken);

    Task<LiveSession?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken);

    Task<LiveSession?> GetAsync(
        string workspaceId,
        string sessionId,
        CancellationToken cancellationToken);

    /// <summary>Finds the currently-active session for a step, if any. Used to route a
    /// shortlist-changed broadcast to the room actually watching that step.</summary>
    Task<LiveSession?> GetActiveByStepAsync(
        string workspaceId,
        string engagementId,
        string journeyStepId,
        CancellationToken cancellationToken);
}

public interface ILiveVoteStore
{
    Task CastAsync(LiveVote vote, CancellationToken cancellationToken);

    Task<IReadOnlyList<LiveVote>> QueryTallyAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken);
}

public interface ILiveIdeationNoteStore
{
    Task<LiveIdeationNote> SubmitAsync(LiveIdeationNote note, CancellationToken cancellationToken);

    Task<IReadOnlyList<LiveIdeationNote>> QueryBySessionAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken);

    Task<LiveIdeationNote?> GetAsync(
        string workspaceId,
        string joinSessionId,
        string noteId,
        CancellationToken cancellationToken);
}

/// <summary>Unlike <see cref="ILiveVoteStore"/>, pinning is toggle-able: casting the same
/// pin twice removes it rather than re-affirming it, so this store needs a delete path the
/// vote store never does.</summary>
public interface ILivePinStore
{
    Task<bool> ToggleAsync(LivePin pin, CancellationToken cancellationToken);

    Task<IReadOnlyList<LivePin>> QueryTallyAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken);
}

/// <summary>Each placement is its own document keyed by its own Id (not by owner+card, unlike
/// every other live-collaboration store). See <see cref="LiveBoardCard"/> for why that's what
/// makes duplication and free movement both safe without real write contention.</summary>
public interface ILiveBoardCardStore
{
    Task<LiveBoardCard> PlaceAsync(LiveBoardCard card, CancellationToken cancellationToken);

    Task<LiveBoardCard> MoveAsync(LiveBoardCard card, CancellationToken cancellationToken);

    Task RemoveAsync(string workspaceId, string placementId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LiveBoardCard>> QueryBySessionAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken);
}
