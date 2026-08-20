---
description: "Implementation tasks for the production AI opportunity engineering system"
---

# Tasks: Production AI Opportunity Engineering System

**Input**: Design documents from `specs/001-production-ai-opportunity-engineering-system/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `quickstart.md`, and `contracts/`

**Organization**: Tasks are grouped by user story. The first runtime delivery is US1 using ASP.NET Core/.NET 10 with consumption-first sizing in a single production Azure Container Apps environment in Australia East with controlled public HTTPS ingress, autoscaling only to serve the fixed 10-engagement/100-user ceiling, API p95 above 750 ms or queue age above two minutes as scaling guardrails, Azure Cosmos DB for NoSQL partitioned by workspace with session consistency, continuous backup, 90-day initial retention, RPO 15 minutes, and RTO 4 hours, Microsoft Entra external-group authorization using three dedicated immutable group-object-ID role groups plus workspace IDs, broad admin access constrained by workspace authorization/audit/human approval, Microsoft Foundry-hosted capabilities with Microsoft Agent Framework for full multimodal capture, and one Azure Service Bus topic with one subscription per downstream consumer, per-workspace ordering, 50 events per second, burst buffering, five retries at 10/30/60/120/240 seconds, and dead-lettering. Work beyond the pilot ceiling is rejected or queued. The initial pilot envelope is 10 concurrent engagements and 100 users, with 95% ordinary agent/multimodal jobs completing within two minutes. Development and test data must remain isolated. Capacity and operational defaults are defined in `research.md` and validated during Phase 1 and Phase 6.

## Format

`[ID] [P?] [Story] Description`

- `[P]` means the task can run in parallel with other tasks in its phase.
- `[US1]`, `[US2]`, and `[US3]` map tasks to the prioritized user stories in `spec.md`.
- Every task names the exact file or directory it changes.

## Phase 1: Foundation and decision closure

**Purpose**: Confirm the production boundary and resolve the implementation choices that the current documentation intentionally leaves open.

- [x] T001 Resolve the Azure Container Apps environment separation, networking details, autoscale thresholds, capacity settings, and source-tree layout for the Australia East single-region zone-redundant consumption-first deployment, preserving the fixed 10-engagement/100-user ceiling and API p95/queue-age guardrails, as recorded in `specs/001-production-ai-opportunity-engineering-system/research.md`.
- [x] T002 [P] Define the Microsoft Entra external-group-to-role mapping configuration contract (three dedicated groups, immutable object ID inputs, workspace membership binding) and validation rules for least-privilege enforcement in `specs/001-production-ai-opportunity-engineering-system/research.md`.
- [x] T003 [P] Define Azure Cosmos DB retention/deletion strategy, RPO 15-minute/RTO 4-hour restore objectives, annual restore-drill success criteria, and recovery validation in `specs/001-production-ai-opportunity-engineering-system/research.md`.
- [x] T004 [P] Define expected agent tools, structured outputs, and guardrails (agent roles, permitted/forbidden operations, quality gates, authorization boundaries, event consumption semantics), including fail-closed policy enforcement, evaluation-only to enforce promotion criteria, and durable append-only audit sink requirements in `specs/001-production-ai-opportunity-engineering-system/contracts/agent-guardrails.md` and Microsoft Foundry/Agent Framework deployment details in `research.md`.
- [ ] T005 [P] Validate the initial 10-concurrent-engagement/100-user envelope and 95%-within-two-minutes asynchronous processing target alongside the US1 interactive API p95 under 500 ms target in `specs/001-production-ai-opportunity-engineering-system/research.md`.
- [x] T006 [P] Define the one-topic/one-subscription-per-consumer Azure Service Bus topology, 50-events/second throughput target, per-workspace ordering, atomic idempotency claim semantics, retries at 10/30/60/120/240 seconds, dead-letter policy with alerts/ownership/replay runbook, 30-day retention, and at-least-once delivery in `specs/001-production-ai-opportunity-engineering-system/research.md`.
- [x] T007 Define the local Azure Developer CLI deployment boundary, operator approvals, preview/plan evidence, environment-scoped deployment concurrency lock, `azd down --purge` typed-confirmation safety gate, and ordered Entra-to-infrastructure-to-agent-to-application validation in `specs/001-production-ai-opportunity-engineering-system/contracts/deployment.md`.

**Checkpoint**: Runtime implementation choices are explicit, source-grounded, and validated through executable Phase 1 checkpoints.

## Phase 2: Canonical model and platform contracts

**Purpose**: Establish foundations that all user stories depend on.

**Blocking rule**: No user-story implementation may begin until this phase is complete.

- [x] T008 Define the canonical Opportunity Graph aggregate boundaries, IDs, versions, relationships, and lifecycle transitions in `specs/001-production-ai-opportunity-engineering-system/data-model.md`.
- [x] T009 [P] Define evidence provenance, modality, validation, conflict, revision, and customer-data separation rules in `specs/001-production-ai-opportunity-engineering-system/data-model.md` and `contracts/evidence-capture.md`.
- [x] T010 [P] Define workspace isolation, RBAC, audit records, retention/deletion, and access-denied behavior in `specs/001-production-ai-opportunity-engineering-system/data-model.md` and `contracts/review-and-gates.md`.
- [x] T011 [P] Define recommendation fit dimensions, evidence citations, uncertainty, abstention, human-review requirements, and non-mutating behavior in `specs/001-production-ai-opportunity-engineering-system/contracts/opportunity-recommendation.md`.
- [x] T012 [P] Define trust/readiness gate predicates, blocker records, approval points, overrides, and consequential decision rules in `specs/001-production-ai-opportunity-engineering-system/contracts/review-and-gates.md`.
- [x] T013 [P] Define the event envelope, adapter boundary, canonical graph versioning, consumer reread rule, idempotency, replay safety, and non-authoritative semantics in `specs/001-production-ai-opportunity-engineering-system/contracts/events.md`.
- [x] T014 Define API error, authorization, correlation, idempotency, asynchronous operation-status wire contract (operation ID, status endpoint, terminal states, retry guidance), and API/event schema versioning plus deprecation conventions for the contracts in `specs/001-production-ai-opportunity-engineering-system/contracts/`.
- [x] T015 [P] Define derived card, source, export, and artifact version/staleness invariants in `specs/001-production-ai-opportunity-engineering-system/data-model.md` and `contracts/handoff-artifacts.md`.

**Checkpoint**: The canonical model and cross-cutting contracts are sufficient to implement each user story without introducing a second source of truth.

## Phase 3: User Story 1 - Facilitate evidence-backed opportunity decisions

**Goal**: Enable a facilitator to capture evidence, frame opportunities, compare alternatives, preserve conflicts, and record an explicit decision.

**Independent test**: Execute the US1 flow in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`; the evidence-to-decision chain is attributable, conflict-aware, and human-approved.

