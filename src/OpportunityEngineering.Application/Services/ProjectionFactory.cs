using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public sealed class ProjectionFactory(TimeProvider timeProvider)
{
    public static IReadOnlyList<DerivedCard> CreateCards(
        OpportunityGraph graph,
        string? type,
        string? search)
    {
        var cards = graph.Opportunities.Select(opportunity =>
            new DerivedCard(
                $"opportunity:{opportunity.Id}",
                "opportunity",
                opportunity.DesiredOutcome,
                opportunity.ValueProfile,
                [.. new[] { opportunity.LifecycleState.ToString(), opportunity.ConfidenceProfile }
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))],
                opportunity.Id,
                opportunity.ObjectVersion,
                graph.ObjectVersion,
                StalenessStatus.Current))
            .Concat(graph.Problems.Select(problem =>
                new DerivedCard(
                    $"problem:{problem.Id}",
                    "problem",
                    problem.Goal,
                    problem.Impact,
                    ["problem"],
                    problem.Id,
                    graph.ObjectVersion,
                    graph.ObjectVersion,
                    StalenessStatus.Current)));

        if (!string.IsNullOrWhiteSpace(type))
        {
            cards = cards.Where(card => card.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            cards = cards.Where(card =>
                card.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                card.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return [.. cards.OrderBy(card => card.Type).ThenBy(card => card.Title)];
    }

    public static OpportunityReviewProjection CreateReview(
        OpportunityGraph graph,
        Opportunity opportunity,
        IReadOnlyList<GovernanceBlocker>? evaluatedBlockers = null)
    {
        return new OpportunityReviewProjection(
            opportunity.Id,
            graph.Id,
            opportunity.ValueProfile,
            opportunity.ConfidenceProfile,
            opportunity.TrustProfile,
            opportunity.ReadinessProfile,
            opportunity.Owner,
            opportunity.EvidenceReferences,
            evaluatedBlockers ??
                [.. graph.Blockers.Where(item => item.OpportunityId == opportunity.Id)],
            graph.Decisions.LastOrDefault(item => item.OpportunityId == opportunity.Id),
            graph.ObjectVersion);
    }

    public ArtifactEnvelope CreateArtifact(
        string artifactId,
        ArtifactType artifactType,
        OpportunityGraph graph,
        Opportunity opportunity,
        string generatedBy)
    {
        var problem = graph.Problems.Single(item => item.Id == opportunity.ProblemId);
        var workflow = graph.Workflows.Single(item => item.Id == opportunity.WorkflowId);
        var decision = graph.Decisions.LastOrDefault(item => item.OpportunityId == opportunity.Id)
            ?? throw new DomainException(
                "artifact.decision_required",
                "A handoff artifact requires a recorded human decision.");

        // Resolve actual evidence versions from the canonical graph.
        var evidenceIndex = graph.Evidence.ToDictionary(e => e.Id, e => e.ObjectVersion);
        var evidenceVersions = opportunity.EvidenceReferences
            .Select(id => evidenceIndex.TryGetValue(id, out var v)
                ? $"{id}:v{v}"
                : $"{id}:v1")
            .ToArray();

        IArtifactContent content = artifactType switch
        {
            ArtifactType.ArchitectureHandoff => new ArchitectureHandoffContent(
                problem.Goal,
                workflow.Trigger,
                workflow.Actors,
                opportunity.DesiredOutcome,
                opportunity.KpiReference,
                opportunity.ReadinessProfile.BaselineDefined ? "defined" : "missing",
                opportunity.ReadinessProfile.TargetDefined ? "defined" : "missing",
                opportunity.Concepts,
                opportunity.TrustProfile,
                [.. opportunity.Concepts.SelectMany(item => item.Dependencies).Distinct()],
                opportunity.Assumptions,
                opportunity.Owner,
                decision),

            ArtifactType.PilotBrief => new PilotBriefContent(
                opportunity.DesiredOutcome,
                opportunity.Owner,
                problem.Goal,
                opportunity.Concepts,
                opportunity.TrustProfile,
                opportunity.Assumptions,
                decision),

            ArtifactType.DecisionRecord => new DecisionRecordContent(
                opportunity.Owner,
                opportunity.DesiredOutcome,
                decision,
                [.. graph.Blockers.Where(b => b.OpportunityId == opportunity.Id)]),

            ArtifactType.ExecutiveSummary => new ExecutiveSummaryContent(
                problem.Goal,
                opportunity.DesiredOutcome,
                opportunity.ValueProfile,
                opportunity.ConfidenceProfile,
                opportunity.LifecycleState,
                decision),

            ArtifactType.ExperimentDefinition => new ExperimentDefinitionContent(
                problem.Goal,
                opportunity.KpiReference,
                opportunity.DesiredOutcome,
                opportunity.Concepts,
                opportunity.Assumptions,
                opportunity.Owner),

            _ => throw new DomainException(
                "artifact.unsupported_type",
                $"Artifact type '{artifactType}' is not supported.")
        };

        return new ArtifactEnvelope(
            artifactId,
            artifactType,
            graph.Id,
            opportunity.Id,
            graph.ObjectVersion,
            graph.MethodVersion,
            [],
            evidenceVersions,
            timeProvider.GetUtcNow(),
            generatedBy,
            StalenessStatus.Current,
            content);
    }

    public static ArtifactEnvelope ApplyStaleness(
        ArtifactEnvelope artifact,
        long currentGraphVersion)
    {
        return artifact with
        {
            Staleness = artifact.SourceCanonicalGraphVersion == currentGraphVersion
                ? StalenessStatus.Current
                : StalenessStatus.Stale
        };
    }
}
