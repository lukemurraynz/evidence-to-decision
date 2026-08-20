export const EvidenceType = {
  Observed: 0,
  Measured: 1,
  CustomerStatement: 2,
  External: 3,
  Interpretation: 4,
  Assumption: 5,
  Hypothesis: 6,
} as const

export type EvidenceTypeValue =
  (typeof EvidenceType)[keyof typeof EvidenceType]

export const EvidenceModality = {
  Text: 0,
  Voice: 1,
  Transcript: 2,
  Document: 3,
  Image: 4,
  Mixed: 5,
} as const

export type EvidenceModalityValue =
  (typeof EvidenceModality)[keyof typeof EvidenceModality]

export const ValidationStatus = {
  Unvalidated: 0,
  NeedsCorrection: 1,
  Validated: 2,
  Rejected: 3,
} as const

export type ValidationStatusValue =
  (typeof ValidationStatus)[keyof typeof ValidationStatus]

export const EngagementLifecycle = {
  Discovery: 0,
  Validation: 1,
  Pilot: 2,
  ProductionReadiness: 3,
  Rejected: 4,
  Parked: 5,
} as const

export type EngagementLifecycleValue =
  (typeof EngagementLifecycle)[keyof typeof EngagementLifecycle]

export const DecisionClass = {
  Validate: 0,
  Pilot: 1,
  ProductionReady: 2,
  PrerequisitesRequired: 3,
  Reject: 4,
  Park: 5,
} as const

export type DecisionClassValue =
  (typeof DecisionClass)[keyof typeof DecisionClass]

export const ArtifactType = {
  PilotBrief: 0,
  DecisionRecord: 1,
  ExecutiveSummary: 2,
  ArchitectureHandoff: 3,
  ExperimentDefinition: 4,
} as const

export type ArtifactTypeValue =
  (typeof ArtifactType)[keyof typeof ArtifactType]

export const OperationStatus = {
  Queued: 0,
  Running: 1,
  Succeeded: 2,
  Failed: 3,
  Canceled: 4,
} as const

export type OperationStatusValue =
  (typeof OperationStatus)[keyof typeof OperationStatus]

export const StalenessStatus = {
  Current: 0,
  Stale: 1,
  Unavailable: 2,
} as const

export type StalenessStatusValue =
  (typeof StalenessStatus)[keyof typeof StalenessStatus]

export type Evidence = {
  readonly id: string
  readonly type: EvidenceTypeValue
  readonly statement: string
  readonly interpretation?: string | null
  readonly sourceReference: string
  readonly participantReference?: string | null
  readonly capturedAt: string
  readonly modality: EvidenceModalityValue
  readonly confidence: number
  readonly validationStatus: ValidationStatusValue
  readonly objectVersion: number
}

// Matches the backend's ConfidenceStatus enum ordinal: 0 Supported, 1 Limited, 2 Abstain,
// 3 HumanReviewRequired.
export type EvidenceQualityAssessment = {
  readonly evidenceId: string
  readonly concerns: readonly string[]
  readonly suggestion: string
  readonly confidenceStatus: number
  readonly requiredReview: string
  readonly canonicalGraphVersion: number
  readonly correlationId: string
  readonly generatedBy: string
  readonly generatedAt: string
}

export type TrustProfile = {
  readonly privacyApproved: boolean
  readonly securityApproved: boolean
  readonly governanceApproved: boolean
  readonly humanOversightDefined: boolean
  readonly dataSensitivity: string
  readonly auditability: string
  readonly modelRisk: string
  readonly operationalRisk: string
}

export type ReadinessProfile = {
  readonly ownerDefined: boolean
  readonly kpiDefined: boolean
  readonly baselineDefined: boolean
  readonly targetDefined: boolean
  readonly dataReady: boolean
  readonly processStable: boolean
  readonly integrationReady: boolean
  readonly changeCapacityReady: boolean
}

export type Workflow = {
  readonly id: string
  readonly trigger: string
  readonly actors: readonly string[]
  readonly inputs: readonly string[]
  readonly steps: readonly string[]
  readonly decisions: readonly string[]
  readonly systems: readonly string[]
  readonly handoffs: readonly string[]
  readonly exceptions: readonly string[]
  readonly outputs: readonly string[]
}

export type Problem = {
  readonly id: string
  readonly workflowId: string
  readonly user: string
  readonly goal: string
  readonly constraint: string
  readonly impact: string
  readonly evidenceReferences: readonly string[]
  readonly confidence: number
}

export type WorkflowDraftContent = Omit<Workflow, 'id'>
export type ProblemDraftContent = Omit<Problem, 'id' | 'workflowId'>

// Matches the backend's ConfidenceStatus enum ordinal: 0 Supported, 1 Limited, 2 Abstain,
// 3 HumanReviewRequired.
export type FrameDraftCandidate = {
  readonly workflow: WorkflowDraftContent
  readonly problem: ProblemDraftContent
  readonly confidenceStatus: number
  readonly requiredReview: string
  readonly citationConcerns: readonly string[]
}

export type FrameDraftResult = {
  readonly candidates: readonly FrameDraftCandidate[]
  readonly canonicalGraphVersion: number
  readonly correlationId: string
  readonly generatedBy: string
  readonly generatedAt: string
}

