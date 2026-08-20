# Canonical Data Model: Production AI Opportunity Engineering System

## Source-of-truth rule

The Opportunity Graph is authoritative. Cards, recommendation views, comparisons, executive summaries, and handoff artifacts are derived projections and must carry the canonical object version used to produce them.

## Workspace boundary

A `CustomerWorkspace` owns engagement-specific data, authorization scope, retention policy, and audit history. Reusable cards and normalized source assets are separate collections with independent lifecycle and permissions.

## Core entities

### CustomerWorkspace

- `id`, `customerReference`, `retentionPolicy`, `status`
- authorization boundary for engagements, evidence, decisions, and audit records

### Engagement

- `id`, `workspaceId`, `objectVersion`, `methodVersion`, `objectives`, `participants`, `owner`, `governanceOwner`, `lifecycleState`
- contains the working context for one customer engagement

### Evidence

- `id`, `engagementId`, `type`, `statement`, `interpretation`, `sourceReference`, `participantReference`, `capturedAt`, `modality`, `confidence`, `validationStatus`, `objectVersion`
- may reference a `MultimodalEvidenceAsset`
- never silently merges with assumptions, preferences, or outcomes

### MultimodalEvidenceAsset

- `id`, `engagementId`, `modality`, `storageReference`, `capturedAt`, `sourceReference`, `speakerSegments`, `extractionConfidence`, `redactionStatus`, `validationStatus`
- transcript segments include speaker attribution, start/end timestamps, and correction status

### Workflow and Problem

- `Workflow`: trigger, actors, inputs, steps, decisions, systems, handoffs, exceptions, outputs
- `Problem`: user, workflow reference, goal, constraint, impact, evidence references, confidence

### Opportunity

- `id`, `engagementId`, `problemReference`, `workflowReference`, `desiredOutcome`, `kpiReference`, `valueProfile`, `confidenceProfile`, `trustProfile`, `readinessProfile`, `owner`, `lifecycleState`, `objectVersion`
- links concepts, assumptions, experiments, decisions, and outcomes

### Concept

- `id`, `opportunityId`, `interventionType`, `capability`, `workflowChange`, `technologyPattern`, `autonomyLevel`, `trustImplications`, `dependencies`, `assumptionReferences`, `validationPlan`

### Assumption and Experiment

- `Assumption`: claim, evidence basis, impact if false, status, owner
- `Experiment`: assumption reference, hypothesis, method, sample, metric, expected result, actual result, interpretation, confidence change, decision impact

### TrustProfile and ReadinessProfile

- `TrustProfile`: privacy, security, regulation, IP, user impact, decision impact, data sensitivity, auditability, human oversight, model risk, operational risk
- `ReadinessProfile`: owner, KPI, baseline, data, process stability, integration, governance, security, change capacity
- gate evaluations record status, blocker rationale, evaluator, and timestamp

### DecisionRecord

- `id`, `opportunityId`, `previousState`, `newState`, `decisionClass`, `rationale`, `evidenceReferences`, `dissent`, `owner`, `approvalPoint`, `timestamp`, `affectedAssumptions`, `objectVersion`

### Pilot and Outcome

- `Pilot`: scope, concept, KPI, baseline, target, owner, trust controls, dependencies, decision reference
- `Outcome`: baseline, target, actual, measurementMethod, period, adoption, unintendedConsequences, trustIncidents, resultState, recommendation

### Card and Source

- `Card`: stable ID, type, title, description, source reference/version, tags, lifecycle, derivedFrom object/version
- `Source`: repository/package/file or customer artifact reference, source version, provenance metadata, review state

### PortfolioAnalyticsProjection (derived)

- `id`, `projectionVersion`, `generatedAt`, `sourceWindow`, `scope`
- aggregate metrics for engagement type demand, technology preference frequency, blocker category rates, stage-conversion outcomes, and cycle-time trends
- derived from canonical graph snapshots/events; read-only by design
- may be consumed by Fabric semantic models and Fabric Data Agent queries under approved governance constraints

## Relationships and invariants

1. Every engagement-scoped record has exactly one workspace boundary.
2. Every recommendation, decision, pilot, and outcome links to attributable evidence or explicitly records that evidence is missing.
3. Contradictory evidence is represented as a conflict relationship; it is not averaged away.
4. Every material state change creates an append-only decision/audit event with actor, timestamp, rationale, and affected object version.
5. Derived cards and artifacts are stale when their recorded canonical version differs from the current graph version.
6. Reusable cards cannot reference customer evidence unless an explicit approved promotion process creates a separately governed derivative.
7. A consequential decision requires an accountable owner, approval point, and escalation path.
8. Event consumers may request recalculation or review but cannot become the source of truth.
9. Portfolio analytics projections are derived artifacts and must never be treated as canonical decision authority.
10. Data-agent query surfaces are read-only and cannot execute mutations, approvals, or gate overrides.