### Tests for User Story 1

- [x] T016 [P] [US1] Add contract validation cases for evidence capture, provenance, multimodal transcript correction, conflict preservation, and workspace authorization in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.
- [x] T017 [P] [US1] Add recommendation validation cases for fit rationale, evidence references, uncertainty, abstention, and human confirmation in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.

### Implementation for User Story 1

- [x] T018 [US1] Implement canonical engagement, workflow, problem, evidence, multimodal asset, and opportunity operations in the selected runtime source tree documented by `plan.md`.
- [x] T019 [US1] Implement evidence conflict links, immutable source wording, auditable corrections, and validation status transitions in the selected runtime source tree documented by `plan.md`.
- [x] T020 [US1] Implement derived card browse/filter/compare/link behavior without duplicating canonical domain state in the selected runtime source tree documented by `plan.md`.
- [x] T021 [US1] Implement advisory recommendation generation against authorized canonical context with rationale, limitations, unknowns, and abstention in the selected runtime source tree documented by `plan.md`.
- [x] T022 [US1] Implement human decision recording with owner, approval point, rationale, dissent, affected assumptions, and audit history in the selected runtime source tree documented by `plan.md`.

**Checkpoint**: US1 is independently demonstrable and no recommendation can silently become a consequential decision.

## Phase 4: User Story 2 - Review opportunities asynchronously with governance context

**Goal**: Allow authorized reviewers to evaluate value, confidence, trust, readiness, rationale, and blockers without attending the workshop.

**Independent test**: Execute the US2 flow in `quickstart.md`; a reviewer can record a decision and missing controls block progression.

