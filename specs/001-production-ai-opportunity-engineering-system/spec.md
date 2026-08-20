# Feature Specification: Production AI Opportunity Engineering System

**Feature Branch**: `[001-production-ai-opportunity-engineering-system]`

**Created**: 2026-08-15

**Status**: Draft

**Input**: User description: "Make all the recommended changes, use production-first framing, and set up the constitution and spec for this solution using Spec Kit."

## Clarifications

The following decisions were confirmed during specification clarification on 2026-08-16:

- The first runtime target is ASP.NET Core on .NET 10.
- Enterprise identity will use Microsoft Entra ID with customer workspace isolation.
- The first independently demonstrable delivery is User Story 1: evidence capture through explainable recommendation and human decision recording.
- The first deployment target is Azure Container Apps.
- Initial authorization will map Microsoft Entra external security groups to application roles and workspace permissions.
- The first canonical graph store will be Azure Cosmos DB for NoSQL.
- The first deployment will use controlled public HTTPS ingress with Entra authentication.
- Cosmos DB will partition engagement data by customer workspace and use session consistency initially.
- Initial application roles will be facilitator, reviewer, and admin.
- The first deployment will target Australia East as a single-region, zone-redundant environment.
- Cosmos DB will use single-region continuous backup initially.
- Full multimodal capture, including voice, transcripts, documents, and visuals, is in scope for US1.
- US1 will use Microsoft Foundry-hosted capabilities with Microsoft Agent Framework for agentic and multimodal processing.
- Each customer workspace will use an approved, customer-configured retention period and redaction policy for multimodal assets.
- Non-agent interactive API operations will target p95 latency under 500 ms; agent and multimodal processing will be asynchronous.
- Graph-change events will use Azure Service Bus topics with at-least-once delivery, idempotent consumers, and a 30-day replay window.
- US1 will use consumption-first Azure sizing, with scale evidence required before production capacity is increased.
- Entra groups will map separately to application roles and permitted workspace memberships.
- Transcripts below the approved confidence threshold will remain unvalidated until a human corrects and approves them.
- Authorization will use immutable Entra group object IDs for role mapping and explicit workspace IDs for membership records.
- Transcription confidence below 0.80 will require human correction and approval.
- The initial US1 pilot envelope is 10 concurrent engagements and 100 users.
- Canonical workspace data will use a 90-day initial retention period with documented restore procedures.
- Graph-change events will use one Service Bus topic with one subscription per downstream consumer.
- At least 95% of ordinary agent and multimodal jobs will complete within two minutes under the initial pilot envelope.
- Scaling beyond the pilot will use sustained API latency and asynchronous queue-depth guardrails.
- The initial Cosmos recovery objectives are RPO 15 minutes and RTO 4 hours.
- Service Bus consumers will use five exponential retries before dead-lettering failed messages.
- Scaling review will trigger when API p95 exceeds 750 ms or asynchronous queue age exceeds two minutes.
- The initial event throughput target is 50 graph-change events per second.
- Three dedicated Entra groups will map to facilitator, reviewer, and admin roles; workspace membership remains separate.
- The initial deployment will use a single production environment.
- Service Bus retry delays will be 10, 30, 60, 120, and 240 seconds.
- Cosmos restore objectives will be verified through an annual restore drill.
- US1 may autoscale within the fixed 10-engagement/100-user ceiling; work beyond the ceiling will be rejected or queued.
- Service Bus consumers will preserve event ordering per workspace; global ordering is not required.
- Azure capacity may autoscale only as needed to serve up to 10 concurrent engagements and 100 users.
- Admins may have broad application access, but workspace authorization, audit logging, and human approval gates remain mandatory.
- Event bursts above 50 events per second will be buffered and processed within the replay window.
- Azure Developer CLI (`azd`) will be the operator-controlled local deployment mechanism; CI/CD and automated environment promotion are out of scope initially.
- The deployment boundary includes Entra app/groups and workspace mappings, Azure infrastructure, Microsoft Foundry/Agent Framework configuration, and the ASP.NET Core application.
- Microsoft Fabric is an optional derived analytics layer and does not replace the canonical transactional store.
- Fabric Data Agent usage is limited to read-only analytics over governed derived datasets and must not mutate canonical graph state.