// Request-only: resolved client-side (card display name, zone label) and sent to the snapshot
// endpoint, which has no server-side discovery-card catalog to resolve these itself.
export type BoardSnapshotItem = {
  readonly placementId: string
  readonly discoveryCardId: string | null
  readonly cardDisplayName: string | null
  readonly rationale: string
  readonly placedByDisplayName: string
  readonly zoneLabel: string
}

export type BoardClusterCardInput = {
  readonly placementId: string
  readonly cardDisplayName: string | null
  readonly rationale: string
  readonly x: number
  readonly y: number
}

export type BoardClusterSuggestion = {
  readonly label: string
  readonly placementIds: readonly string[]
  readonly rationale: string
}

export type BoardClusterResult = {
  readonly clusters: readonly BoardClusterSuggestion[]
  readonly outlierPlacementIds: readonly string[]
  readonly confidenceStatus: number
  readonly requiredReview: string
  readonly correlationId: string
  readonly generatedBy: string
  readonly generatedAt: string
}

export type Persona = {
  readonly id: string
  readonly name: string
  readonly role: string
  readonly goals: readonly string[]
  readonly painPoints: readonly string[]
  readonly characteristics: readonly string[]
}

export type JourneyStep = {
  readonly id: string
  readonly order: number
  readonly name: string
  readonly painPoint: string
  readonly opportunityArea: string
  readonly successMetric: string
}

export type JourneyMap = {
  readonly id: string
  readonly personaId: string
  readonly workflowId: string | null
  readonly steps: readonly JourneyStep[]
}

export type CardShortlistEntry = {
  readonly id: string
  readonly journeyStepId: string
  readonly discoveryCardId: string
  readonly rationale: string
  readonly rank: number
  readonly facilitatorSelected: boolean
}

// A null journeyStepId means this session is engagement-wide (an ideation round) rather
// than scoped to one journey step (a vote round); see the backend LiveSession doc comment.
export type LiveSession = {
  readonly id: string
  readonly workspaceId: string
  readonly engagementId: string
  readonly journeyStepId: string | null
  readonly joinCode: string
  readonly createdBy: string
  readonly createdAt: string
  readonly expiresAt: string
  readonly status: string
}

export type CardVoteTally = {
  readonly discoveryCardId: string
  readonly journeyStepId: string
  readonly count: number
}

export type CardPinTally = {
  readonly discoveryCardId: string
  readonly journeyStepId: string
  readonly count: number
}

export type PinToggleResult = {
  readonly pinned: boolean
  readonly tally: readonly CardPinTally[]
}

export type LiveIdeationNote = {
  readonly id: string
  readonly workspaceId: string
  readonly joinSessionId: string
  readonly participantId: string
  readonly displayName: string
  readonly text: string
  readonly submittedAt: string
}

export type IdeationNote = {
  readonly id: string
  readonly text: string
  readonly submittedBy: string
  readonly curatedAt: string
}

export type LiveBoardCard = {
  readonly id: string
  readonly workspaceId: string
  readonly joinSessionId: string
  readonly placedByParticipantId: string
  readonly placedByDisplayName: string
  // null means this placement is a freeform sticky note rather than a catalog card reference.
  readonly discoveryCardId: string | null
  // Normalized 0..1 position on the open mural canvas; there is no separate lane/category field.
  readonly x: number
  readonly y: number
  readonly rationale: string
  readonly placedAt: string
  readonly lastMovedAt: string
}

export type JoinLiveSessionResponse = {
  readonly token: string
  readonly workspaceId: string
  readonly engagementId: string
  readonly joinSessionId: string
  readonly journeyStepId: string | null
  readonly journeyStepName: string | null
  readonly journeyStepPainPoint: string | null
  readonly shortlistedDiscoveryCardIds: readonly string[]
}

export type DerivedCard = {
  readonly id: string
  readonly type: string
  readonly title: string
  readonly description: string
  readonly tags: readonly string[]
  readonly derivedFromId: string
  readonly derivedFromVersion: number
  readonly currentCanonicalGraphVersion: number
  readonly staleness: StalenessStatusValue
}

export type Concept = {
  readonly id: string
  readonly interventionType: string
  readonly capability: string
  readonly workflowChange: string
  readonly technologyPattern: string
  readonly autonomyLevel: string
  readonly trustImplications: readonly string[]
  readonly dependencies: readonly string[]
  readonly assumptionReferences: readonly string[]
  readonly validationPlan: string
}

export type Opportunity = {
  readonly id: string
  readonly problemId: string
  readonly workflowId: string
  readonly desiredOutcome: string
  readonly kpiReference: string
  readonly owner: string
  readonly valueProfile: string
  readonly confidenceProfile: string
  readonly trustProfile: TrustProfile
  readonly readinessProfile: ReadinessProfile
  readonly evidenceReferences: readonly string[]
  readonly concepts: readonly Concept[]
  readonly lifecycleState: EngagementLifecycleValue
  readonly objectVersion: number
}

