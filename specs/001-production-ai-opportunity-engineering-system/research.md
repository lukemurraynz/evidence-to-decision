# Research Notes: Production AI Opportunity Engineering System

**Date**: 2026-08-16  
**Source specification**: `spec.md`  
**Status**: Phase 0 planning baseline

## Decisions

### R-001: Keep the Opportunity Graph as a domain model first

**Decision**: Represent the graph as canonical domain objects with explicit relationships. Start with JSON-backed controlled storage behind auditable APIs; defer a graph database until measured access patterns justify it.

**Evidence**: `ai-envisioning-workshop-system-spec.md`, sections 141-142; `spec.md` assumption on JSON-backed storage.

**Rationale**: This preserves a small reversible first delivery and avoids coupling the method to a storage technology before query and scale requirements are known.

### R-002: Treat cards and exports as derived representations

**Decision**: Cards, comparisons, executive views, and handoff artifacts reference canonical object IDs and versions; they are not independent sources of truth.

**Evidence**: `ai-envisioning-workshop-system-spec.md`, sections 5, 24, 34, 125-127; normalized card schema.

### R-003: Preserve evidence classes and provenance

**Decision**: Store observed, measured, customer-stated, external, interpreted, assumed, and hypothesized information as distinguishable evidence records. Preserve source, participant/author, timestamp, original wording, interpretation, validation status, and modality metadata.

**Evidence**: `spec.md` FR-002, FR-003, FR-015, FR-016; `ai-envisioning-workshop-system-spec.md`, sections 10-14.

### R-004: Use human-gated, explainable automation

**Decision**: The Facilitator Agent may summarize, challenge, compare, and propose validation actions. It cannot make binding decisions. Recommendation responses must include fit rationale, evidence references, limitations, unknowns, and required human review.

**Evidence**: Constitution principles III and V; `spec.md` FR-005, FR-006, FR-013, FR-017.

### R-005: Keep event processing non-authoritative

**Decision**: Emit versioned graph-change events through an adapter boundary for re-score, re-summarization, policy/readiness gating, and reviewer notification. Consumers must reread canonical state, be replay-safe, and never mutate authoritative graph state without an auditable domain operation.

**Evidence**: `spec.md` FR-018 and FR-019; `ai-envisioning-workshop-system-spec.md`, sections 141 and 144. Drasi remains a candidate adapter, not a committed dependency.

### R-006: Separate customer evidence from reusable card content

**Decision**: Enforce customer workspace/tenant boundaries and separate engagement evidence storage and permissions from the reusable card library and normalized source assets.

**Evidence**: Constitution operational constraints; `spec.md` FR-009 and FR-010; normalized card schema traceability rules.

### R-007: Use Azure Developer CLI for local operator-controlled deployment

**Decision**: Use Azure Developer CLI (`azd`) as the first deployment orchestrator. Its deployment contract covers Entra prerequisites, infrastructure, agent configuration, the ASP.NET Core application, and end-to-end validation. CI/CD pipelines and automated promotion are out of scope for the initial delivery.

**Evidence**: User clarification on 2026-08-16; `contracts/deployment.md`; selected Azure architecture in `plan.md`.

**Rationale**: `azd` provides a repeatable local deployment path while keeping high-impact identity and production changes explicitly operator-controlled. The repository currently has no runtime or infrastructure source, so the deployment contract must precede implementation.

### R-008: Enforce fail-closed runtime governance for agent operations

**Decision**: Agent policy and guardrail configuration must fail closed at startup and runtime. If policy documents are missing, invalid, unreadable, or produce no active rules, the runtime must reject operation rather than continue in default-allow mode.

**Evidence**: `contracts/agent-guardrails.md`; `spec.md` FR-013, FR-017, FR-020, FR-023.

### R-009: Standardize an asynchronous operation status contract

**Decision**: Agentic and multimodal processing must expose a durable asynchronous operation contract with operation ID, status endpoint, terminal states, retry guidance, and idempotent submission semantics.

**Evidence**: `spec.md` FR-021; `contracts/events.md`; Phase 2 API conventions task in `tasks.md`.

### R-010: Govern schema lifecycle for API and event contracts

**Decision**: API and event contracts must classify changes as breaking or non-breaking, version schema explicitly, and include deprecation/sunset plus migration guidance when contracts evolve.

**Evidence**: `spec.md` FR-022; `contracts/events.md`; `tasks.md` API/versioning conventions.