### Tests for User Story 2

- [x] T023 [P] [US2] Add async-review contract cases for authorized read access, current graph version, evidence basis, rationale, and outstanding blockers in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.
- [x] T024 [P] [US2] Add gate-validation cases proving missing owner, KPI, privacy, security, governance, or oversight controls block pilot/production progression in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.

### Implementation for User Story 2

- [x] T025 [US2] Implement an asynchronous opportunity review projection containing value, confidence, trust, readiness, owner, rationale, blockers, and graph version in the selected runtime source tree documented by `plan.md`.
- [x] T026 [US2] Implement trust/readiness evaluation and durable governance blocker records in the selected runtime source tree documented by `plan.md`.
- [x] T027 [US2] Implement reviewer decisions, authorized overrides, approval points, escalation paths, and audit history in the selected runtime source tree documented by `plan.md`.
- [x] T028 [US2] Implement replay-safe event consumers for reviewer notification and policy/readiness reevaluation without allowing consumers to replace canonical graph state in the selected runtime source tree documented by `plan.md`.

**Checkpoint**: US2 is independently demonstrable through asynchronous review and fail-closed governance gates.

## Phase 5: User Story 3 - Handoff validated opportunities into delivery

**Goal**: Generate delivery-ready artifacts from canonical graph state without rediscovery.

**Independent test**: Execute the US3 flow in `quickstart.md`; generated artifacts contain required context and expose staleness after graph changes.

### Tests for User Story 3

- [x] T029 [P] [US3] Add handoff contract cases for pilot brief, executive summary, decision record, and architecture handoff required fields in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.
- [x] T030 [P] [US3] Add artifact reproducibility and staleness cases for canonical graph, method, card, and source versions in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.

### Implementation for User Story 3

- [x] T031 [US3] Implement read-only generation of pilot briefs, decision records, executive summaries, and architecture handoffs from authorized canonical graph state in the selected runtime source tree documented by `plan.md`.
- [x] T032 [US3] Implement artifact envelopes with source graph version, method version, referenced card/source versions, generator, timestamp, and staleness status in the selected runtime source tree documented by `plan.md`.
- [x] T033 [US3] Implement delivery handoff access controls, export audit records, and current-versus-stale artifact signaling in the selected runtime source tree documented by `plan.md`.
- [x] T040 [P] [US3] Define derived portfolio analytics projection contract (engagement type demand, technology preference trends, blocker categories, progression outcomes) and source-version metadata in `specs/001-production-ai-opportunity-engineering-system/data-model.md` and `contracts/handoff-artifacts.md`.
- [x] T041 [P] [US3] Define Fabric Data Agent access boundary as read-only over approved derived datasets with workspace-governed access and no canonical mutation authority in `specs/001-production-ai-opportunity-engineering-system/contracts/handoff-artifacts.md`.

**Checkpoint**: US3 is independently demonstrable and delivery artifacts remain traceable to the canonical graph version that produced them.

## Phase 6: Cross-cutting validation and production readiness

**Purpose**: Verify consistency, security, operations, and measurable outcomes before implementation or release.

- [x] T034 [P] Validate all `spec.md` acceptance scenarios against `quickstart.md`, `data-model.md`, and the contracts; record gaps in `specs/001-production-ai-opportunity-engineering-system/research.md`.
- [x] T035 [P] Verify SC-001 through SC-006 have an owner, measurement method, denominator/sample definition, and evidence source in `specs/001-production-ai-opportunity-engineering-system/research.md`.
- [x] T036 [P] Validate customer-workspace isolation, least privilege, sensitive-data handling, audit completeness, retention/deletion, backup/recovery, observability boundaries, and OWASP agentic baseline coverage for ASI01/ASI03/ASI06/ASI08/ASI09 against the constitution and `spec.md`.
- [x] T037 [P] Validate event replay, duplicate delivery, consumer failure, canonical graph reread, and non-authoritative trigger behavior against `specs/001-production-ai-opportunity-engineering-system/contracts/events.md`.
- [x] T038 Re-run the constitution check and document the result and any justified exception in `specs/001-production-ai-opportunity-engineering-system/plan.md`.
- [ ] T039 Run the complete `quickstart.md` validation flow, including asynchronous operation-status contract checks and policy decision audit-evidence checks, and record passed, failed, skipped, and not-run checks in the implementation change record.
- [x] T042 [P] Validate portfolio analytics metric reproducibility (engagement type, technology demand, blocker distribution, progression outcomes) against canonical source versions in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.
- [ ] T043 [P] Validate Fabric Data Agent governance controls: read-only query behavior, workspace scope enforcement, and prohibition of canonical writes or gate overrides in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.
- [ ] T044 [P] Validate Fabric/Data Agent enablement prerequisites (capacity readiness, required tenant settings, identity/security posture) and record pass/fail evidence in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.
- [ ] T045 [P] Validate portfolio semantic query-mode behavior (including Direct Lake fallback visibility where applicable) and capture metric-trust evidence in `specs/001-production-ai-opportunity-engineering-system/quickstart.md`.