export type DecisionRecord = {
  readonly id: string
  readonly opportunityId: string
  readonly previousState: EngagementLifecycleValue
  readonly newState: EngagementLifecycleValue
  readonly decisionClass: DecisionClassValue
  readonly rationale: string
  readonly evidenceReferences: readonly string[]
  readonly dissent: readonly string[]
  readonly owner: string
  readonly approvalPoint: string
  readonly escalationPath: string
  readonly timestamp: string
  readonly affectedAssumptions: readonly string[]
  readonly objectVersion: number
}

export type GovernanceBlocker = {
  readonly id: string
  readonly opportunityId: string
  readonly category: number
  readonly rationale: string
  readonly remediationPath: string
}

export type Engagement = {
  readonly id: string
  readonly workspaceId: string
  readonly methodVersion: string
  readonly owner: string
  readonly governanceOwner: string
  readonly objectVersion: number
  readonly lifecycleState: EngagementLifecycleValue
  readonly objectives: readonly string[]
  readonly participants: readonly string[]
  readonly workflows: readonly Workflow[]
  readonly problems: readonly Problem[]
  readonly personas: readonly Persona[]
  readonly journeyMaps: readonly JourneyMap[]
  readonly cardShortlist: readonly CardShortlistEntry[]
  readonly ideationNotes: readonly IdeationNote[]
  readonly evidence: readonly Evidence[]
  readonly opportunities: readonly Opportunity[]
  readonly decisions: readonly DecisionRecord[]
  readonly blockers: readonly GovernanceBlocker[]
}

export type CreateEngagementInput = {
  readonly engagementId: string
  readonly methodVersion: string
  readonly owner: string
  readonly governanceOwner: string
  readonly objectives: readonly string[]
  readonly participants: readonly string[]
}

export type UpdateEngagementDetailsInput = {
  readonly objectives: readonly string[]
  readonly participants: readonly string[]
}

export type OpportunityReview = {
  readonly opportunityId: string
  readonly engagementId: string
  readonly value: string
  readonly confidence: string
  readonly trust: TrustProfile
  readonly readiness: ReadinessProfile
  readonly owner: string
  readonly evidenceReferences: readonly string[]
  readonly blockers: readonly GovernanceBlocker[]
  readonly latestDecision?: DecisionRecord | null
  readonly canonicalGraphVersion: number
}

export type DurableOperation = {
  readonly id: string
  readonly workspaceId: string
  readonly operationType: string
  readonly status: OperationStatusValue
  readonly createdAt: string
  readonly updatedAt: string
  readonly correlationId: string
  readonly resultReference?: string | null
  readonly errorCode?: string | null
  readonly errorDetail?: string | null
  readonly retryAfterSeconds: number
}

export type ArtifactNarrative = {
  readonly summary: string
  readonly requiredReview: string
  readonly generatedBy: string
  readonly generatedAt: string
}

export type ArtifactEnvelope = {
  readonly artifactId: string
  readonly artifactType: ArtifactTypeValue
  readonly engagementId: string
  readonly opportunityId: string
  readonly sourceCanonicalGraphVersion: number
  readonly methodVersion: string
  readonly generatedAt: string
  readonly generatedBy: string
  readonly staleness: number
  readonly content: Readonly<Record<string, unknown>>
  // Best-effort agent-authored prose alongside content. Null when generation wasn't
  // attempted or failed; content alone remains the artifact's complete, authoritative source.
  readonly narrativeSummary: ArtifactNarrative | null
}

export type CaptureEvidenceInput = {
  readonly id: string
  readonly type: EvidenceTypeValue
  readonly statement: string
  readonly sourceReference: string
  readonly capturedAt: string
  readonly modality: EvidenceModalityValue
  readonly confidence: number
  readonly validationStatus: ValidationStatusValue
  readonly participantReference?: string
  readonly interpretation?: string
}

export type CreateWorkflowInput = Workflow

export type CreateProblemInput = Problem

export type CreatePersonaInput = Persona

export type CreateJourneyMapInput = JourneyMap

export type CreateCardShortlistEntryInput = CardShortlistEntry

export type DiscoveryCardCandidateInput = {
  readonly id: string
  readonly displayName: string
  readonly categoryId: string
  readonly description: string
}

export type DiscoveryCardSuggestion = {
  readonly discoveryCardId: string
  readonly rationale: string
}

// Matches the backend's ConfidenceStatus enum ordinal: 0 Supported, 1 Limited, 2 Abstain,
// 3 HumanReviewRequired.
export type DiscoveryCardSuggestionResult = {
  readonly suggestions: readonly DiscoveryCardSuggestion[]
  readonly confidenceStatus: number
  readonly requiredReview: string
  readonly canonicalGraphVersion: number
  readonly correlationId: string
  readonly generatedBy: string
  readonly generatedAt: string
}

export type CreateOpportunityInput = {
  readonly id: string
  readonly problemId: string
  readonly workflowId: string
  readonly desiredOutcome: string
  readonly kpiReference: string
  readonly owner: string
  readonly valueProfile: string
  readonly confidenceProfile: string
  readonly trustProfile: TrustProfile
  readonly readinessProfile: ReadinessProfile
  readonly evidenceReferences?: readonly string[]
  readonly concepts?: readonly Concept[]
}

