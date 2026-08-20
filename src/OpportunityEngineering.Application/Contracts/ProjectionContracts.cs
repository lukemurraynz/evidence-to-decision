using System.Text.Json.Serialization;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Contracts;

public sealed record FitDimension(string Name, string Explanation, string Limitation);

public sealed record RecommendationDraft
{
    public required IReadOnlyList<string> CandidateReferences { get; init; }
    public required IReadOnlyList<FitDimension> FitDimensions { get; init; }
    public required IReadOnlyList<string> EvidenceReferences { get; init; }
    public required IReadOnlyList<string> Unknowns { get; init; }
    public required IReadOnlyList<string> Limitations { get; init; }
    public required string ConfidenceStatus { get; init; }
    public required string RequiredReview { get; init; }
}

public sealed record ValidatedRecommendationDraft(
    IReadOnlyList<string> CandidateReferences,
    IReadOnlyList<FitDimension> FitDimensions,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<string> Unknowns,
    IReadOnlyList<string> Limitations,
    ConfidenceStatus ConfidenceStatus,
    string RequiredReview);

public sealed record OpportunityRecommendation(
    string RecommendationId,
    IReadOnlyList<string> CandidateReferences,
    IReadOnlyList<FitDimension> FitDimensions,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<string> Unknowns,
    IReadOnlyList<string> Limitations,
    ConfidenceStatus ConfidenceStatus,
    string RequiredReview,
    long CanonicalGraphVersion,
    string CorrelationId,
    string GeneratedBy,
    DateTimeOffset GeneratedAt);

/// <summary>A catalog card the frontend supplies as an eligible candidate. The agent may
/// reference only IDs found in the set it's given, never invent one (see
/// FoundryDiscoveryCardSuggestionAgent.ValidateOutput).</summary>
public sealed record DiscoveryCardCandidate(
    string Id,
    string DisplayName,
    string CategoryId,
    string Description);

public sealed record DiscoveryCardSuggestion(
    string DiscoveryCardId,
    string Rationale);

/// <summary>Advisory only, never persisted to the canonical graph. A facilitator reviews these
/// and adds any they agree with through the existing card-shortlist mutation, same as every
/// other agent output in this codebase.</summary>
public sealed record DiscoveryCardSuggestionResult(
    IReadOnlyList<DiscoveryCardSuggestion> Suggestions,
    ConfidenceStatus ConfidenceStatus,
    string RequiredReview,
    long CanonicalGraphVersion,
    string CorrelationId,
    string GeneratedBy,
    DateTimeOffset GeneratedAt);

public sealed record DiscoveryCardSuggestionItemDraft
{
    public required string DiscoveryCardId { get; init; }
    public required string Rationale { get; init; }
}

public sealed record DiscoveryCardSuggestionDraft
{
    public required IReadOnlyList<DiscoveryCardSuggestionItemDraft> Suggestions { get; init; }
    public required string ConfidenceStatus { get; init; }
    public required string RequiredReview { get; init; }
}

public sealed record ArtifactNarrativeDraft
{
    public required string Summary { get; init; }
    public required string RequiredReview { get; init; }
}

/// <summary>Advisory only, never persisted, never auto-applied. A facilitator who agrees with
/// the Suggestion submits it themselves through the existing evidence-correction flow.</summary>
public sealed record EvidenceQualityAssessment(
    string EvidenceId,
    IReadOnlyList<string> Concerns,
    string Suggestion,
    ConfidenceStatus ConfidenceStatus,
    string RequiredReview,
    long CanonicalGraphVersion,
    string CorrelationId,
    string GeneratedBy,
    DateTimeOffset GeneratedAt);

public sealed record EvidenceQualityDraft
{
    public required IReadOnlyList<string> Concerns { get; init; }
    public required string Suggestion { get; init; }
    public required string ConfidenceStatus { get; init; }
    public required string RequiredReview { get; init; }
}

/// <summary>Fields only, never an Id, and Problem never carries a WorkflowId. A facilitator
/// reviews and edits this in the same Frame stage forms used for manual entry, then submits
/// each through the existing AddWorkflowAsync/AddProblemAsync mutations; the agent has no path
/// to the canonical graph itself.</summary>
public sealed record WorkflowDraftContent(
    string Trigger,
    IReadOnlyList<string> Actors,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Decisions,
    IReadOnlyList<string> Systems,
    IReadOnlyList<string> Handoffs,
    IReadOnlyList<string> Exceptions,
    IReadOnlyList<string> Outputs);