## Dependencies and execution order

### Phase dependencies

- Phase 1 has no task dependency and validates the documented planning defaults.
- Phase 2 depends on Phase 1 and blocks all user-story implementation.
- Phases 3, 4, and 5 depend on Phase 2 and can proceed in parallel after the foundation checkpoint.
- Phase 6 depends on the completed user stories and the selected runtime implementation.

### User story dependencies

- US1 is the P1 first delivery slice and depends only on Phase 2.
- US2 is independently testable after Phase 2 but consumes the authorization, gate, and decision contracts.
- US3 is independently testable after Phase 2 but consumes canonical versioning and decision records.

### Parallel opportunities

- T002-T006 can run in parallel because each resolves a separate planning decision; T007 follows the selected deployment boundary and can proceed in parallel with the other planning decisions.
- T008-T012 and T014 can run in parallel after T007 where they target separate contract concerns.
- Test tasks within each user story can run in parallel.
- US1, US2, and US3 can be staffed in parallel after Phase 2, subject to shared contract review.

## Implementation strategy

### First delivery slice

1. Validate and execute the Phase 1 documented defaults.
2. Complete Phase 2 canonical model and contracts.
3. Implement and validate US1.
4. Stop for an independent US1 review before adding governance review and handoff capabilities.

### Incremental delivery

1. Add US2 without weakening US1 evidence and human-decision controls.
2. Add US3 using only canonical graph state and versioned projections.
3. Complete Phase 6 validation and production-readiness review.

### Definition of done for each story

- The story's independent quickstart flow passes.
- Authorization, provenance, audit, and failure behavior are covered.
- No customer evidence is promoted into reusable content implicitly.
- No agent recommendation changes consequential state without an authorized human action.

## Execution Timeline and Parallel Lanes

### Phase 1 Timeline (Planning foundation, no blocking dependencies)

| Lane               | Task | Blocking dependency | Handoff                            |
| ------------------ | ---- | ------------------- | ---------------------------------- |
| A (infrastructure) | T001 | None                | Deployment boundary definition     |
| B (identity)       | T002 | None                | Authorization contract             |
| C (storage)        | T003 | None                | Cosmos/retention strategy          |
| D (agent policy)   | T004 | None                | Agent guardrails                   |
| E (performance)    | T005 | None                | Performance SLO validation         |
| F (events)         | T006 | None                | Service Bus topology               |
| G (deployment)     | T007 | T001, T002          | Deployment contract + safety gates |

**Phase 1 Handoff Gate**: All planning defaults are explicit, documented, and validated. No constitution violations. Proceed to Phase 2 and user-story staffing.

---

### Phase 2 Timeline (Contracts and canonical model, blocks all user stories)

| Lane               | Task | Blocking dependency          | Handoff                       |
| ------------------ | ---- | ---------------------------- | ----------------------------- |
| 1 (graph)          | T008 | T001, T002, T003             | Opportunity Graph boundaries  |
| 2 (evidence)       | T009 | T008                         | Evidence provenance rules     |
| 3 (access)         | T010 | T008                         | Workspace/RBAC rules          |
| 4 (recommendation) | T011 | T009                         | Recommendation contract       |
| 5 (gates)          | T012 | T010                         | Trust/readiness predicates    |
| 6 (events)         | T013 | T006, T008                   | Event contract                |
| 7 (API)            | T014 | T009, T010, T011, T012, T013 | API versioning/error contract |
| 8 (artifacts)      | T015 | T008                         | Card/handoff staleness rules  |