export type RecordDecisionInput = {
  readonly id: string
  readonly opportunityId: string
  readonly previousState: EngagementLifecycleValue
  readonly newState: EngagementLifecycleValue
  readonly decisionClass: DecisionClassValue
  readonly rationale: string
  readonly evidenceReferences: readonly string[]
  readonly dissent: readonly string[]
  readonly owner: string
  readonly approvalPoint: string
  readonly escalationPath: string
  readonly timestamp: string
  readonly affectedAssumptions: readonly string[]
  readonly objectVersion: number
}

type BoundaryRecord = Readonly<Record<string, unknown>> & {
  readonly actors?: unknown
  readonly affectedAssumptions?: unknown
  readonly apiBaseUrl?: unknown
  readonly approvalPoint?: unknown
  readonly artifactId?: unknown
  readonly artifactType?: unknown
  readonly assumptionReferences?: unknown
  readonly auditability?: unknown
  readonly authClientId?: unknown
  readonly authScope?: unknown
  readonly authTenantId?: unknown
  readonly autonomyLevel?: unknown
  readonly baselineDefined?: unknown
  readonly blockers?: unknown
  readonly canonicalGraphVersion?: unknown
  readonly candidates?: unknown
  readonly capability?: unknown
  readonly capturedAt?: unknown
  readonly cardShortlist?: unknown
  readonly category?: unknown
  readonly changeCapacityReady?: unknown
  readonly characteristics?: unknown
  readonly citationConcerns?: unknown
  readonly clusters?: unknown
  readonly concepts?: unknown
  readonly concerns?: unknown
  readonly confidence?: unknown
  readonly confidenceProfile?: unknown
  readonly confidenceStatus?: unknown
  readonly constraint?: unknown
  readonly content?: unknown
  readonly correlationId?: unknown
  readonly count?: unknown
  readonly createdAt?: unknown
  readonly createdBy?: unknown
  readonly curatedAt?: unknown
  readonly currentCanonicalGraphVersion?: unknown
  readonly dataReady?: unknown
  readonly dataSensitivity?: unknown
  readonly decisionClass?: unknown
  readonly decisions?: unknown
  readonly dependencies?: unknown
  readonly derivedFromId?: unknown
  readonly derivedFromVersion?: unknown
  readonly description?: unknown
  readonly desiredOutcome?: unknown
  readonly detail?: unknown
  readonly discoveryCardId?: unknown
  readonly displayName?: unknown
  readonly dissent?: unknown
  readonly engagementId?: unknown
  readonly errorCode?: unknown
  readonly errorDetail?: unknown
  readonly escalationPath?: unknown
  readonly evidence?: unknown
  readonly evidenceId?: unknown
  readonly evidenceReferences?: unknown
  readonly exceptions?: unknown
  readonly expiresAt?: unknown
  readonly facilitatorSelected?: unknown
  readonly generatedAt?: unknown
  readonly generatedBy?: unknown
  readonly goal?: unknown
  readonly goals?: unknown
  readonly governanceApproved?: unknown
  readonly governanceOwner?: unknown
  readonly handoffs?: unknown
  readonly humanOversightDefined?: unknown
  readonly id?: unknown
  readonly ideationNotes?: unknown
  readonly impact?: unknown
  readonly inputs?: unknown
  readonly instance?: unknown
  readonly integrationReady?: unknown
  readonly interpretation?: unknown
  readonly interventionType?: unknown
  readonly joinCode?: unknown
  readonly joinSessionId?: unknown
  readonly journeyMaps?: unknown
  readonly journeyStepId?: unknown
  readonly journeyStepName?: unknown
  readonly journeyStepPainPoint?: unknown
  readonly kpiDefined?: unknown
  readonly kpiReference?: unknown
  readonly label?: unknown
  readonly lastMovedAt?: unknown
  readonly latestDecision?: unknown
  readonly lifecycleState?: unknown
  readonly methodVersion?: unknown
  readonly modality?: unknown
  readonly modelRisk?: unknown
  readonly name?: unknown
  readonly narrativeSummary?: unknown
  readonly newState?: unknown
  readonly objectVersion?: unknown
  readonly objectives?: unknown
  readonly operationType?: unknown
  readonly operationalRisk?: unknown
  readonly opportunities?: unknown
  readonly opportunityArea?: unknown
  readonly opportunityId?: unknown
  readonly order?: unknown
  readonly outlierPlacementIds?: unknown
  readonly outputs?: unknown
  readonly owner?: unknown
  readonly ownerDefined?: unknown
  readonly painPoint?: unknown
  readonly painPoints?: unknown
  readonly participantId?: unknown
  readonly participantReference?: unknown
  readonly participants?: unknown
  readonly personaId?: unknown
  readonly personas?: unknown
  readonly pinned?: unknown
  readonly placedAt?: unknown
  readonly placedByDisplayName?: unknown
  readonly placedByParticipantId?: unknown
  readonly placementIds?: unknown
  readonly pollMaxAttempts?: unknown
  readonly pollMaxElapsedMs?: unknown
  readonly previousState?: unknown
  readonly privacyApproved?: unknown
  readonly problem?: unknown
  readonly problemId?: unknown
  readonly problems?: unknown
  readonly processStable?: unknown
  readonly rank?: unknown
  readonly rationale?: unknown
  readonly readiness?: unknown
  readonly readinessProfile?: unknown
  readonly remediationPath?: unknown
  readonly reply?: unknown
  readonly requestTimeoutMs?: unknown
  readonly requiredReview?: unknown
  readonly respondedAt?: unknown
  readonly resultReference?: unknown
  readonly retryAfterSeconds?: unknown
  readonly role?: unknown
  readonly securityApproved?: unknown
  readonly shortlistedDiscoveryCardIds?: unknown
  readonly sourceCanonicalGraphVersion?: unknown
  readonly sourceReference?: unknown
  readonly staleness?: unknown
  readonly statement?: unknown
  readonly status?: unknown
  readonly steps?: unknown
  readonly submittedAt?: unknown
  readonly submittedBy?: unknown
  readonly successMetric?: unknown
  readonly suggestion?: unknown
  readonly suggestions?: unknown
  readonly summary?: unknown
  readonly systems?: unknown
  readonly tags?: unknown
  readonly tally?: unknown
  readonly targetDefined?: unknown
  readonly technologyPattern?: unknown
  readonly text?: unknown
  readonly timestamp?: unknown
  readonly title?: unknown
  readonly token?: unknown
  readonly traceId?: unknown
  readonly trigger?: unknown
  readonly trust?: unknown
  readonly trustImplications?: unknown
  readonly trustProfile?: unknown
  readonly type?: unknown
  readonly updatedAt?: unknown
  readonly user?: unknown
  readonly validationPlan?: unknown
  readonly validationStatus?: unknown
  readonly value?: unknown
  readonly valueProfile?: unknown
  readonly workflow?: unknown
  readonly workflowChange?: unknown
  readonly workflowId?: unknown
  readonly workflows?: unknown
  readonly workspaceId?: unknown
  readonly x?: unknown
  readonly y?: unknown
}