public sealed record ProblemDraftContent(
    string User,
    string Goal,
    string Constraint,
    string Impact,
    IReadOnlyList<string> EvidenceReferences,
    decimal Confidence);

/// <summary>One candidate framing among possibly several the agent proposes in a single draft
/// call. See <see cref="FrameDraftResult"/>. No Id: matches the existing advisory-only, no-id
/// convention on <see cref="WorkflowDraftContent"/>/<see cref="ProblemDraftContent"/>; a
/// candidate's position in the list is its only identity, which is fine because nothing about a
/// candidate persists past the facilitator selecting and saving one.</summary>
public sealed record FrameDraftCandidate(
    WorkflowDraftContent Workflow,
    ProblemDraftContent Problem,
    ConfidenceStatus ConfidenceStatus,
    string RequiredReview,
    IReadOnlyList<string> CitationConcerns);

/// <summary>Up to a handful of distinct workflow+problem candidates from one draft call, not
/// forced into a single synthesis, so a room whose evidence points in genuinely different
/// directions doesn't lose everything but the winning framing. See
/// FrameDraftOutputValidator.MaximumCandidates for the cap.</summary>
public sealed record FrameDraftResult(
    IReadOnlyList<FrameDraftCandidate> Candidates,
    long CanonicalGraphVersion,
    string CorrelationId,
    string GeneratedBy,
    DateTimeOffset GeneratedAt);

public sealed record FrameDraftCandidateDraft
{
    public required WorkflowDraftContent Workflow { get; init; }
    public required ProblemDraftContent Problem { get; init; }
    public required string ConfidenceStatus { get; init; }
    public required string RequiredReview { get; init; }
}

/// <summary>Raw structured output from <see cref="Ports.IFrameCritiqueAgent"/>: one short
/// sentence per citation in a drafted candidate's <see cref="ProblemDraftContent.EvidenceReferences"/>
/// that doesn't clearly substantiate the specific claim it's attached to; empty when every
/// citation holds up. A different concern than <see cref="EvidenceQualityDraft"/>, which checks
/// raw evidence wording quality, not whether a drafted claim is actually grounded in what it
/// cites.</summary>
public sealed record FrameCritiqueDraft
{
    public required IReadOnlyList<string> Concerns { get; init; }
}

public sealed record FrameDraft
{
    public required IReadOnlyList<FrameDraftCandidateDraft> Candidates { get; init; }
}

/// <summary>One placement on the live mural the frontend supplies as clustering input. Mirrors
/// <see cref="DiscoveryCardCandidate"/>'s frontend-resolves-the-catalog precedent, since the
/// backend has no server-side discovery card catalog to resolve <c>PlacementId</c>'s card name
/// itself.</summary>
public sealed record BoardClusterCardInput(
    string PlacementId,
    string? CardDisplayName,
    string Rationale,
    double X,
    double Y);

/// <summary>One suggested grouping among the placements. Advisory only, referencing only
/// literal PlacementId values from the supplied input; a facilitator reads this and moves cards
/// themselves, nothing here auto-mutates the board (see FoundryBoardClusterAgent).</summary>
public sealed record BoardClusterSuggestion(
    string Label,
    IReadOnlyList<string> PlacementIds,
    string Rationale);

public sealed record BoardClusterResult(
    IReadOnlyList<BoardClusterSuggestion> Clusters,
    IReadOnlyList<string> OutlierPlacementIds,
    ConfidenceStatus ConfidenceStatus,
    string RequiredReview,
    string CorrelationId,
    string GeneratedBy,
    DateTimeOffset GeneratedAt);

public sealed record BoardClusterSuggestionDraftItem
{
    public required string Label { get; init; }
    public required IReadOnlyList<string> PlacementIds { get; init; }
    public required string Rationale { get; init; }
}

public sealed record BoardClusterDraft
{
    public required IReadOnlyList<BoardClusterSuggestionDraftItem> Clusters { get; init; }
    public required IReadOnlyList<string> OutlierPlacementIds { get; init; }
    public required string ConfidenceStatus { get; init; }
    public required string RequiredReview { get; init; }
}

public sealed record DerivedCard(
    string Id,
    string Type,
    string Title,
    string Description,
    IReadOnlyList<string> Tags,
    string DerivedFromId,
    long DerivedFromVersion,
    long CurrentCanonicalGraphVersion,
    StalenessStatus Staleness);

public sealed record OpportunityReviewProjection(
    string OpportunityId,
    string EngagementId,
    string Value,
    string Confidence,
    TrustProfile Trust,
    ReadinessProfile Readiness,
    string Owner,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<GovernanceBlocker> Blockers,
    DecisionRecord? LatestDecision,
    long CanonicalGraphVersion);