**Phase 2 Handoff Gate**: Canonical model is sufficient for independent story delivery. All cross-cutting contracts are explicit and signed off. Proceed to parallel US1/US2/US3 implementation.

---

### Phases 3–5 Timeline (User stories, parallel lanes after Phase 2 handoff)

| Story   | Test Lane  | Implementation Lane  | Validation Lane       | Handoff                                                                           |
| ------- | ---------- | -------------------- | --------------------- | --------------------------------------------------------------------------------- |
| **US1** | T016, T017 | T018–T022            | T016, T017, T039      | US1 independently demonstrable; no recommendation → decision without human action |
| **US2** | T023, T024 | T025–T028            | T023, T024, T036      | US2 asynchronous review + fail-closed gates; blocker records persisted            |
| **US3** | T029, T030 | T031–T033, T040–T041 | T029, T030, T042–T045 | US3 handoff artifacts + Fabric analytics with governance                          |

**Execution rule**: US1 implementation proceeds immediately after Phase 2. US2/US3 staffing can begin in parallel but must not land breaking changes to shared Phase 2 contracts without Phase 2 owner sign-off.

**US1 Handoff Gate**: Passes quickstart.md independent test. Evidence capture → recommendation → decision flow is attributable, conflict-aware, and human-approved. Proceed to Phase 6 validation or US2 implementation.

---

### Phase 6 Timeline (Validation and production readiness, depends on completed user stories)

| Lane         | Task(s)   | Blocking dependency                   | Handoff                                                    |
| ------------ | --------- | ------------------------------------- | ---------------------------------------------------------- |
| Scenarios    | T034      | US1, US2, US3 implementation complete | All spec scenarios mapped and passed                       |
| Metrics      | T035      | US1, US2, US3 implementation complete | SC-001 through SC-009 have owners and measurement methods  |
| Security     | T036      | US1, US2, US3 implementation complete | OWASP baseline coverage validated                          |
| Events       | T037      | T028 implementation complete          | Event replay and consumer semantics validated              |
| Constitution | T038      | All phases complete                   | Constitution re-checked and any exceptions justified       |
| Execution    | T039      | All implementation complete           | Full quickstart flow validated (async/audit/policy checks) |
| Analytics    | T042–T045 | T040, T041 implementation complete    | Portfolio analytics and Fabric prerequisites validated     |

**Phase 6 Handoff Gate**: Production readiness sign-off. All acceptance scenarios passed. All success criteria have owners and evidence. Security baseline met. Proceed to release.

---

### Staffing Model

- **Phase 1**: One core architect/engineer per lane (7 total); focus on planning validation, not implementation.
- **Phase 2**: Two engineers: one on contracts (T008–T015 critical path), one on cross-cutting utilities (testing, audit, validation scaffolding).
- **US1 (Phase 3)**: Two engineers: one on tests and contract validation (T016–T017), one on implementation (T018–T022).
- **US2 (Phase 4)**: One engineer (after US1 complete): tests (T023–T024), then implementation (T025–T028).
- **US3 (Phase 5)**: One engineer (after Phase 2 contracts stable): tests (T029–T030), then implementation (T031–T033, T040–T041).
- **Phase 6**: One engineer on validation (T034–T039, T042–T045) running in parallel with Phase 5 if schedule permits.

---

### Risk and Gate Summary

| Gate                | Criteria                                                                             | Owner              | Escalation                                              |
| ------------------- | ------------------------------------------------------------------------------------ | ------------------ | ------------------------------------------------------- |
| **Phase 1 Handoff** | All planning defaults explicit; no constitution violations                           | Architecture Lead  | Proceed to Phase 2 or revise spec                       |
| **Phase 2 Handoff** | All contracts drafted and signed; canonical model sufficient for US1                 | Engineering Lead   | Revise/extend Phase 2 before proceeding to user stories |
| **US1 Handoff**     | quickstart.md independent test passes; no silent recommendation→decision             | Engineering Lead   | Do not proceed to US2 until fixed                       |
| **Phase 6 Handoff** | All acceptance scenarios passed; success criteria have owners; security baseline met | Product + Security | Release approved or iterate Phase 5                     |