export function isRecord(value: unknown): value is BoundaryRecord {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function isStringArray(value: unknown): value is readonly string[] {
  return Array.isArray(value) && value.every((item) => typeof item === 'string')
}

function isEnumValue(value: unknown, maximum: number): value is number {
  return Number.isInteger(value) && Number(value) >= 0 && Number(value) <= maximum
}

export function isEvidenceTypeValue(
  value: unknown,
): value is EvidenceTypeValue {
  return isEnumValue(value, 6)
}

export function isArtifactTypeValue(
  value: unknown,
): value is ArtifactTypeValue {
  return isEnumValue(value, 4)
}

function isTrustProfile(value: unknown): value is TrustProfile {
  return (
    isRecord(value) &&
    typeof value.privacyApproved === 'boolean' &&
    typeof value.securityApproved === 'boolean' &&
    typeof value.governanceApproved === 'boolean' &&
    typeof value.humanOversightDefined === 'boolean' &&
    typeof value.dataSensitivity === 'string' &&
    typeof value.auditability === 'string' &&
    typeof value.modelRisk === 'string' &&
    typeof value.operationalRisk === 'string'
  )
}

function isReadinessProfile(value: unknown): value is ReadinessProfile {
  return (
    isRecord(value) &&
    typeof value.ownerDefined === 'boolean' &&
    typeof value.kpiDefined === 'boolean' &&
    typeof value.baselineDefined === 'boolean' &&
    typeof value.targetDefined === 'boolean' &&
    typeof value.dataReady === 'boolean' &&
    typeof value.processStable === 'boolean' &&
    typeof value.integrationReady === 'boolean' &&
    typeof value.changeCapacityReady === 'boolean'
  )
}

function isEvidence(value: unknown): value is Evidence {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    isEnumValue(value.type, 6) &&
    typeof value.statement === 'string' &&
    (value.interpretation === undefined ||
      value.interpretation === null ||
      typeof value.interpretation === 'string') &&
    typeof value.sourceReference === 'string' &&
    (value.participantReference === undefined ||
      value.participantReference === null ||
      typeof value.participantReference === 'string') &&
    typeof value.capturedAt === 'string' &&
    isEnumValue(value.modality, 5) &&
    typeof value.confidence === 'number' &&
    isEnumValue(value.validationStatus, 3) &&
    Number.isInteger(value.objectVersion)
  )
}

function isWorkflow(value: unknown): value is Workflow {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.trigger === 'string' &&
    isStringArray(value.actors) &&
    isStringArray(value.inputs) &&
    isStringArray(value.steps) &&
    isStringArray(value.decisions) &&
    isStringArray(value.systems) &&
    isStringArray(value.handoffs) &&
    isStringArray(value.exceptions) &&
    isStringArray(value.outputs)
  )
}

function isProblem(value: unknown): value is Problem {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.workflowId === 'string' &&
    typeof value.user === 'string' &&
    typeof value.goal === 'string' &&
    typeof value.constraint === 'string' &&
    typeof value.impact === 'string' &&
    isStringArray(value.evidenceReferences) &&
    typeof value.confidence === 'number'
  )
}

function isPersona(value: unknown): value is Persona {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.name === 'string' &&
    typeof value.role === 'string' &&
    isStringArray(value.goals) &&
    isStringArray(value.painPoints) &&
    isStringArray(value.characteristics)
  )
}