### R-011: Add Fabric as a derived analytics plane with non-authoritative Data Agent access

**Decision**: Adopt Microsoft Fabric only as a derived analytics and reporting plane. Fabric Data Agent is permitted for read-only trend exploration over governed analytical datasets and is explicitly prohibited from mutating canonical graph state or bypassing trust/readiness gates.

**Evidence**: `spec.md` FR-024, FR-025; `data-model.md` source-of-truth rule; `contracts/handoff-artifacts.md` derived artifact boundary.

### R-012: Enforce Fabric readiness and governance prerequisites before Data Agent enablement

**Decision**: Data Agent rollout requires validated Fabric prerequisites: supported capacity profile, required tenant settings for AI processing, workspace identity/governance posture, and auditable access controls. Enablement is blocked when prerequisites are not met.

**Evidence**: `spec.md` FR-026, FR-027; Fabric skill guidance dated 2026-08-10.

### R-013: Treat Fabric semantic query-mode behavior as a metric trust concern

**Decision**: If Direct Lake is used for portfolio analytics, validation must explicitly confirm query-mode behavior and detect fallback conditions so reported metrics remain trusted and reproducible.

**Evidence**: `spec.md` FR-028; Fabric skill guidance on Direct Lake fallback detection.

## Open decisions

### Deployment topology

**Status**: Clarified and closed for planning; implementation executed in Phase 1 Task T001.

- **Container Apps environment**: Single Australia East region, zone-redundant, consumption-first sizing, controlled public HTTPS ingress.
- **VNet and ingress routing**: Use direct HTTPS ingress with Entra authentication for initial delivery; NSGs allow only required ingress/egress paths for app, identity, and observability dependencies.
- **Autoscale thresholds**: Scale review thresholds are API p95 above 750 ms or async queue age above two minutes. Runtime autoscale is constrained to the pilot ceiling and must reject/queue beyond 10 concurrent engagements and 100 users.
- **Capacity SKU and limits**: Consumption profile with `minReplicas=1`, `maxReplicas=3`, workload target of `1 vCPU/2 GiB` per replica for API runtime baseline; increase requires measured evidence and operator approval.
- **Networking dependencies**: Use managed identity with approved secret references; enable private connectivity patterns for Cosmos DB, Service Bus, and Key Vault when enterprise network policy requires private ingress/egress.

### Authorization and claims contract

**Status**: Clarified and closed for planning; implementation executed in Phase 1 Task T002.

- **Entra group mapping contract**: Three dedicated external security groups (facilitator, reviewer, admin) with immutable object ID configuration. Workspace membership is separate from role assignment and enforced at API/data authorization time. Least-privilege rules: facilitator cannot approve decisions, reviewer cannot modify canonical graph, admin can perform application operations but all workspace actions require workspace ownership/approval.
- **Configuration validation**: At startup or deployment time, validate that group object IDs resolve in the target tenant, that workspace ID mappings are consistent, that role-to-permission bindings are least-privilege, and that no user holds multiple conflicting roles for the same workspace. Reject deployments with stale/missing group references.
- **Claims and audit**: Entra ID provides group membership in JWT/token claims; application converts group object IDs to workspace permissions. All authorization decisions are logged with user, workspace, action, result, timestamp, and correlation ID.

### Storage durability and restore objectives

**Status**: Clarified and closed for planning; implementation executed in Phase 1 Task T003.

- **Cosmos DB configuration**: Single-region continuous backup in Australia East, 90-day initial retention, session consistency, workspace partitioning (partition key `/workspaceId`). RPO 15 minutes, RTO 4 hours.
- **Restore drill schedule and evidence**: Annual restore drill with pass criteria of recoverability within RPO 15 minutes and RTO 4 hours, including timestamped drill evidence, failure analysis, and remediation ownership.

### Multimodal boundaries and agent guardrails

**Status**: Clarified and closed for planning; implementation executed in Phase 1 Task T004.

- **Agent tools and permissions**: Agents may call:
  - Evidence capture and validation tools (record statements, observations, transcripts with source/timestamp/modality).
  - Recommendation and comparison tools (retrieve canonical context, generate fit rationale, cite evidence, express uncertainty, propose abstention).
  - Policy/readiness evaluation tools (readonly access to trust/readiness profiles, readonly access to gate predicates, propose blockers/overrides but cannot apply them).
  - Multimodal tools (speech-to-text with confidence reporting, vision-to-structured-output with extraction confidence).
  - Agents cannot: mutate canonical graph, write decisions, bypass approval gates, access other workspaces, or redact/delete evidence without workspace policy and audit logging.