Planning defaults for autoscale guardrails, queue response handling, and operational limits are documented in `research.md` and validated through `tasks.md` and `quickstart.md` checkpoints.

## User Scenarios & Testing _(mandatory)_

### User Story 1 - Facilitate evidence-backed opportunity decisions (Priority: P1)

A facilitator or opportunity lead runs a customer engagement and captures evidence, workflows, problems, opportunities, assumptions, and decisions in one canonical system.

**Why this priority**: The facilitator is the primary production user and the system's main value is improving decision quality before investment.

**Independent Test**: A facilitator can complete a full engagement flow from evidence capture to decision record and produce a traceable opportunity portfolio without needing separate tools or hidden context.

**Acceptance Scenarios**:

1. **Given** a new engagement with participant inputs, **When** the facilitator records evidence and frames an opportunity, **Then** the opportunity is linked to source evidence, workflow context, trust signals, readiness status, and an explicit next decision.
2. **Given** conflicting claims about a problem or baseline, **When** the facilitator records both claims, **Then** the system preserves the conflict, flags the uncertainty, and prompts a validation action instead of collapsing the claims into a single answer.
3. **Given** a live workshop includes spoken discussion and visual material, **When** the facilitator captures multimodal inputs, **Then** the system stores speaker-attributed transcript segments, linked visual artifacts, and provenance metadata as first-class evidence objects.

---

### User Story 2 - Review opportunities asynchronously with governance context (Priority: P2)

An executive sponsor, architect, delivery lead, or governance reviewer can review the current state asynchronously without attending the live workshop.

**Why this priority**: Production adoption depends on decision-makers and control functions being able to review, challenge, and approve work outside the live facilitation moment.

**Independent Test**: A reviewer can open the current engagement state asynchronously, understand why an opportunity is recommended, see what is missing, and record a review decision or blocker.

**Acceptance Scenarios**:

1. **Given** an opportunity is prioritised for validation or pilot, **When** a reviewer opens the decision view, **Then** the system shows value, confidence, trust, readiness, rationale, owner, and outstanding blockers in one place.
2. **Given** an opportunity lacks a required trust, security, privacy, or ownership control, **When** the reviewer evaluates it, **Then** the system blocks progression and records the reason.

---

### User Story 3 - Handoff validated opportunities into delivery (Priority: P3)

A delivery lead or architect receives a production-ready handoff package derived from the canonical graph rather than recreated from notes or slides.

**Why this priority**: The solution must reduce rediscovery and improve delivery readiness, not stop at workshop outputs.

**Independent Test**: A delivery team can consume a pilot brief or architecture handoff artifact and begin planning without re-running discovery for already-captured information.

**Acceptance Scenarios**:

1. **Given** an opportunity reaches pilot or production-readiness review, **When** a handoff artifact is generated, **Then** it includes the problem, workflow, users, trust profile, autonomy, dependencies, KPI, owner, assumptions, and decision rationale.
2. **Given** the underlying graph changes after the handoff is generated, **When** the artifact is viewed again, **Then** it reflects the updated canonical state or shows that the viewed export is from an earlier version.

---

### Edge Cases

- What happens when the evidence is weak, contradictory, or stale at the time a recommendation is requested?
- What happens when customer evidence risks being mixed with reusable cards or cross-customer insights?
- How does the system handle opportunities that are high value but blocked by missing owner, governance approval, or measurable baseline?
- How does the system behave when the agent cannot recommend confidently or loses access to required context?
- How does the system handle transcription ambiguity, speaker attribution errors, or low-confidence extraction from voice/visual inputs?

## Requirements _(mandatory)_

### Functional Requirements

