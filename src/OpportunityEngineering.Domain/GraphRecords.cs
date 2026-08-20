namespace OpportunityEngineering.Domain;

public sealed record Workflow(
    string Id,
    string Trigger,
    IReadOnlyList<string> Actors,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Decisions,
    IReadOnlyList<string> Systems,
    IReadOnlyList<string> Handoffs,
    IReadOnlyList<string> Exceptions,
    IReadOnlyList<string> Outputs);

public sealed record Problem(
    string Id,
    string WorkflowId,
    string User,
    string Goal,
    string Constraint,
    string Impact,
    IReadOnlyList<string> EvidenceReferences,
    decimal Confidence);

public sealed record Persona(
    string Id,
    string Name,
    string Role,
    IReadOnlyList<string> Goals,
    IReadOnlyList<string> PainPoints,
    IReadOnlyList<string> Characteristics);

public sealed record JourneyStep(
    string Id,
    int Order,
    string Name,
    string PainPoint,
    string OpportunityArea,
    string SuccessMetric);

public sealed record JourneyMap(
    string Id,
    string PersonaId,
    string? WorkflowId,
    IReadOnlyList<JourneyStep> Steps);

public sealed record CardShortlistEntry(
    string Id,
    string JourneyStepId,
    string DiscoveryCardId,
    string Rationale,
    int Rank,
    bool FacilitatorSelected);

public sealed record IdeationNote(
    string Id,
    string Text,
    string SubmittedBy,
    DateTimeOffset CuratedAt);

public sealed record BxtScore(
    string Id,
    string CardShortlistEntryId,
    int StrategicFit,
    int BusinessImpact,
    int Desirability,
    int Feasibility,
    string Notes);

public sealed record SolutionRecommendation(
    string Id,
    string CardShortlistEntryId,
    IReadOnlyList<string> ServiceMapping,
    string CostEstimate,
    string TimeEstimate,
    IReadOnlyList<string> References,
    string FollowUpEngagementType,
    string GeneratedBy,
    DateTimeOffset GeneratedAt);

public sealed record TrustProfile(
    bool PrivacyApproved,
    bool SecurityApproved,
    bool GovernanceApproved,
    bool HumanOversightDefined,
    string DataSensitivity,
    string Auditability,
    string ModelRisk,
    string OperationalRisk);

public sealed record ReadinessProfile(
    bool OwnerDefined,
    bool KpiDefined,
    bool BaselineDefined,
    bool TargetDefined,
    bool DataReady,
    bool ProcessStable,
    bool IntegrationReady,
    bool ChangeCapacityReady);

public sealed record Opportunity
{
    public required string Id { get; init; }
    public required string ProblemId { get; init; }
    public required string WorkflowId { get; init; }
    public required string DesiredOutcome { get; init; }
    public required string KpiReference { get; init; }
    public required string Owner { get; init; }
    public required string ValueProfile { get; init; }
    public required string ConfidenceProfile { get; init; }
    public required TrustProfile TrustProfile { get; init; }
    public required ReadinessProfile ReadinessProfile { get; init; }
    public IReadOnlyList<string> EvidenceReferences { get; init; } = [];
    public IReadOnlyList<Concept> Concepts { get; init; } = [];
    public IReadOnlyList<Assumption> Assumptions { get; init; } = [];
    public EngagementLifecycle LifecycleState { get; init; } = EngagementLifecycle.Discovery;
    public long ObjectVersion { get; init; } = 1;
}

public sealed record Concept(
    string Id,
    string InterventionType,
    string Capability,
    string WorkflowChange,
    string TechnologyPattern,
    string AutonomyLevel,
    IReadOnlyList<string> TrustImplications,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> AssumptionReferences,
    string ValidationPlan);

public sealed record Assumption(
    string Id,
    string Claim,
    IReadOnlyList<string> EvidenceBasis,
    string ImpactIfFalse,
    string Status,
    string Owner);

public sealed record DecisionRecord
{
    public required string Id { get; init; }
    public required string OpportunityId { get; init; }
    public required EngagementLifecycle PreviousState { get; init; }
    public required EngagementLifecycle NewState { get; init; }
    public required DecisionClass DecisionClass { get; init; }
    public required string Rationale { get; init; }
    public IReadOnlyList<string> EvidenceReferences { get; init; } = [];
    public IReadOnlyList<string> Dissent { get; init; } = [];
    public required string Owner { get; init; }
    public required string ApprovalPoint { get; init; }
    public required string EscalationPath { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public IReadOnlyList<string> AffectedAssumptions { get; init; } = [];
    public long ObjectVersion { get; init; }
}

public sealed record GovernanceBlocker(
    string Id,
    string OpportunityId,
    BlockerCategory Category,
    string Rationale,
    string Evaluator,
    DateTimeOffset EvaluatedAt,
    string RemediationPath,
    long CanonicalGraphVersion);

public sealed record GateEvaluation(
    string OpportunityId,
    GateStatus Status,
    IReadOnlyList<GovernanceBlocker> Blockers,
    string Evaluator,
    DateTimeOffset EvaluatedAt,
    long EvaluatedGraphVersion);

// FR-001: Experiment, Pilot, and Outcome records linked in the Opportunity Graph.

public sealed record Experiment(
    string Id,
    string OpportunityId,
    string Hypothesis,
    string SuccessCriteria,
    string Owner,
    DateTimeOffset PlannedAt,
    IReadOnlyList<string> EvidenceReferences,
    string Status);

public sealed record PilotRecord(
    string Id,
    string OpportunityId,
    string ExperimentId,
    string Scope,
    string Owner,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string Status,
    IReadOnlyList<string> EvidenceReferences);

public sealed record OutcomeRecord(
    string Id,
    string OpportunityId,
    string PilotId,
    string Summary,
    string Owner,
    DateTimeOffset RecordedAt,
    IReadOnlyList<string> EvidenceReferences,
    bool MetSuccessCriteria);