- **Structured outputs**: Recommendation responses must include fit dimensions, evidence reference IDs, confidence/uncertainty levels, limitations, unknowns, and required human review actions.
- **Quality thresholds**: Transcription confidence below 0.80 remains unvalidated; human correction/approval is mandatory before use in consequential recommendations or decisions. Visual extractions below 0.80 confidence are marked as needing human review.
- **Customer retention/redaction policy**: Workspace-owned configuration specifies retention period and approved redaction rules for multimodal assets. Agents cannot override workspace policy.
- **Foundry/Agent Framework deployment**: Initial release uses prompt+tool-configuration hardening (no fine-tuning required), allowlisted tool bindings, versioned prompt/policy artifacts, and operator-approved promotion from evaluation-only to enforce mode.
- **Fail-closed policy enforcement**: Policy and guardrail artifacts must validate at startup and runtime; deployments fail when policy load or validation fails.
- **Policy rollout mode**: New policy bundles begin in evaluation-only mode and promote to enforcement mode only after measured verdict telemetry and explicit operator sign-off.
- **Audit durability**: Policy and tool-call decisions are written to an external durable append-only store; local logs alone are insufficient.

### Performance scale targets

**Status**: Clarified and closed for planning; implementation validation executed in Phase 6.

- **Interactive API p95**: Target under 500 ms for non-agent operations (read evidence, retrieve recommendations, record decisions).
- **Asynchronous processing**: 95% of ordinary agent and multimodal jobs complete within two minutes under the pilot envelope.
- **Initial pilot envelope**: 10 concurrent engagements, 100 users, 50 graph-change events per second.
- **Scaling guardrails**: API p95 sustained above 750 ms or asynchronous queue age above two minutes triggers scale review or capacity increase.
- **Load testing approach**: Phase 6 executes a defined validation profile at pilot envelope (10 concurrent engagements/100 users), verifies API p95 and async completion SLOs, and captures evidence for scale-change decisions.

### Event implementation details

**Status**: Clarified and closed for planning; implementation executed in Phase 1 Task T006.

- **Service Bus topic and subscriptions**: One topic, one subscription per downstream consumer (e.g., re-scorer, reviewer notifier, policy evaluator).
- **Event envelope**: Versioned event with event ID, aggregate/object ID, graph version, event type, actor, timestamp, correlation ID.
- **Delivery semantics**: At-least-once delivery; consumers must be idempotent (check idempotency key before acting).
- **Replay and ordering**: 30-day replay window; per-workspace ordering (global ordering not required); bursts above 50 events/second buffered within replay window.
- **Retry and dead-letter policy**: Five retries at 10, 30, 60, 120, and 240 seconds; dead-letter after final retry.
- **Consumer contract**: Rereads canonical graph state before acting; records result; never mutates authoritative state without auditable domain operation.
- **Idempotency implementation**: Consumers use atomic idempotency claim semantics (not check-then-act) before side effects.
- **DLQ operations**: Dead-letter backlog must have alert thresholds, ownership, replay/runbook procedure, and evidence of periodic validation.
- **Operational limits**: Queue-depth warning threshold 100, critical threshold 250, dead-letter critical threshold 25, replay retention 30 days, and monitoring alerts routed to the operational owner.

### Deployment and operational safety controls

**Status**: Clarified and closed for planning; implementation executed in Phase 1 Task T007.

- **Concurrency control**: Prevent concurrent `azd` deployments to the same environment using an environment-scoped lock.
- **Destructive teardown guard**: `azd down --purge` requires explicit typed confirmation and environment/subscription verification prior to execution.
- **Async contract validation**: Deployment validation must confirm operation-status endpoint behavior and terminal-state progression for asynchronous workloads.

### Fabric analytics augmentation

**Status**: Clarified and accepted as non-blocking for Phase 1; implementation can begin after canonical contracts stabilize.