function isJourneyStep(value: unknown): value is JourneyStep {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    Number.isInteger(value.order) &&
    typeof value.name === 'string' &&
    typeof value.painPoint === 'string' &&
    typeof value.opportunityArea === 'string' &&
    typeof value.successMetric === 'string'
  )
}

function isJourneyMap(value: unknown): value is JourneyMap {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.personaId === 'string' &&
    (value.workflowId === null || typeof value.workflowId === 'string') &&
    Array.isArray(value.steps) &&
    value.steps.every(isJourneyStep)
  )
}

function isCardShortlistEntry(value: unknown): value is CardShortlistEntry {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.journeyStepId === 'string' &&
    typeof value.discoveryCardId === 'string' &&
    typeof value.rationale === 'string' &&
    Number.isInteger(value.rank) &&
    typeof value.facilitatorSelected === 'boolean'
  )
}

function isIdeationNote(value: unknown): value is IdeationNote {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.text === 'string' &&
    typeof value.submittedBy === 'string' &&
    typeof value.curatedAt === 'string'
  )
}

export function isDerivedCard(value: unknown): value is DerivedCard {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.type === 'string' &&
    typeof value.title === 'string' &&
    typeof value.description === 'string' &&
    isStringArray(value.tags) &&
    typeof value.derivedFromId === 'string' &&
    Number.isInteger(value.derivedFromVersion) &&
    Number.isInteger(value.currentCanonicalGraphVersion) &&
    isEnumValue(value.staleness, 2)
  )
}

export function isDerivedCardArray(value: unknown): value is readonly DerivedCard[] {
  return Array.isArray(value) && value.every(isDerivedCard)
}

function isConcept(value: unknown): value is Concept {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.interventionType === 'string' &&
    typeof value.capability === 'string' &&
    typeof value.workflowChange === 'string' &&
    typeof value.technologyPattern === 'string' &&
    typeof value.autonomyLevel === 'string' &&
    isStringArray(value.trustImplications) &&
    isStringArray(value.dependencies) &&
    isStringArray(value.assumptionReferences) &&
    typeof value.validationPlan === 'string'
  )
}

function isOpportunity(value: unknown): value is Opportunity {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.problemId === 'string' &&
    typeof value.workflowId === 'string' &&
    typeof value.desiredOutcome === 'string' &&
    typeof value.kpiReference === 'string' &&
    typeof value.owner === 'string' &&
    typeof value.valueProfile === 'string' &&
    typeof value.confidenceProfile === 'string' &&
    isTrustProfile(value.trustProfile) &&
    isReadinessProfile(value.readinessProfile) &&
    isStringArray(value.evidenceReferences) &&
    Array.isArray(value.concepts) &&
    value.concepts.every(isConcept) &&
    isEnumValue(value.lifecycleState, 5) &&
    Number.isInteger(value.objectVersion)
  )
}

export function isDecisionRecord(value: unknown): value is DecisionRecord {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.opportunityId === 'string' &&
    isEnumValue(value.previousState, 5) &&
    isEnumValue(value.newState, 5) &&
    isEnumValue(value.decisionClass, 5) &&
    typeof value.rationale === 'string' &&
    isStringArray(value.evidenceReferences) &&
    isStringArray(value.dissent) &&
    typeof value.owner === 'string' &&
    typeof value.approvalPoint === 'string' &&
    typeof value.escalationPath === 'string' &&
    typeof value.timestamp === 'string' &&
    isStringArray(value.affectedAssumptions) &&
    Number.isInteger(value.objectVersion)
  )
}

function isGovernanceBlocker(value: unknown): value is GovernanceBlocker {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.opportunityId === 'string' &&
    isEnumValue(value.category, 9) &&
    typeof value.rationale === 'string' &&
    typeof value.remediationPath === 'string'
  )
}

export function isEngagement(value: unknown): value is Engagement {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.workspaceId === 'string' &&
    typeof value.methodVersion === 'string' &&
    typeof value.owner === 'string' &&
    typeof value.governanceOwner === 'string' &&
    Number.isInteger(value.objectVersion) &&
    isEnumValue(value.lifecycleState, 5) &&
    isStringArray(value.objectives) &&
    isStringArray(value.participants) &&
    Array.isArray(value.workflows) &&
    value.workflows.every(isWorkflow) &&
    Array.isArray(value.problems) &&
    value.problems.every(isProblem) &&
    Array.isArray(value.personas) &&
    value.personas.every(isPersona) &&
    Array.isArray(value.journeyMaps) &&
    value.journeyMaps.every(isJourneyMap) &&
    Array.isArray(value.cardShortlist) &&
    value.cardShortlist.every(isCardShortlistEntry) &&
    Array.isArray(value.ideationNotes) &&
    value.ideationNotes.every(isIdeationNote) &&
    Array.isArray(value.evidence) &&
    value.evidence.every(isEvidence) &&
    Array.isArray(value.opportunities) &&
    value.opportunities.every(isOpportunity) &&
    Array.isArray(value.decisions) &&
    value.decisions.every(isDecisionRecord) &&
    Array.isArray(value.blockers) &&
    value.blockers.every(isGovernanceBlocker)
  )
}

