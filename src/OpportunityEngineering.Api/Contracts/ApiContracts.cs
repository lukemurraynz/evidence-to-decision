using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api.Contracts;

public sealed record CreateEngagementRequest(
    string EngagementId,
    string MethodVersion,
    string Owner,
    string GovernanceOwner,
    IReadOnlyList<string> Objectives,
    IReadOnlyList<string> Participants);

public sealed record CorrectEvidenceRequest(
    string CorrectedStatement,
    string Reason);

public sealed record CaptureEvidenceRequest(
    string Id,
    EvidenceType Type,
    string Statement,
    string SourceReference,
    DateTimeOffset CapturedAt,
    EvidenceModality Modality,
    decimal Confidence,
    ValidationStatus ValidationStatus,
    string? ParticipantReference,
    string? Interpretation,
    string? MultimodalAssetId);

public sealed record GenerateArtifactRequest(
    string OpportunityId,
    ArtifactType ArtifactType);

public sealed record AnalyticsRequest(
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd);

public sealed record RecommendationRequest(
    string OpportunityId);

public sealed record DeleteEngagementRequest(
    string TypedConfirmation);

public sealed record UpdateEngagementDetailsRequest(
    IReadOnlyList<string> Objectives,
    IReadOnlyList<string> Participants);

public sealed record SuggestDiscoveryCardsRequest(
    IReadOnlyList<DiscoveryCardCandidate> Candidates);

public sealed record MarkCardShortlistSelectionRequest(
    bool FacilitatorSelected);

public sealed record JoinLiveSessionRequest(string DisplayName);

public sealed record JoinLiveSessionResponse(
    string Token,
    string WorkspaceId,
    string EngagementId,
    string JoinSessionId,
    string? JourneyStepId,
    string? JourneyStepName,
    string? JourneyStepPainPoint,
    IReadOnlyList<string> ShortlistedDiscoveryCardIds);

/// <summary>A null <see cref="JourneyStepId"/> starts an engagement-wide ideation round
/// instead of a step-scoped voting round; see LiveSession's doc comment.
/// <see cref="StartPrivate"/> defaults false, matching every session minted before mural
/// privacy existed: the mural is public unless a facilitator opts into private mode up
/// front (there is no way to make an already-public board private, only to reveal a private
/// one; see LiveSessionService.RevealBoardAsync).</summary>
public sealed record CreateLiveSessionRequest(string? JourneyStepId, bool StartPrivate = false);

public sealed record PromoteLiveVoteRequest(
    string DiscoveryCardId,
    string JourneyStepId,
    string Rationale,
    int Rank);

public sealed record CurateIdeationNoteRequest(string NoteId);

/// <summary>One mural placement to capture as Evidence. <see cref="CardDisplayName"/> and
/// <see cref="ZoneLabel"/> are resolved client-side: the backend has no server-side discovery
/// card catalog (it's a frontend-only static list), the same reason
/// <c>DiscoveryCardSuggestionService</c> already takes caller-supplied candidate data instead of
/// resolving ids itself.</summary>
public sealed record BoardSnapshotItem(
    string PlacementId,
    string? DiscoveryCardId,
    string? CardDisplayName,
    string Rationale,
    string PlacedByDisplayName,
    string ZoneLabel);

public sealed record SnapshotBoardRequest(IReadOnlyList<BoardSnapshotItem> Items);

public sealed record BoardClusterSuggestionRequest(IReadOnlyList<BoardClusterCardInput> Cards);

public sealed record PinToggleResult(bool Pinned, IReadOnlyList<CardPinTally> Tally);

public sealed record AddOpportunityRequest(
    string Id,
    string ProblemId,
    string WorkflowId,
    string DesiredOutcome,
    string KpiReference,
    string Owner,
    string ValueProfile,
    string ConfidenceProfile,
    TrustProfile TrustProfile,
    ReadinessProfile ReadinessProfile,
    IReadOnlyList<string>? EvidenceReferences,
    IReadOnlyList<Concept>? Concepts,
    IReadOnlyList<Assumption>? Assumptions);