- **Scope**: Portfolio analytics for engagement demand, technology preferences, blocker categories, and progression outcomes.
- **Authority boundary**: Fabric datasets are derived projections; canonical graph remains authoritative.
- **Data Agent boundary**: Read-only analytical query capability with workspace-governed access and no canonical writes.
- **Governance**: Cross-workspace reporting requires approved aggregation/de-identification controls and auditable access.
- **Fabric prerequisites**: Validate capacity and tenant readiness before Data Agent enablement; block rollout when prerequisites are unmet.
- **Security posture**: Enforce OneLake/workspace access boundaries and auditable query access for analytics consumers.
- **Metric trust**: Validate semantic query-mode behavior for analytics models and record fallback observations in evidence.

## Clarification decisions

- **Runtime**: ASP.NET Core on .NET 10.
- **Identity and data boundary**: Microsoft Entra ID with customer workspace isolation.
- **First delivery slice**: US1 only, covering evidence capture, conflict preservation, explainable recommendation, and human decision recording.
- **Deployment**: Azure Container Apps.
- **Authorization**: Microsoft Entra external security groups mapped to application roles and workspace permissions.
- **Canonical graph storage**: Azure Cosmos DB for NoSQL.
- **Deployment posture**: Controlled public HTTPS ingress with Entra authentication.
- **Cosmos data strategy**: Workspace partitioning with session consistency.
- **Initial roles**: Facilitator, reviewer, and admin.
- **Region posture**: Australia East, single-region and zone-redundant.
- **Backup posture**: Single-region continuous backup.
- **US1 multimodal scope**: Voice, transcripts, documents, and visuals with provenance and human validation gates.
- **Multimodal provider posture**: Microsoft Foundry-hosted capabilities with Microsoft Agent Framework.
- **Retention posture**: Customer-configured approved retention and redaction policy per workspace.
- **US1 interactive performance**: Non-agent API operations target p95 latency under 500 ms; agent and multimodal processing is asynchronous.
- **Event transport**: Azure Service Bus topics with at-least-once delivery, idempotent consumers, and 30-day replay retention.
- **Sizing posture**: Consumption-first Azure capacity with scale evidence required before increasing production capacity.
- **Workspace authorization**: Separate Entra group-to-role and workspace membership mappings.
- **Transcript validation**: Human correction and approval required before below-threshold transcripts can be used for recommendations or decisions.
- **Authorization claims**: Immutable Entra group object IDs plus explicit workspace IDs.
- **Transcription threshold**: Confidence below 0.80 requires human correction and approval.
- **Initial scale envelope**: 10 concurrent engagements and 100 users.
- **Graph retention**: 90 days initially, with documented restore procedures.
- **Service Bus topology**: One topic with one subscription per downstream consumer.
- **Async processing target**: At least 95% of ordinary agent and multimodal jobs complete within two minutes under the initial pilot envelope.
- **Scaling guardrails**: Sustained API latency and asynchronous queue depth trigger scale review or capacity increase.
- **Recovery objectives**: RPO 15 minutes and RTO 4 hours initially.
- **Consumer failure policy**: Five exponential retries followed by dead-lettering.
- **Scale review guardrails**: API p95 above 750 ms or asynchronous queue age above two minutes.
- **Event throughput target**: 50 graph-change events per second initially.
- **Entra group mapping**: Three dedicated groups for facilitator, reviewer, and admin, with separate workspace membership.
- **Environment posture**: Single production environment initially; development and test data must remain isolated from production.
- **Retry schedule**: 10, 30, 60, 120, and 240 seconds, then dead-letter.
- **Restore validation**: Annual restore drill with recorded RPO/RTO evidence.
- **Capacity policy**: Autoscale within the fixed 10-engagement/100-user ceiling; reject or queue work beyond it.
- **Event ordering**: Per-workspace ordering, without global ordering.
- **Autoscale scope**: Autoscale only to serve the 10-engagement/100-user pilot ceiling.
- **Admin safeguards**: Broad application access remains constrained by workspace authorization, audit logging, and human approval gates.
- **Event burst handling**: Buffer bursts above 50 events/second within the replay window.
- **Runtime governance mode**: New policy bundles start in evaluation-only mode and require explicit promotion to enforcement mode.
- **Audit sink requirement**: Policy/tool-call decision evidence must be persisted to a durable append-only store.
- **Async operation contract**: Agentic and multimodal jobs expose durable operation IDs, status endpoints, and terminal-state semantics.
- **Schema lifecycle policy**: API and event changes require explicit versioning, compatibility classification, and deprecation handling.
- **Deployment concurrency lock**: One active deployment per environment at a time.
- **Purge safety**: `azd down --purge` requires explicit typed confirmation and environment verification.
- **Fabric posture**: Fabric is a derived analytics plane, not a transactional system of record.
- **Data Agent posture**: Read-only on approved datasets; cannot approve decisions, progress lifecycle states, or override gates.
- **Fabric enablement gate**: Capacity/tenant/identity prerequisites must pass before Data Agent is enabled.
- **Direct Lake trust gate**: Fallback/query-mode behavior must be observable during validation for portfolio metrics.