public enum ReviewAttentionReason
{
    NewOpportunity,
    EvidenceConflict,
    ControlsChanged,
    DecisionChanged
}

public sealed record ReviewerNotificationsPage(
    IReadOnlyList<ReviewerNotification> Items,
    string? ContinuationToken);

public sealed record CardVoteTally(
    string DiscoveryCardId,
    string JourneyStepId,
    int Count);

public sealed record CardPinTally(
    string DiscoveryCardId,
    string JourneyStepId,
    int Count);

public sealed record ReviewerNotification(
    string NotificationId,
    string WorkspaceId,
    string EngagementId,
    string OpportunityId,
    ReviewAttentionReason Reason,
    string Summary,
    long CanonicalGraphVersion,
    DateTimeOffset CreatedAt,
    string CorrelationId);

public sealed record ArtifactEnvelope(
    string ArtifactId,
    ArtifactType ArtifactType,
    string EngagementId,
    string OpportunityId,
    long SourceCanonicalGraphVersion,
    string MethodVersion,
    IReadOnlyList<string> ReferencedCardVersions,
    IReadOnlyList<string> ReferencedSourceVersions,
    DateTimeOffset GeneratedAt,
    string GeneratedBy,
    StalenessStatus Staleness,
    IArtifactContent Content,
    // Best-effort agent-authored prose explaining Content for a downstream reader. Never a
    // source of new facts, only ever a synthesis of the deterministic fields alongside it. Null
    // when narrative generation wasn't attempted or failed; the artifact is still complete and
    // authoritative without it, since Content alone is what every field is sourced from.
    ArtifactNarrative? NarrativeSummary = null);

public sealed record ArtifactNarrative(
    string Summary,
    string RequiredReview,
    string GeneratedBy,
    DateTimeOffset GeneratedAt);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "contentType")]
[JsonDerivedType(typeof(ArchitectureHandoffContent), "architectureHandoff")]
[JsonDerivedType(typeof(PilotBriefContent), "pilotBrief")]
[JsonDerivedType(typeof(DecisionRecordContent), "decisionRecord")]
[JsonDerivedType(typeof(ExecutiveSummaryContent), "executiveSummary")]
[JsonDerivedType(typeof(ExperimentDefinitionContent), "experimentDefinition")]
[Newtonsoft.Json.JsonConverter(typeof(ArtifactContentConverter))]
public interface IArtifactContent { }

public sealed record ArchitectureHandoffContent(
    string Problem,
    string Workflow,
    IReadOnlyList<string> Users,
    string DesiredOutcome,
    string Kpi,
    string Baseline,
    string Target,
    IReadOnlyList<Concept> Concepts,
    TrustProfile Trust,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<Assumption> Assumptions,
    string Owner,
    DecisionRecord Decision) : IArtifactContent;

public sealed record PilotBriefContent(
    string DesiredOutcome,
    string Owner,
    string Scope,
    IReadOnlyList<Concept> Concepts,
    TrustProfile Trust,
    IReadOnlyList<Assumption> Assumptions,
    DecisionRecord Decision) : IArtifactContent;

public sealed record DecisionRecordContent(
    string OpportunityOwner,
    string DesiredOutcome,
    DecisionRecord Decision,
    IReadOnlyList<GovernanceBlocker> Blockers) : IArtifactContent;

public sealed record ExecutiveSummaryContent(
    string Problem,
    string DesiredOutcome,
    string ValueProfile,
    string ConfidenceProfile,
    EngagementLifecycle LifecycleState,
    DecisionRecord Decision) : IArtifactContent;

public sealed record ExperimentDefinitionContent(
    string Hypothesis,
    string SuccessCriteria,
    string DesiredOutcome,
    IReadOnlyList<Concept> Concepts,
    IReadOnlyList<Assumption> Assumptions,
    string Owner) : IArtifactContent;

public sealed record PortfolioMetric(
    string Name,
    IReadOnlyDictionary<string, long> Values,
    IReadOnlyList<string> SourceGraphVersions,
    string Definition);

public sealed record PortfolioAnalyticsProjection(
    string Id,
    string ProjectionVersion,
    DateTimeOffset GeneratedAt,
    DateTimeOffset SourceWindowStart,
    DateTimeOffset SourceWindowEnd,
    string Scope,
    IReadOnlyList<PortfolioMetric> Metrics,
    IReadOnlyList<string> SourceGraphVersions);
