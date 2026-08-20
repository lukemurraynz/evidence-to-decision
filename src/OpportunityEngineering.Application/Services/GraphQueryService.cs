using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>Builds workspace-scoped projections without changing canonical state.</summary>
public sealed class GraphQueryService(
    IOpportunityGraphStore graphStore,
    IProjectionStore projectionStore,
    IActivityAuditSink auditSink,
    IIdentifierFactory identifiers,
    ProjectionFactory projections,
    IArtifactNarrativeAgent narrativeAgent,
    IAgentPolicyEvaluator policyEvaluator,
    IAppendOnlyAuditSink policyAuditSink,
    TimeProvider timeProvider)
{
    public Task<OpportunityGraph?> GetGraphAsync(
        ActorContext actor,
        string engagementId,
        CancellationToken cancellationToken) =>
        graphStore.GetAsync(actor.WorkspaceId, engagementId, cancellationToken);

    public Task<IReadOnlyList<OpportunityGraph>> ListEngagementsAsync(
        ActorContext actor,
        CancellationToken cancellationToken) =>
        graphStore.QueryWorkspaceAsync(actor.WorkspaceId, cancellationToken);

    public async Task<IReadOnlyList<DerivedCard>> GetCardsAsync(
        ActorContext actor,
        string engagementId,
        string? type,
        string? search,
        CancellationToken cancellationToken)
    {
        var graph = await GetRequiredAsync(actor, engagementId, cancellationToken);
        return ProjectionFactory.CreateCards(graph, type, search);
    }

    public async Task<OpportunityReviewProjection> GetReviewAsync(
        ActorContext actor,
        string engagementId,
        string opportunityId,
        CancellationToken cancellationToken)
    {
        var graph = await GetRequiredAsync(actor, engagementId, cancellationToken);
        var opportunity = graph.Opportunities.SingleOrDefault(item => item.Id == opportunityId)
            ?? throw new DomainException("opportunity.not_found", "Opportunity was not found.");
        return ProjectionFactory.CreateReview(graph, opportunity);
    }

    public Task<ReviewerNotificationsPage> GetReviewerNotificationsAsync(
        ActorContext actor,
        int pageSize,
        string? continuationToken,
        CancellationToken cancellationToken)
    {
        return !actor.Has(ApplicationRole.Reviewer) && !actor.Has(ApplicationRole.Facilitator)
            ? throw new DomainException(
                "authorization.review_denied",
                "Only reviewers and facilitators can view review notifications.")
            : projectionStore.QueryReviewerNotificationsAsync(
                actor.WorkspaceId,
                pageSize,
                continuationToken,
                cancellationToken);
    }

    public async Task<ArtifactEnvelope> GenerateArtifactAsync(
        ActorContext actor,
        string engagementId,
        string opportunityId,
        ArtifactType artifactType,
        CancellationToken cancellationToken)
    {
        var graph = await GetRequiredAsync(actor, engagementId, cancellationToken);
        var opportunity = graph.Opportunities.SingleOrDefault(item => item.Id == opportunityId)
            ?? throw new DomainException("opportunity.not_found", "Opportunity was not found.");
        var artifact = projections.CreateArtifact(
            identifiers.Create(),
            artifactType,
            graph,
            opportunity,
            actor.ActorId);
        artifact = artifact with
        {
            NarrativeSummary = await TryGenerateNarrativeAsync(
                graph, opportunity, artifactType, artifact.Content, actor, cancellationToken)
        };
        await projectionStore.SaveArtifactAsync(actor.WorkspaceId, artifact, cancellationToken);
        await auditSink.AppendAsync(
            new AuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                "artifact.generate",
                artifact.ArtifactId,
                "recorded",
                $"Generated {artifact.ArtifactType} from canonical graph version {graph.ObjectVersion}.",
                timeProvider.GetUtcNow(),
                actor.CorrelationId,
                graph.ObjectVersion),
            cancellationToken);
        return artifact;
    }

    /// <summary>Best-effort: a facilitator handing off an artifact shouldn't be blocked by a
    /// Foundry outage or a guardrail warning on an enhancement that's optional by design (the
    /// deterministic Content remains complete and authoritative on its own). Any failure here
    /// is swallowed after being recorded in the policy audit trail.</summary>
    private async Task<ArtifactNarrative?> TryGenerateNarrativeAsync(
        OpportunityGraph graph,
        Opportunity opportunity,
        ArtifactType artifactType,
        IArtifactContent content,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        try
        {
            var modelCallPolicy = await policyEvaluator.EvaluateAsync(
                actor,
                "artifact_narrative.model_call",
                "foundry-artifact-narrative",
                cancellationToken);
            await policyAuditSink.AppendAsync(
                new PolicyAuditRecord(
                    identifiers.Create(),
                    actor.WorkspaceId,
                    actor.ActorId,
                    modelCallPolicy.PolicyVersion,
                    "artifact_narrative.model_call",
                    modelCallPolicy.Verdict,
                    modelCallPolicy.Reason,
                    timeProvider.GetUtcNow(),
                    actor.CorrelationId,
                    "foundry-artifact-narrative"),
                cancellationToken);
            // A ternary here would push the awaited call into the false-branch of a
            // conditional expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
            if (!modelCallPolicy.Permitted)
            {
                return null;
            }
#pragma warning restore IDE0046

            return await narrativeAgent.SummarizeAsync(
                graph, opportunity, artifactType, content, actor, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await auditSink.AppendAsync(
                new AuditRecord(
                    identifiers.Create(),
                    actor.WorkspaceId,
                    actor.ActorId,
                    "artifact.narrative_failed",
                    opportunity.Id,
                    "skipped",
                    $"Narrative generation failed and was skipped: {exception.GetType().Name}.",
                    timeProvider.GetUtcNow(),
                    actor.CorrelationId,
                    graph.ObjectVersion),
                cancellationToken);
            return null;
        }
    }

    public async Task<ArtifactEnvelope> GetArtifactAsync(
        ActorContext actor,
        string artifactId,
        CancellationToken cancellationToken)
    {
        var artifact = await projectionStore.GetArtifactAsync(
            actor.WorkspaceId,
            artifactId,
            cancellationToken)
            ?? throw new DomainException("artifact.not_found", "Artifact was not found.");
        var graph = await GetRequiredAsync(actor, artifact.EngagementId, cancellationToken);
        return ProjectionFactory.ApplyStaleness(artifact, graph.ObjectVersion);
    }

    public async Task<PortfolioAnalyticsProjection> GenerateWorkspaceAnalyticsAsync(
        ActorContext actor,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {
        if (windowStart >= windowEnd)
        {
            throw new DomainException(
                "analytics.invalid_window",
                "The analytics source window start must be before its end.");
        }

        var graphs = await graphStore.QueryWorkspaceInWindowAsync(
            actor.WorkspaceId,
            windowStart,
            windowEnd,
            cancellationToken);
        var scoped = graphs.ToArray();
        var versions = scoped.Select(graph => $"{graph.Id}:v{graph.ObjectVersion}").ToArray();
        var stageValues = scoped
            .GroupBy(graph => graph.LifecycleState.ToString())
            .ToDictionary(group => group.Key, group => (long)group.Count());
        var technologyValues = scoped
            .SelectMany(graph => graph.Opportunities)
            .SelectMany(opportunity => opportunity.Concepts)
            .GroupBy(concept => concept.TechnologyPattern)
            .ToDictionary(group => group.Key, group => (long)group.Count());
        var blockerValues = scoped
            .SelectMany(graph => graph.Blockers)
            .GroupBy(blocker => blocker.Category.ToString())
            .ToDictionary(group => group.Key, group => (long)group.Count());

        var analytics = new PortfolioAnalyticsProjection(
            identifiers.Create(),
            "1.0",
            timeProvider.GetUtcNow(),
            windowStart,
            windowEnd,
            actor.WorkspaceId,
            [
                new PortfolioMetric(
                    "engagementStageDemand",
                    stageValues,
                    versions,
                    "Count of engagements by current lifecycle stage within the source window."),
                new PortfolioMetric(
                    "technologyPreference",
                    technologyValues,
                    versions,
                    "Count of canonical concepts by declared technology pattern."),
                new PortfolioMetric(
                    "blockerDistribution",
                    blockerValues,
                    versions,
                    "Count of current durable governance blockers by category.")
            ],
            versions);

        await projectionStore.SaveAnalyticsAsync(actor.WorkspaceId, analytics, cancellationToken);
        await auditSink.AppendAsync(
            new AuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                "analytics.generate",
                analytics.Id,
                "recorded",
                $"Generated derived analytics from {versions.Length} canonical graph version(s).",
                timeProvider.GetUtcNow(),
                actor.CorrelationId,
                versions.Length == 0 ? 0 : scoped.Max(graph => graph.ObjectVersion)),
            cancellationToken);
        return analytics;
    }

    private async Task<OpportunityGraph> GetRequiredAsync(
        ActorContext actor,
        string engagementId,
        CancellationToken cancellationToken)
    {
        return await graphStore.GetAsync(actor.WorkspaceId, engagementId, cancellationToken)
            ?? throw new DomainException("engagement.not_found", "Engagement was not found.");
    }
}