## Constitution gate

- Evidence-Led Opportunity Engineering: **PASS**
- Production-First System Design: **PASS**
- Human Accountability and Governed AI: **PASS**
- Canonical Graph, Derived Cards: **PASS**
- Explainable and Conservative Automation: **PASS**

Implementation choices are now bounded by clarified defaults and explicit validation checkpoints; no constitution violations are identified.

## Agentic security baseline (OWASP)

Phase 6 validation must include a focused assessment for the highest-relevance OWASP Agentic Security categories in this architecture:

- **ASI01 (Agent Goal Hijack)**: prompt/context manipulations that attempt to redirect intended facilitation or review outcomes.
- **ASI03 (Identity and Privilege Abuse)**: over-privileged or cross-workspace access paths through agent/tool flows.
- **ASI06 (Memory and Context Poisoning)**: corrupted or unvalidated evidence/memory influencing recommendations.
- **ASI08 (Cascading Failures)**: downstream failure propagation in asynchronous/event-driven workflows.
- **ASI09 (Human-Agent Trust Exploitation)**: persuasive but under-evidenced outputs that could bypass human review intent.

Findings must map to preventive controls, detection signals, and remediation ownership before production readiness sign-off.

## Requirement traceability baseline

| Requirement area                | Planning evidence                                                  |
| ------------------------------- | ------------------------------------------------------------------ |
| FR-001 to FR-006                | `data-model.md`, `contracts/opportunity-recommendation.md`         |
| FR-007 to FR-010                | `data-model.md`, `contracts/review-and-gates.md`                   |
| FR-011 to FR-014                | `contracts/handoff-artifacts.md`, `quickstart.md`                  |
| FR-015 to FR-017                | `data-model.md`, `contracts/evidence-capture.md`                   |
| FR-018 to FR-019                | `contracts/events.md`                                              |
| FR-020 and FR-023               | `contracts/agent-guardrails.md`, `contracts/deployment.md`         |
| FR-021 to FR-022                | `contracts/events.md`, `contracts/deployment.md`                   |
| FR-024 to FR-025                | `data-model.md`, `contracts/handoff-artifacts.md`, `quickstart.md` |
| FR-026 to FR-028                | `research.md`, `quickstart.md`, `contracts/handoff-artifacts.md`   |
| Deployment and operations       | `contracts/deployment.md`                                          |
| Agent boundaries and guardrails | `contracts/agent-guardrails.md`                                    |

## Requirement-to-task execution coverage

| Requirement set   | Primary implementation tasks | Primary validation tasks |
| ----------------- | ---------------------------- | ------------------------ |
| FR-001 to FR-006  | T008, T009, T011             | T016, T017, T034         |
| FR-007 to FR-010  | T010, T012, T018, T022       | T023, T024, T036         |
| FR-011 to FR-014  | T015, T031, T032, T033       | T029, T030, T034         |
| FR-015 to FR-017  | T009, T018, T019, T021       | T016, T017, T039         |
| FR-018 to FR-019  | T013, T028                   | T037, T039               |
| FR-020 and FR-023 | T004, T007                   | T039                     |
| FR-021 to FR-022  | T014                         | T039                     |
| FR-024 to FR-025  | T040, T041                   | T042, T043               |
| FR-026 to FR-028  | T041                         | T044, T045               |

## Offline Phase 6 validation record

### Acceptance-scenario traceability (T034)