- **FR-001**: The system MUST maintain a canonical Opportunity Graph that links engagement, evidence, workflow, problem, opportunity, concept, assumption, experiment, decision, pilot, and outcome records.
- **FR-002**: The system MUST preserve provenance for all evidence, including source, author or participant where applicable, timestamp, original wording, and validation status.
- **FR-003**: The system MUST distinguish evidence, assumptions, participant preference, and measured outcomes as separate objects and MUST NOT silently merge them.
- **FR-004**: The system MUST allow facilitators to browse, filter, compare, pin, connect, and promote cards that are derived from canonical domain objects.
- **FR-005**: The system MUST provide explainable recommendations that show why a candidate fits, why it may not fit, what evidence was used, and what uncertainty remains.
- **FR-006**: The system MUST abstain, downgrade confidence, or require human review when evidence is incomplete, stale, or contradictory.
- **FR-007**: The system MUST support facilitated workshop mode, asynchronous review mode, executive review mode, and delivery handoff mode over the same canonical graph.
- **FR-008**: The system MUST require explicit owner, rationale, and relevant trust/readiness controls before an opportunity can progress to pilot or production-readiness stages.
- **FR-009**: The system MUST support role-based access control, auditable sensitive actions, and customer workspace or tenant isolation for engagement data.
- **FR-010**: The system MUST separate customer evidence from reusable cards, normalized source material, and cross-engagement intelligence.
- **FR-011**: The system MUST generate delivery-ready artifacts, including decision records, experiment definitions, pilot briefs, executive summaries, and architecture handoff views.
- **FR-012**: The system MUST maintain version history for cards, sources, method versions, and decision changes so historical engagements remain reproducible.
- **FR-013**: The Facilitator Agent MUST assist with summarization, challenge, comparison, validation planning, and handoff preparation, but MUST NOT make autonomous consequential decisions.
- **FR-014**: The production architecture MUST support enterprise identity integration, retention and deletion controls, audit logging, observability boundaries, and backup/recovery procedures.
- **FR-015**: The system MUST support multimodal evidence ingestion, including voice, transcript text, uploaded documents, and workshop visuals, while preserving modality-specific provenance and confidence metadata.
- **FR-016**: The system MUST provide speaker attribution, timestamped segments, and human-correctable transcript workflows for voice capture before evidence is treated as validated input for recommendations or decisions.
- **FR-017**: Agentic workflows MAY use multimodal context to propose summaries, opportunities, and validation actions, but MUST surface uncertainty and require human confirmation for consequential outputs.
- **FR-018**: The production architecture MUST support an event-processing adapter pattern for graph-change detection and downstream workflow triggers, with Drasi explicitly permitted as a candidate implementation.
- **FR-019**: Event-driven triggers for re-score, re-summarization, policy/readiness gating, and reviewer notification MUST be auditable, replay-safe, and non-authoritative relative to the canonical Opportunity Graph.
- **FR-020**: Agent runtime governance MUST fail closed when policy or guardrail configuration cannot be loaded or validated; silent default-allow behavior is prohibited.
- **FR-021**: Asynchronous operations for agentic and multimodal processing MUST expose a durable operation-status contract (operation identifier, polling endpoint, terminal states, and retry guidance) and MUST support idempotent request handling.
- **FR-022**: API and event contracts MUST define explicit schema versioning, backward-compatibility classification (breaking vs non-breaking), and deprecation/sunset handling with migration guidance.
- **FR-023**: Policy decisions, agent tool calls, and consequential workflow evaluations MUST be recorded in an externally durable append-only audit sink suitable for compliance and incident review.
- **FR-024**: The system MUST support a derived analytics projection for cross-engagement trend analysis (engagement type, technology preference, blocker distribution, and progression outcomes) without changing canonical authority boundaries.
- **FR-025**: Any Fabric Data Agent capability MUST be read-only, workspace-governed, and restricted to approved analytics datasets; it MUST NOT perform canonical writes, approvals, or gate overrides.
- **FR-026**: Fabric analytics and Data Agent enablement MUST validate platform prerequisites (supported capacity/SKU, tenant settings, and identity posture), and MUST block enablement when prerequisites are unmet.
- **FR-027**: Fabric-derived analytics access MUST enforce OneLake/workspace security boundaries, approved aggregation/de-identification rules for cross-workspace views, and auditable query access.
- **FR-028**: Where Direct Lake semantic models are used for portfolio analytics, the system MUST detect and surface fallback/query-mode behavior in validation evidence to protect metric trust.

