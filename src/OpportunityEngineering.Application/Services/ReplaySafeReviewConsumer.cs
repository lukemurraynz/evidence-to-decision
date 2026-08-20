using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>Rereads canonical state and writes only a derived review projection.</summary>
public sealed class ReplaySafeReviewConsumer(
    IEventConsumerClaimStore claimStore,
    IOpportunityGraphStore graphStore,
    IProjectionStore projectionStore,
    GateEvaluator gateEvaluator,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    public async Task ConsumeAsync(
        GraphChangedEvent graphEvent,
        CancellationToken cancellationToken)
    {
        var claimed = await claimStore.TryClaimAsync(
            graphEvent.WorkspaceId,
            graphEvent.EventId,
            "review-projection",
            cancellationToken);
        if (!claimed)
        {
            return;
        }

        try
        {
            // Deletion events have no graph to read; tombstone any stored review projections.
            if (string.Equals(graphEvent.EventType, "EngagementDeleted", StringComparison.Ordinal))
            {
                await projectionStore.DeleteReviewsByEngagementAsync(
                    graphEvent.WorkspaceId,
                    graphEvent.AggregateId,
                    cancellationToken);

                await claimStore.CompleteAsync(
                    new ConsumerResult(
                        "review-projection",
                        graphEvent.EventId,
                        graphEvent.WorkspaceId,
                        graphEvent.CanonicalGraphVersion,
                        "projection_tombstoned",
                        timeProvider.GetUtcNow(),
                        graphEvent.CorrelationId),
                    cancellationToken);
                return;
            }

            var graph = await graphStore.GetAsync(
                graphEvent.WorkspaceId,
                graphEvent.AggregateId,
                cancellationToken)
                ?? throw new DomainException("engagement.not_found", "Engagement was not found.");
            foreach (var opportunity in graph.Opportunities)
            {
                var evaluation = gateEvaluator.Evaluate(
                    opportunity,
                    "review-projection",
                    graph.ObjectVersion,
                    identifiers.Create);
                await projectionStore.SaveReviewAsync(
                    graph.WorkspaceId,
                    ProjectionFactory.CreateReview(graph, opportunity, evaluation.Blockers),
                    cancellationToken);
            }

            // Notify only for the affected opportunity; fall back to a single
            // engagement-level notification when the event pre-dates schema 1.1
            // and carries no AffectedOpportunityId.
            if (AttentionReason(graphEvent.EventType) is { } reason)
            {
                var affectedOpportunity = graphEvent.AffectedOpportunityId is not null
                    ? graph.Opportunities.FirstOrDefault(o => o.Id == graphEvent.AffectedOpportunityId)
                    : null;

                if (affectedOpportunity is not null)
                {
                    var evaluation = gateEvaluator.Evaluate(
                        affectedOpportunity,
                        "review-projection",
                        graph.ObjectVersion,
                        identifiers.Create);
                    await projectionStore.SaveReviewerNotificationAsync(
                        graph.WorkspaceId,
                        new ReviewerNotification(
                            $"{graphEvent.EventId}:{affectedOpportunity.Id}",
                            graph.WorkspaceId,
                            graph.Id,
                            affectedOpportunity.Id,
                            reason,
                            NotificationSummary(reason, evaluation.Status),
                            graph.ObjectVersion,
                            timeProvider.GetUtcNow(),
                            graphEvent.CorrelationId),
                        cancellationToken);
                }
                else
                {
                    // Engagement-level fallback: a single notification without
                    // attributing the change to a specific opportunity.
                    var representativeStatus = graph.Opportunities
                        .Select(o => gateEvaluator.Evaluate(o, "review-projection", graph.ObjectVersion, identifiers.Create).Status)
                        .FirstOrDefault();
                    await projectionStore.SaveReviewerNotificationAsync(
                        graph.WorkspaceId,
                        new ReviewerNotification(
                            $"{graphEvent.EventId}:engagement",
                            graph.WorkspaceId,
                            graph.Id,
                            graph.Id,
                            reason,
                            NotificationSummary(reason, representativeStatus),
                            graph.ObjectVersion,
                            timeProvider.GetUtcNow(),
                            graphEvent.CorrelationId),
                        cancellationToken);
                }
            }

            await claimStore.CompleteAsync(
                new ConsumerResult(
                    "review-projection",
                    graphEvent.EventId,
                    graphEvent.WorkspaceId,
                    graph.ObjectVersion,
                    "projection_refreshed",
                    timeProvider.GetUtcNow(),
                    graphEvent.CorrelationId),
                cancellationToken);
        }
        catch
        {
            await claimStore.ReleaseAsync(
                graphEvent.WorkspaceId,
                graphEvent.EventId,
                "review-projection",
                cancellationToken);
            throw;
        }
    }

    private static ReviewAttentionReason? AttentionReason(string eventType) =>
        eventType switch
        {
            "OpportunityCreated" => ReviewAttentionReason.NewOpportunity,
            "EvidenceConflictDetected" => ReviewAttentionReason.EvidenceConflict,
            "GateEvaluationChanged" => ReviewAttentionReason.ControlsChanged,
            "DecisionChanged" => ReviewAttentionReason.DecisionChanged,
            _ => null
        };

    private static string NotificationSummary(
        ReviewAttentionReason reason,
        GateStatus gateStatus) =>
        reason switch
        {
            ReviewAttentionReason.NewOpportunity =>
                $"A new opportunity is ready for review. Current controls are {GateLabel(gateStatus)}.",
            ReviewAttentionReason.EvidenceConflict =>
                "Conflicting evidence requires human resolution before progression.",
            ReviewAttentionReason.ControlsChanged =>
                $"Readiness controls changed and are now {GateLabel(gateStatus)}.",
            ReviewAttentionReason.DecisionChanged =>
                "A human decision changed the opportunity and its review view was refreshed.",
            _ => throw new ArgumentOutOfRangeException(nameof(reason))
        };

    private static string GateLabel(GateStatus status) =>
        status == GateStatus.Passed ? "satisfied" : "blocked";
}