| Scenario | Implementation evidence | Deterministic evidence | Remaining live evidence |
| --- | --- | --- | --- |
| US1 evidence-backed decision | Canonical graph commands, immutable evidence corrections, conflict links, typed recommendation output, and human decision records | Domain and application tests cover provenance, conflict preservation, abstention inputs, decision accountability, and output validation | A live Foundry recommendation and deployed end-to-end journey remain blocked on Azure access |
| US2 asynchronous review | Derived review projections, deterministic gate evaluation, reviewer notifications, authorized decisions, and replay-safe canonical rereads | Unit tests cover duplicate delivery, current-state reread, non-mutating reevaluation, notifications, reviewer accountability, and blocked progression | Entra role-group and Service Bus broker behavior require the target Azure environment |
| US3 delivery handoff | Versioned pilot, decision, executive, architecture, and experiment artifacts derived from canonical graph state | Unit tests cover artifact required fields, source-version metadata, staleness, access control, and analytics reproducibility | Delivery-user observation and optional Fabric consumption require deployed services |

No contract or model gap blocks continued offline implementation. Live validation remains explicitly open in T039 and T043-T045 and must not be represented as passed.

### Success-criteria measurement ownership (T035)

| Criterion | Owner | Measurement and denominator | Evidence source |
| --- | --- | --- | --- |
| SC-001 | Product owner + implementation lead | Prioritised opportunities with evidence provenance, owner, and next decision divided by all prioritised opportunities; initial sample is 10 engagements; pass at 95% | Canonical graphs, decision records, and activity audit |
| SC-002 | Product owner | Reviews completed asynchronously without workshop attendance divided by all completed reviews; initial sample is 10 engagements; pass at 90% | Review projections, reviewer decision records, and access audit |
| SC-003 | Delivery lead | Pilot-ready handoffs accepted without rediscovery of problem, workflow, owner, KPI, or trust context divided by all pilot-ready handoffs; initial sample is 10 engagements; pass at 80% | Artifact envelopes and delivery acceptance review |
| SC-004 | Product owner + operations lead | Progressions with an explicit rationale or blocker divided by all validation, pilot, and rejection progressions; pass at 100% | Decision records, gate evaluations, and activity audit |
| SC-005 | Implementation lead + QA lead | Recommendations containing fit rationale, evidence basis, and uncertainty divided by all generated recommendations; pass at 100% | Stored recommendation projections and evaluation results |
| SC-006 | Implementation lead | Multimodal records with modality, timestamp, and attributable source divided by all multimodal records used in prioritisation; initial sample is 10 engagements; pass at 90% | Canonical multimodal assets, evidence records, and correction history |

### Security and operational control assessment (T036)

| Area | Offline result | Evidence and remaining gate |
| --- | --- | --- |
| Workspace isolation and least privilege | PASS (static and deterministic) | Workspace-scoped partitions, immutable object-ID role mapping, application authorization, and cross-workspace tests; live Entra validation remains open |
| Sensitive-data handling | PASS with production classification blocked | Prompts receive only authorized evidence context, browser/client design stores no tokens, and telemetry excludes prompt/evidence content; production retention cannot be approved until data classification is supplied |
| Audit completeness | PASS (deterministic) | Canonical mutations, policy decisions, recommendations, exports, analytics, and replay results use durable audit/projection stores with actor, workspace, version, and correlation metadata |
| Retention, deletion, and recovery | PASS (design and code), live drill open | Workspace deletion is administrator-only with exact confirmation; Cosmos continuous-backup and restore runbook exist; restore evidence requires Azure |
| Observability boundary | PASS (instrumentation), exporter validation open | Correlation identifiers propagate through API, operations, events, and agent activities; agent spans expose provider/model/outcome metadata without content; deployed exporter/dashboard evidence remains open |
| ASI01 goal hijack | PASS (preventive), adversarial live eval open | Retrieved evidence is explicitly untrusted data, structured output is schema-bound and validated, and prompt-injection attempts cannot grant tools or mutation authority |
| ASI03 identity and privilege abuse | PASS (deterministic), live identity open | Workspace and role checks are independent of model output; managed identity is used for Azure dependencies |
| ASI06 context poisoning | PASS (deterministic) | Unvalidated, low-confidence, or conflicting evidence causes abstention before a model call; citations and candidates are allowlisted against canonical context |
| ASI08 cascading failures | PASS (deterministic) | Durable operations, bounded retries, dead-lettering, idempotent claims, session ordering, and canonical rereads contain failures |
| ASI09 human trust exploitation | PASS (deterministic), usability observation open | Recommendations remain advisory, always identify required review, cannot mutate state, and progression gates require accountable human decisions |

The offline assessment identifies no critical or high unresolved code finding. Azure identity, recovery, telemetry-export, Foundry evaluation, load, and Fabric evidence remain release blockers rather than offline implementation defects.