export function isEngagementArray(value: unknown): value is readonly Engagement[] {
  return Array.isArray(value) && value.every(isEngagement)
}

export function isOpportunityReview(value: unknown): value is OpportunityReview {
  return (
    isRecord(value) &&
    typeof value.opportunityId === 'string' &&
    typeof value.engagementId === 'string' &&
    typeof value.value === 'string' &&
    typeof value.confidence === 'string' &&
    isTrustProfile(value.trust) &&
    isReadinessProfile(value.readiness) &&
    typeof value.owner === 'string' &&
    isStringArray(value.evidenceReferences) &&
    Array.isArray(value.blockers) &&
    value.blockers.every(isGovernanceBlocker) &&
    (value.latestDecision === undefined ||
      value.latestDecision === null ||
      isDecisionRecord(value.latestDecision)) &&
    Number.isInteger(value.canonicalGraphVersion)
  )
}

export function isLiveSession(value: unknown): value is LiveSession {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.workspaceId === 'string' &&
    typeof value.engagementId === 'string' &&
    (value.journeyStepId === null || typeof value.journeyStepId === 'string') &&
    typeof value.joinCode === 'string' &&
    typeof value.createdBy === 'string' &&
    typeof value.createdAt === 'string' &&
    typeof value.expiresAt === 'string' &&
    typeof value.status === 'string'
  )
}

function isCardVoteTally(value: unknown): value is CardVoteTally {
  return (
    isRecord(value) &&
    typeof value.discoveryCardId === 'string' &&
    typeof value.journeyStepId === 'string' &&
    typeof value.count === 'number'
  )
}

export function isCardVoteTallyArray(value: unknown): value is readonly CardVoteTally[] {
  return Array.isArray(value) && value.every(isCardVoteTally)
}

function isCardPinTally(value: unknown): value is CardPinTally {
  return (
    isRecord(value) &&
    typeof value.discoveryCardId === 'string' &&
    typeof value.journeyStepId === 'string' &&
    Number.isInteger(value.count)
  )
}

export function isCardPinTallyArray(value: unknown): value is readonly CardPinTally[] {
  return Array.isArray(value) && value.every(isCardPinTally)
}

export function isPinToggleResult(value: unknown): value is PinToggleResult {
  return (
    isRecord(value) &&
    typeof value.pinned === 'boolean' &&
    Array.isArray(value.tally) &&
    value.tally.every(isCardPinTally)
  )
}

function isLiveIdeationNote(value: unknown): value is LiveIdeationNote {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.workspaceId === 'string' &&
    typeof value.joinSessionId === 'string' &&
    typeof value.participantId === 'string' &&
    typeof value.displayName === 'string' &&
    typeof value.text === 'string' &&
    typeof value.submittedAt === 'string'
  )
}

export function isLiveIdeationNoteArray(value: unknown): value is readonly LiveIdeationNote[] {
  return Array.isArray(value) && value.every(isLiveIdeationNote)
}

function isLiveBoardCard(value: unknown): value is LiveBoardCard {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.workspaceId === 'string' &&
    typeof value.joinSessionId === 'string' &&
    typeof value.placedByParticipantId === 'string' &&
    typeof value.placedByDisplayName === 'string' &&
    (value.discoveryCardId === null || typeof value.discoveryCardId === 'string') &&
    typeof value.x === 'number' &&
    typeof value.y === 'number' &&
    typeof value.rationale === 'string' &&
    typeof value.placedAt === 'string' &&
    typeof value.lastMovedAt === 'string'
  )
}

export function isLiveBoardCardArray(value: unknown): value is readonly LiveBoardCard[] {
  return Array.isArray(value) && value.every(isLiveBoardCard)
}

function isDiscoveryCardSuggestion(value: unknown): value is DiscoveryCardSuggestion {
  return (
    isRecord(value) &&
    typeof value.discoveryCardId === 'string' &&
    typeof value.rationale === 'string'
  )
}

export function isDiscoveryCardSuggestionResult(value: unknown): value is DiscoveryCardSuggestionResult {
  return (
    isRecord(value) &&
    Array.isArray(value.suggestions) &&
    value.suggestions.every(isDiscoveryCardSuggestion) &&
    isEnumValue(value.confidenceStatus, 3) &&
    typeof value.requiredReview === 'string' &&
    Number.isInteger(value.canonicalGraphVersion) &&
    typeof value.correlationId === 'string' &&
    typeof value.generatedBy === 'string' &&
    typeof value.generatedAt === 'string'
  )
}

export function isEvidenceQualityAssessment(value: unknown): value is EvidenceQualityAssessment {
  return (
    isRecord(value) &&
    typeof value.evidenceId === 'string' &&
    Array.isArray(value.concerns) &&
    value.concerns.every((concern) => typeof concern === 'string') &&
    typeof value.suggestion === 'string' &&
    isEnumValue(value.confidenceStatus, 3) &&
    typeof value.requiredReview === 'string' &&
    Number.isInteger(value.canonicalGraphVersion) &&
    typeof value.correlationId === 'string' &&
    typeof value.generatedBy === 'string' &&
    typeof value.generatedAt === 'string'
  )
}

