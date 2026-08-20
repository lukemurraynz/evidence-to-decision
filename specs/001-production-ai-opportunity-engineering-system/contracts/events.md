# Contract: Graph-Change Event Adapter

## Event envelope

- `eventId`
- `eventType`: OpportunityCreated | EvidenceAdded | EvidenceConflictDetected | ConceptCreated | AssumptionCreated | ExperimentStarted | ExperimentCompleted | ConfidenceChanged | GateEvaluationChanged | DecisionChanged | PilotStarted | OutcomeRecorded | EngagementCreated | EngagementDeleted
- `aggregateId`, `workspaceId`, `canonicalGraphVersion`
- `affectedOpportunityId` (optional): the specific opportunity affected by the event; absent for engagement-level events
- `actorReference`, `occurredAt`, `correlationId`
- `schemaVersion`: `1.1` adds `affectedOpportunityId`; `1.0` events without this field are treated as engagement-level

## Adapter rules

- The canonical Opportunity Graph remains authoritative.
- The initial transport is Azure Service Bus topics with consumer subscriptions.
- The initial topology is one topic with one subscription per downstream consumer.
- Delivery is at least once; consumers must be idempotent using `eventId` plus consumer identity.
- Idempotency claims must be atomic before side effects (no check-then-act pattern).
- Consumers retry transient failures up to five times at 10, 30, 60, 120, and 240 seconds, then dead-letter the event for operator review.
- Events remain replayable for 30 days, subject to the approved workspace and operational retention policy.
- Events are emitted after an auditable domain change and are safe to replay using `eventId` plus consumer identity.
- Consumers reread authorized canonical state before taking action.
- Consumers may request re-score, re-summarization, policy/readiness evaluation, or reviewer notification; they cannot directly replace canonical state.
- Delivery status, attempts, deduplication, failures, and compensating actions are auditable.
- Ordering is required per workspace where a consumer depends on graph version; global ordering is not assumed.
- Drasi is an allowed candidate implementation behind this adapter, not a contract dependency.

## Dead-letter operations and replay control

- Dead-letter queues must have defined ownership, alert thresholds, and operational response SLAs.
- Replay of dead-lettered messages requires a documented runbook with audit evidence of who replayed, when, and why.
- Replay operations must preserve idempotency guarantees and must not bypass canonical reread requirements.

## Event schema lifecycle and compatibility

- `schemaVersion` is mandatory and versioned per event contract release.
- Contract changes must be classified as non-breaking or breaking before release.
- Breaking changes require a new event schema version and a compatibility/migration plan for consumers.
- Deprecated event shapes require a sunset timeline and explicit consumer migration guidance.