### Key Entities _(include if feature involves data)_

- **Engagement**: A customer-specific working context containing objectives, participants, opportunities, decisions, and outcomes.
- **Evidence**: Attributable observed, measured, stated, external, interpreted, assumed, or hypothesized information attached to workflows and opportunities.
- **Opportunity**: The primary decision object linking a problem, workflow, desired outcome, evidence, value, confidence, trust, readiness, concepts, and lifecycle state.
- **Concept**: A specific implementation direction for an opportunity, including intervention type, autonomy, dependencies, trust implications, and validation plan.
- **Decision Record**: A traceable record of the chosen action, rationale, dissent, approvals, affected assumptions, and resulting lifecycle change.
- **Trust Profile**: A structured view of privacy, security, regulation, data sensitivity, oversight, auditability, and operational risk requirements.
- **Customer Workspace**: The isolation boundary for engagement-specific data, permissions, retention policy, and audit history.
- **Card**: A derived visual representation of a canonical domain object used for browse, comparison, recommendation, and handoff experiences.
- **Multimodal Evidence Asset**: A voice, transcript, image, document, or mixed-media artifact linked to an engagement with source metadata, extraction confidence, and validation status.

## Success Criteria _(mandatory)_

### Measurable Outcomes

- **SC-001**: At least 95% of prioritised opportunities in a production engagement have linked evidence provenance, an identified owner, and an explicit next decision.
- **SC-002**: Reviewers can complete an asynchronous decision review, including trust and readiness evaluation, without joining the live workshop in at least 90% of observed review cases.
- **SC-003**: At least 80% of pilot-ready opportunities are handed to delivery without substantial rediscovery of problem, workflow, owner, KPI, or trust context.
- **SC-004**: The system records an explicit rationale or blocker for 100% of opportunities that progress to validation, pilot, or rejection.
- **SC-005**: Recommendation views expose fit rationale, evidence basis, and uncertainty indicators for 100% of generated recommendations.
- **SC-006**: At least 90% of multimodal workshop evidence records include modality type, timestamp, and attributable source metadata before use in prioritisation decisions.
- **SC-007**: 100% of asynchronous agent and multimodal operations expose operation status through to a terminal state with auditable timestamps and correlation identifiers.
- **SC-008**: 100% of policy-denied, policy-escalated, and consequential agent tool-call decisions are present in the durable audit store with actor, workspace, rule/verdict, and correlation data.
- **SC-009**: Portfolio analytics can report engagement type demand, technology preference trends, and top blocker categories across approved scopes with reproducible metric definitions.

## Assumptions

- The facilitator or opportunity lead is the primary interactive production user.
- **Operational owner**: The delivery or platform engineering lead responsible for the production deployment, SLA, and operational runbooks.
- **Governance owner**: The enterprise AI governance lead or designated data-and-trust officer responsible for trust profile approval, policy gates, and audit sign-off.
- The initial production implementation can represent the Opportunity Graph in JSON-backed domain storage behind auditable APIs.
- Enterprise identity, access control, and customer-data isolation will be integrated with existing organizational capabilities rather than built from scratch in the first implementation.
- Technology adapters, including Microsoft-specific mappings, remain downstream of problem framing and evidence capture.
- Multimodal and voice capture services can be integrated using enterprise-approved providers with configurable retention and redaction controls.
- Drasi may be used as an event-processing adapter for real-time graph-change detection and workflow triggers, while the canonical Opportunity Graph remains the source of truth.
- Fabric may be adopted as a derived analytics and reporting plane after canonical contracts stabilize; Fabric Data Agent remains constrained to non-authoritative analytical queries.
- Fabric rollout requires explicit validation of tenant settings, capacity readiness, and OneLake/workspace security controls before enabling Data Agent access.