function isWorkflowDraftContent(value: unknown): value is WorkflowDraftContent {
  return (
    isRecord(value) &&
    typeof value.trigger === 'string' &&
    isStringArray(value.actors) &&
    isStringArray(value.inputs) &&
    isStringArray(value.steps) &&
    isStringArray(value.decisions) &&
    isStringArray(value.systems) &&
    isStringArray(value.handoffs) &&
    isStringArray(value.exceptions) &&
    isStringArray(value.outputs)
  )
}

function isProblemDraftContent(value: unknown): value is ProblemDraftContent {
  return (
    isRecord(value) &&
    typeof value.user === 'string' &&
    typeof value.goal === 'string' &&
    typeof value.constraint === 'string' &&
    typeof value.impact === 'string' &&
    isStringArray(value.evidenceReferences) &&
    typeof value.confidence === 'number'
  )
}

function isFrameDraftCandidate(value: unknown): value is FrameDraftCandidate {
  return (
    isRecord(value) &&
    isWorkflowDraftContent(value.workflow) &&
    isProblemDraftContent(value.problem) &&
    isEnumValue(value.confidenceStatus, 3) &&
    typeof value.requiredReview === 'string' &&
    isStringArray(value.citationConcerns)
  )
}

export function isFrameDraftResult(value: unknown): value is FrameDraftResult {
  return (
    isRecord(value) &&
    Array.isArray(value.candidates) &&
    value.candidates.every(isFrameDraftCandidate) &&
    Number.isInteger(value.canonicalGraphVersion) &&
    typeof value.correlationId === 'string' &&
    typeof value.generatedBy === 'string' &&
    typeof value.generatedAt === 'string'
  )
}

function isBoardClusterSuggestion(value: unknown): value is BoardClusterSuggestion {
  return (
    isRecord(value) &&
    typeof value.label === 'string' &&
    Array.isArray(value.placementIds) &&
    value.placementIds.every((id) => typeof id === 'string') &&
    typeof value.rationale === 'string'
  )
}

export function isBoardClusterResult(value: unknown): value is BoardClusterResult {
  return (
    isRecord(value) &&
    Array.isArray(value.clusters) &&
    value.clusters.every(isBoardClusterSuggestion) &&
    Array.isArray(value.outlierPlacementIds) &&
    value.outlierPlacementIds.every((id) => typeof id === 'string') &&
    isEnumValue(value.confidenceStatus, 3) &&
    typeof value.requiredReview === 'string' &&
    typeof value.correlationId === 'string' &&
    typeof value.generatedBy === 'string' &&
    typeof value.generatedAt === 'string'
  )
}

export function isJoinLiveSessionResponse(value: unknown): value is JoinLiveSessionResponse {
  return (
    isRecord(value) &&
    typeof value.token === 'string' &&
    typeof value.workspaceId === 'string' &&
    typeof value.engagementId === 'string' &&
    typeof value.joinSessionId === 'string' &&
    (value.journeyStepId === null || typeof value.journeyStepId === 'string') &&
    (value.journeyStepName === null || typeof value.journeyStepName === 'string') &&
    (value.journeyStepPainPoint === null || typeof value.journeyStepPainPoint === 'string') &&
    Array.isArray(value.shortlistedDiscoveryCardIds) &&
    value.shortlistedDiscoveryCardIds.every((id) => typeof id === 'string')
  )
}

export function isDurableOperation(value: unknown): value is DurableOperation {
  return (
    isRecord(value) &&
    typeof value.id === 'string' &&
    typeof value.workspaceId === 'string' &&
    typeof value.operationType === 'string' &&
    isEnumValue(value.status, 4) &&
    typeof value.createdAt === 'string' &&
    typeof value.updatedAt === 'string' &&
    typeof value.correlationId === 'string' &&
    (value.resultReference === undefined ||
      value.resultReference === null ||
      typeof value.resultReference === 'string') &&
    (value.errorCode === undefined ||
      value.errorCode === null ||
      typeof value.errorCode === 'string') &&
    (value.errorDetail === undefined ||
      value.errorDetail === null ||
      typeof value.errorDetail === 'string') &&
    typeof value.retryAfterSeconds === 'number'
  )
}

function isArtifactNarrative(value: unknown): value is ArtifactNarrative {
  return (
    isRecord(value) &&
    typeof value.summary === 'string' &&
    typeof value.requiredReview === 'string' &&
    typeof value.generatedBy === 'string' &&
    typeof value.generatedAt === 'string'
  )
}

export function isArtifactEnvelope(value: unknown): value is ArtifactEnvelope {
  return (
    isRecord(value) &&
    typeof value.artifactId === 'string' &&
    isEnumValue(value.artifactType, 4) &&
    typeof value.engagementId === 'string' &&
    typeof value.opportunityId === 'string' &&
    Number.isInteger(value.sourceCanonicalGraphVersion) &&
    typeof value.methodVersion === 'string' &&
    typeof value.generatedAt === 'string' &&
    typeof value.generatedBy === 'string' &&
    isEnumValue(value.staleness, 2) &&
    isRecord(value.content) &&
    (value.narrativeSummary === null || isArtifactNarrative(value.narrativeSummary))
  )
}
