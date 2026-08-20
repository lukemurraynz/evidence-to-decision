namespace OpportunityEngineering.Domain;

public enum EngagementLifecycle
{
    Discovery,
    Validation,
    Pilot,
    ProductionReadiness,
    Rejected,
    Parked
}

public enum EvidenceType
{
    Observed,
    Measured,
    CustomerStatement,
    External,
    Interpretation,
    Assumption,
    Hypothesis
}

public enum EvidenceModality
{
    Text,
    Voice,
    Transcript,
    Document,
    Image,
    Mixed
}

public enum ValidationStatus
{
    Unvalidated,
    NeedsCorrection,
    Validated,
    Rejected
}

public enum DecisionClass
{
    Validate,
    Pilot,
    ProductionReady,
    PrerequisitesRequired,
    Reject,
    Park
}

public enum GateStatus
{
    Passed,
    Blocked
}

public enum BlockerCategory
{
    Owner,
    Kpi,
    Baseline,
    Target,
    Privacy,
    Security,
    Governance,
    Data,
    Integration,
    Oversight
}

public enum ConfidenceStatus
{
    Supported,
    Limited,
    Abstain,
    HumanReviewRequired
}

public enum ArtifactType
{
    PilotBrief,
    DecisionRecord,
    ExecutiveSummary,
    ArchitectureHandoff,
    ExperimentDefinition
}

public enum StalenessStatus
{
    Current,
    Stale,
    Unavailable
}

public enum OperationStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Canceled
}

public enum PolicyVerdict
{
    Allow,
    Warn,
    Deny,
    Escalate,
    Transform
}
