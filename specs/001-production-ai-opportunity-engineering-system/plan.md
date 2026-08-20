# Implementation Plan: Production AI Opportunity Engineering System

**Branch**: `[001-production-ai-opportunity-engineering-system]` | **Date**: 2026-08-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `spec.md`, repository constitution, canonical system specification, and normalized card schema.

## Summary

Deliver a production-first, evidence-led Opportunity Engineering System that keeps a canonical Opportunity Graph as the system of record, supports facilitated and asynchronous decision workflows, and produces delivery-ready handoff artifacts with explicit trust/readiness gating and human accountability. Phase 0/1 establishes the domain model, provenance rules, contracts, and validation flow before runtime implementation choices are committed.

## Technical Context

**Language/Version**: C# with ASP.NET Core on .NET 10.

**Primary Dependencies**: Azure Developer CLI (`azd`) for local operator-controlled deployment, ASP.NET Core/.NET 10, Microsoft Entra external-group mapping, Azure Cosmos DB for NoSQL, Azure Container Apps, Microsoft Foundry-hosted capabilities, Microsoft Agent Framework, Azure Service Bus topics, and optional Microsoft Fabric for derived analytics/reporting.

**Storage**: Azure Cosmos DB for NoSQL as the initial JSON-backed canonical domain store behind controlled, auditable APIs, partitioned by customer workspace with session consistency, single-region continuous backup, and 90-day initial retention; initial recovery objectives are RPO 15 minutes and RTO 4 hours.

**Testing**: Scenario validation in `quickstart.md`, contract tests for `contracts/`, and implementation-specific unit/integration tests after stack selection.

**Target Platform**: Enterprise web production environment in Australia East, single-region and zone-redundant, deployed locally through `azd` across Entra prerequisites, infrastructure, agent configuration, and ASP.NET Core application layers. CI/CD is out of scope initially.

**Project Type**: Production system specification and implementation planning.

**Performance Goals**: Non-agent interactive API operations target p95 latency under 500 ms; agent and multimodal processing is asynchronous. Initial US1 load testing targets 10 concurrent engagements and 100 users, with at least 95% of ordinary agent/multimodal jobs completing within two minutes. Autoscaling may operate only to serve the fixed pilot ceiling; work beyond the ceiling is rejected or queued. Scaling review triggers at API p95 above 750 ms or queue age above two minutes; initial event throughput target is 50 events per second, with bursts buffered within the replay window.

**Constraints**: Controlled public HTTPS ingress with Entra authentication; a single production environment with development/test data isolated; customer-workspace isolation; three dedicated Entra role groups using immutable group object IDs plus explicit workspace IDs; broad admin access constrained by workspace authorization, audit logging, and human approval gates; consumption-first capacity with autoscaling only within the fixed 10-engagement/100-user ceiling; rejection or queuing beyond the ceiling; API p95 above 750 ms or queue age above two minutes as scaling guardrails; audit logging; retention/deletion; backup/recovery with RPO 15 minutes/RTO 4 hours and annual restore drills; per-workspace event ordering; trust/readiness gates; explainability; conservative automation; and separation of customer evidence from reusable content. Transcripts below 0.80 confidence require human correction and approval.
**Scale/Scope**: Multi-engagement, multi-role enterprise usage with a defined initial pilot envelope and controlled scale-review guardrails.

## Constitution Check

_GATE: Must pass before Phase 0 research. Re-check after Phase 1 design._

- **Status**: PASS for documentation planning with implementation-ready defaults and explicit validation checkpoints.
- **Evidence-Led Opportunity Engineering**: PASS. Plan and tasks preserve provenance, conflict visibility, and explicit uncertainty.
- **Production-First System Design**: PASS. Scope is framed around governed production workflows, not workshop-only output.
- **Human Accountability and Governed AI**: PASS. Decisions require owner/approval; facilitator agent remains assistive.
- **Canonical Graph, Derived Cards**: PASS. Opportunity Graph is canonical and cards/artifacts are derived.
- **Explainable and Conservative Automation**: PASS. Recommendation behavior requires fit rationale, uncertainty handling, and abstention paths.
- **Required Follow-up**: Execute Phase 1 and Phase 6 validation checkpoints, including policy/audit, async operation, and Fabric-readiness controls.

### Post-implementation constitution re-check (T038)

- **Status**: PASS for the implemented offline scope; no exception is required.
- **Evidence-Led Opportunity Engineering**: PASS. Recommendations and handoffs remain bound to canonical evidence references and source versions; conflicting or weak evidence produces abstention or review.
- **Production-First System Design**: PASS. Durable operations, replay-safe consumers, workspace-scoped persistence, notifications, audits, and runbooks are implemented rather than simulated.
- **Human Accountability and Governed AI**: PASS. Reviewer decisions require owner, rationale, approval point, and escalation path; derived reevaluation and recommendation output cannot progress lifecycle state.
- **Canonical Graph, Derived Cards**: PASS. Review notifications, review views, analytics, and artifacts are projections; event consumers reread but do not replace canonical state.
- **Explainable and Conservative Automation**: PASS. Typed outputs require fit explanations and limitations, citations and candidates are allowlisted, output is bounded, and human review remains mandatory.
- **Release boundary**: The constitution pass does not waive live Azure gates. T039 and T043-T045, data classification, Entra validation, Foundry evaluation, recovery drills, telemetry export, and load evidence remain open.

## Execution Control Summary

This summary mirrors the detailed requirement-to-task mapping in `research.md` and provides a release-control view for implementation planning.

| Requirement set   | Implementation owner tasks | Validation owner tasks |
| ----------------- | -------------------------- | ---------------------- |
| FR-001 to FR-006  | T008, T009, T011           | T016, T017, T034       |
| FR-007 to FR-010  | T010, T012, T018, T022     | T023, T024, T036       |
| FR-011 to FR-014  | T015, T031, T032, T033     | T029, T030, T034       |
| FR-015 to FR-017  | T009, T018, T019, T021     | T016, T017, T039       |
| FR-018 to FR-019  | T013, T028                 | T037, T039             |
| FR-020 and FR-023 | T004, T007                 | T039                   |
| FR-021 to FR-022  | T014                       | T039                   |
| FR-024 to FR-025  | T040, T041                 | T042, T043             |
| FR-026 to FR-028  | T041                       | T044, T045             |

## Project Structure

### Documentation (this feature)

```text
specs/001-production-ai-opportunity-engineering-system/
├── plan.md
├── spec.md
├── tasks.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
	├── evidence-capture.md
	├── opportunity-recommendation.md
	├── review-and-gates.md
	├── handoff-artifacts.md
	├── events.md
	├── deployment.md
	└── agent-guardrails.md
```

### Source Code (repository root)

```text
.
├── ai-envisioning-workshop-system-spec.md   # architecture and method source
├── ai-envisioning-workshop-system-assets/
│   ├── normalized-cards/                    # reusable, versioned card source
│   └── source-cards/                         # upstream source inventory
├── specs/001-production-ai-opportunity-engineering-system/
└── .specify/                                # Spec Kit workflow and constitution
```

**Structure Decision**: Use an ASP.NET Core/.NET 10 web API and service composition deployed by operator-controlled `azd` to a single-region, zone-redundant Azure Container Apps environment in Australia East with controlled public HTTPS ingress and consumption-first sizing, Azure Cosmos DB for NoSQL as the initial workspace-partitioned canonical store with continuous backup, and Microsoft Entra external-group authorization with separate role and workspace mappings. Full multimodal capture uses Microsoft Foundry-hosted capabilities with Microsoft Agent Framework and remains behind provenance, customer-configured retention/redaction, and human-validation gates; below-threshold transcripts require human correction and approval. The repository currently contains the domain/system specification and normalized assets but no runtime application, so the first implementation slice establishes canonical domain boundaries and auditable contracts before adding runtime modules. Cards, handoff artifacts, and optional Fabric portfolio analytics are derived from the graph and remain non-authoritative.

## Complexity Tracking

No constitution violations identified. Technology choices are explicitly documented with bounded implementation checkpoints and validation evidence requirements.

---

# Analysis and Consistency Report

## Coverage Analysis

### Requirement-to-Task Traceability

**Total Functional Requirements (FR)**: 28  
**Total Success Criteria (SC)**: 9  
**Total Implementation Tasks (T)**: 45 (T001–T007 Phase 1, T008–T015 Phase 2, T016–T045 Phases 3–6)

#### Traceability Matrix by Requirement Set

| Requirement Set                                                      | Count | Implementation Tasks                                                | Validation Tasks                                            | Coverage |
| -------------------------------------------------------------------- | ----- | ------------------------------------------------------------------- | ----------------------------------------------------------- | -------- |
| FR-001 to FR-006 (Canonical graph, provenance, evidence)             | 6     | T008, T009, T011                                                    | T016, T017, T034                                            | **100%** |
| FR-007 to FR-010 (Workflows, access control, isolation)              | 4     | T010, T012, T018, T022                                              | T023, T024, T036                                            | **100%** |
| FR-011 to FR-014 (Artifacts, versioning, agent role, infrastructure) | 4     | T015, T031, T032, T033                                              | T029, T030, T034                                            | **100%** |
| FR-015 to FR-017 (Multimodal, confidence, agent uncertainty)         | 3     | T009, T018, T019, T021                                              | T016, T017, T039                                            | **100%** |
| FR-018 to FR-019 (Events, adapter, non-authoritarianism)             | 2     | T013, T028                                                          | T037, T039                                                  | **100%** |
| FR-020 and FR-023 (Agent policy, audit sink)                         | 2     | T004, T007                                                          | T039                                                        | **100%** |
| FR-021 to FR-022 (Async ops, schema versioning)                      | 2     | T014                                                                | T039                                                        | **100%** |
| FR-024 to FR-025 (Fabric analytics, Data Agent)                      | 2     | T040, T041                                                          | T042, T043                                                  | **100%** |
| FR-026 to FR-028 (Fabric prerequisites, OneLake, Direct Lake)        | 3     | T041                                                                | T044, T045                                                  | **100%** |
| **TOTAL COVERAGE**                                                   | 28    | T004-T009, T011-T015, T018-T019, T021-022, T028, T031-033, T040-041 | T016-T017, T023-T024, T029-T030, T034, T036-T039, T042-T045 | **100%** |

#### Success Criteria Coverage

| Criteria                                                               | Type       | Implementation support | Validation task        | Owner                        |
| ---------------------------------------------------------------------- | ---------- | ---------------------- | ---------------------- | ---------------------------- |
| **SC-001**: 95% opportunities with linked provenance, owner, decision  | Evidence   | T009, T018, T019, T022 | T034, T039             | Implementation + Validation  |
| **SC-002**: 90% async review without live attendance                   | Process    | T025, T026, T027       | T023, T024             | US2 Implementation           |
| **SC-003**: 80% pilot-ready with minimal rediscovery                   | Handoff    | T031, T032, T033       | T029, T030             | US3 Implementation           |
| **SC-004**: 100% rationale/blocker recorded for progression            | Records    | T022, T026, T027       | T034, T039             | Implementation + Validation  |
| **SC-005**: 100% recommendations expose rationale/evidence/uncertainty | Output     | T021                   | T017, T034             | US1 Implementation           |
| **SC-006**: 90% multimodal metadata with modality/timestamp/source     | Capture    | T009, T018, T019       | T016, T039             | US1 Implementation           |
| **SC-007**: 100% async ops expose terminal status with audit           | Operations | T014, T021             | T039                   | Implementation + Validation  |
| **SC-008**: 100% policy decisions in durable audit store               | Audit      | T004, T007             | T039                   | Phase 1 Foundation           |
| **SC-009**: Portfolio analytics metric reproducibility                 | Analytics  | T040, T041             | T042, T043, T044, T045 | US3/Analytics Implementation |

**Coverage Summary**: All 28 functional requirements and all 9 success criteria have explicit task assignments. No requirement is unaddressed or implicitly deferred.

---

## Gap Detection

### Gaps Identified: **None**

**Verification**:

- All FR 001–028 are mapped to implementation and validation tasks.
- All SC 001–009 have defined measurement methods and validation checkpoints.
- All Phase 1 planning defaults are clarified in `research.md`.
- All Phase 2 contracts have explicit task ownership.
- All three user stories (US1, US2, US3) have independent test/implementation/validation lanes.
- Fabric analytics and Data Agent governance scoped with prerequisite gates.
- OWASP agentic baseline (ASI01/03/06/08/09) assigned to Phase 6 validation (T036).

**Minor clarifications scoped to implementation**:

- Exact Cosmos DB failover timing (covered under T003 restore-drill criteria).
- Foundry/Agent Framework version and policy template format (covered under T004).
- Service Bus DLQ replay runbook specifics (covered under T006).
- ASP.NET Core middleware ordering and error-handling detail (covered under T014).

These are not gaps; they are bounded implementation choices identified during Phase 1 and resolved through explicit validation in Phase 6.

---

## Risk Assessment

### Scheduling Risks

| Risk                                                    | Trigger                                                              | Mitigation                                                                                             | Owner             | Priority   |
| ------------------------------------------------------- | -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ----------------- | ---------- |
| **Phase 2 blocking gate delays user story staffing**    | T008–T015 slips more than 5 days                                     | Execute T008–T012 in parallel; defer T014/T015 if needed (can merge with US1/US2 API work)             | Engineering Lead  | **HIGH**   |
| **Shared contract mutations between stories**           | US2 or US3 changes Phase 2 contracts after handoff                   | Explicit Phase 2 owner sign-off required for any Phase 2 contract changes after gate                   | Architecture Lead | **HIGH**   |
| **US1 handoff blocked on security/audit validation**    | T039 (quickstart validation) discovers missing audit/policy controls | Run T039 early (end of US1, not end of Phase 5); remediate before proceeding                           | Validation Owner  | **MEDIUM** |
| **Async processing targets unmet under pilot envelope** | T005 validation shows 95%-within-2-min SLO unachievable              | Scale engineering effort for async components (T021, T028); escalate to product for SLO adjustment     | Engineering Lead  | **MEDIUM** |
| **Fabric prerequisites block analytics rollout**        | T044 discovers missing capacity or tenant settings                   | Plan Fabric enablement after Phase 5 user-story implementation; prerequisite validation is a hard gate | Product/Cloud Ops | **LOW**    |

### Dependency Risks

| Dependency                                        | Critical path                                   | Remedy                                                                                                                    | Owner               |
| ------------------------------------------------- | ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| **Microsoft Entra group resolution** (T002)       | All authorization in Phases 3–5                 | Validate group object IDs in Phase 1; document group lifecycle and deprovisioning                                         | Azure Ops           |
| **Cosmos DB restore procedure** (T003)            | Phase 6 production-readiness validation         | Conduct annual restore drill in Phase 1; evidence required for Phase 1 handoff                                            | Data Ops            |
| **Agent policy artifact format** (T004)           | Phase 6 OWASP baseline validation               | Determine Foundry/Agent Framework policy format early in Phase 1; align with evaluation-only → enforce promotion workflow | Foundry Architect   |
| **Service Bus topology stability** (T006, T013)   | Event consumer implementation across Phases 4–5 | Finalize topic/subscription design in Phase 1; allow no mutations after Phase 2 handoff                                   | Infrastructure Lead |
| **Multimodal confidence thresholds** (T009, T019) | US1 evidence validation (T016, T039)            | Baseline confidence threshold at 0.80 in Phase 1; gather early user feedback in Phase 3 pilot                             | US1 Tech Lead       |

### Staffing Risks

| Phase                    | Staffing model                     | Risk                                                         | Remedy                                                                                    |
| ------------------------ | ---------------------------------- | ------------------------------------------------------------ | ----------------------------------------------------------------------------------------- |
| **Phase 1**              | 7 engineers (1 per lane)           | Single-lane bottleneck if architect unavailable              | Cross-train 2 engineers on each planning decision                                         |
| **Phase 2**              | 2 engineers (critical path)        | Contract review bottleneck; slow handoff                     | Allocate senior + mid-level engineer; asynchronous code review with 24-hour SLA           |
| **US1 (Phase 3)**        | 2 engineers                        | Test-first approach may require rework if contracts unstable | Defer full test automation until Phase 2 contracts locked; smoke tests only during US1    |
| **US2/US3 (Phases 4–5)** | 1 engineer each                    | Sequential delivery extends timeline                         | Hire/reassign earlier; parallelize US1 and US2 validation if staffing available           |
| **Phase 6**              | 1 engineer (parallel with Phase 5) | Validation discovers critical defects late                   | Run Phase 6 smoke tests during Phase 5 implementation; do not defer all validation to end |

---

## Consistency Checks

### Spec / Plan / Tasks Alignment

| Aspect                      | Spec                                           | Plan                                      | Tasks                            | Status         |
| --------------------------- | ---------------------------------------------- | ----------------------------------------- | -------------------------------- | -------------- |
| Canonical graph authority   | FR-001; SC-001, SC-009                         | ✓ (canonical store)                       | T008, T015, T031–T033, T040–T041 | **CONSISTENT** |
| Evidence provenance         | FR-002, FR-003, FR-015–FR-017; SC-001, SC-006  | ✓ (auditable)                             | T009, T016, T018, T019           | **CONSISTENT** |
| Human accountability        | FR-008, FR-013, FR-020, FR-023                 | ✓ (approval gates + audit)                | T004, T022, T026, T027           | **CONSISTENT** |
| Recommendation non-mutation | FR-005, FR-006, FR-013, FR-021, FR-025; SC-005 | ✓ (assistive only)                        | T021, T025, T028                 | **CONSISTENT** |
| Workspace isolation         | FR-009, FR-010; SC-001                         | ✓ (partitioned store)                     | T010, T018, T031–T033            | **CONSISTENT** |
| Multimodal confidence       | FR-015, FR-016, FR-017; SC-006                 | ✓ (0.80 threshold + human review)         | T009, T016, T019, T039           | **CONSISTENT** |
| Event non-authoritarianism  | FR-018, FR-019; SC-007                         | ✓ (reread before acting)                  | T013, T028, T037                 | **CONSISTENT** |
| Fabric derived-only posture | FR-024, FR-025; SC-009                         | ✓ (non-authoritative analytics)           | T040, T041, T042–T045            | **CONSISTENT** |
| Async operation contract    | FR-021; SC-007                                 | ✓ (operation ID, status, terminal states) | T014, T039                       | **CONSISTENT** |
| Schema versioning           | FR-022                                         | ✓ (explicit versions)                     | T014, T037                       | **CONSISTENT** |
| Audit durability            | FR-023; SC-008                                 | ✓ (external durable store)                | T004, T007, T039                 | **CONSISTENT** |
| Deployment safety           | FR-014; T007                                   | ✓ (concurrency lock, typed confirmation)  | T007, T039                       | **CONSISTENT** |

**Consensus**: No contradictions found. Spec, plan, and tasks are mutually consistent with explicit traceability.

### Contract Completeness

| Contract File                   | Spec coverage                         | Phase 2 task           | Status       |
| ------------------------------- | ------------------------------------- | ---------------------- | ------------ |
| `evidence-capture.md`           | FR-002, FR-003, FR-015–FR-017         | T009                   | **Complete** |
| `opportunity-recommendation.md` | FR-005, FR-006, FR-013                | T011                   | **Complete** |
| `review-and-gates.md`           | FR-008, FR-009                        | T010, T012             | **Complete** |
| `handoff-artifacts.md`          | FR-011, FR-012, FR-024, FR-025–FR-028 | T015, T040, T041       | **Complete** |
| `events.md`                     | FR-018, FR-019                        | T013                   | **Complete** |
| `deployment.md`                 | FR-014, FR-020, FR-021, FR-023        | T007                   | **Complete** |
| `agent-guardrails.md`           | FR-020, FR-023                        | T004                   | **Complete** |
| `data-model.md`                 | All FR                                | T008, T009, T010, T015 | **Complete** |

**Consensus**: All contracts are in scope. No orphaned contracts or missing contract definitions.

---

## Quality Metrics

### Definition of Done Verification

| Criterion                                                           | How verified                             | Owner               | Phase gate        |
| ------------------------------------------------------------------- | ---------------------------------------- | ------------------- | ----------------- |
| **Behavior implemented with clear inputs, outputs, error handling** | Code review + unit tests                 | Implementation Lead | Phase 1–6         |
| **Production dependencies wired (no mocks, stubs, fallbacks)**      | Integration test + deployment validation | Implementation Lead | Phase 1–6         |
| **Tests added/updated when behavior changes**                       | Test audit + coverage report             | QA Lead             | Phase 3–6         |
| **Security/privacy/least-privilege reviewed**                       | Security checklist + T036 validation     | Security Lead       | Phase 6           |
| **Cost and sustainability impacts documented**                      | Resource calc + cost model               | Cloud Ops           | Phase 1 + Phase 6 |
| **Documentation updated (behavior, contracts, config, setup, ops)** | Doc review + link verification           | Tech Writer         | Per phase         |
| **All checks (passing, failing, skipped) reported exactly**         | Test report + evidence capture           | QA Lead             | Phase 6           |

### Success Criteria Measurement Methods

| SC         | Measurement method                                                                                                                                  | Sample                               | Evidence owner           | Phase   |
| ---------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------ | ------------------------ | ------- |
| **SC-001** | Audit engagement records: count opportunities with linked evidence provenance, owner, decision; divide by total opportunities                       | 10 engagements, 95% threshold        | Product + Implementation | Phase 6 |
| **SC-002** | Reviewer access log: count reviews completed without attendee presence; divide by total reviews                                                     | 90% of 10 engagements, 90% threshold | Product                  | Phase 6 |
| **SC-003** | Handoff artifact review: count opportunities handed without problem/workflow/owner/KPI rediscovery; divide by pilot-ready total                     | 10 engagements, 80% threshold        | Delivery Partner         | Phase 6 |
| **SC-004** | Decision record audit: 100% of opportunities progressing to validation/pilot/rejection must have rationale or blocker; divide by total progressions | 10 engagements, 100% threshold       | Product + Ops            | Phase 6 |
| **SC-005** | Recommendation output inspection: 100% of recommendations expose fit rationale, evidence basis, uncertainty; divide by total recommendations        | 10 engagements, 100% threshold       | Implementation + QA      | Phase 6 |
| **SC-006** | Multimodal asset metadata audit: count assets with modality, timestamp, source; divide by total captured assets                                     | 10 engagements, 90% threshold        | Implementation           | Phase 6 |
| **SC-007** | Async operation trace audit: 100% of async operations expose terminal status with correlation ID; divide by total operations                        | 10 engagements, 100% threshold       | Implementation           | Phase 6 |
| **SC-008** | Audit sink query: count policy-denied/escalated/tool-call decisions in durable store; divide by total policy evaluations                            | 10 engagements, 100% threshold       | Security                 | Phase 6 |
| **SC-009** | Portfolio analytics query: engagement type, technology, blockers, progression metrics reproducible from canonical source versions                   | 10 engagements, verified metrics     | Analytics                | Phase 6 |

**Consensus**: All success criteria have measurable, observable, achievable outcomes.

---

## Production Readiness Checklist

### Pre-Phase 1 (NOW)

- [x] Spec documented with all 28 FR and 9 SC.
- [x] Constitution check passed.
- [x] OWASP agentic baseline identified.
- [x] Research.md clarifications closed.
- [x] Tasks.md complete with parallel lanes and staffing model.

### Post-Phase 1 (Handoff gate)

- [ ] T001–T007 complete and signed off.
- [ ] Planning defaults documented in deployment.md.
- [ ] Azure Container Apps target environment provisioned with networking and autoscaling configured.
- [ ] Entra group object IDs resolved and validated in target tenant.
- [ ] Cosmos DB topology and restore procedure documented with annual drill evidence.
- [ ] Agent policy format and evaluation-only → enforce promotion workflow defined.
- [ ] Service Bus topology finalized and operational.
- [ ] No planning decisions remain open or deferred.

### Post-Phase 2 (Canonical model handoff gate)

- [ ] T008–T015 complete and signed off.
- [ ] All contracts (evidence-capture, opportunity-recommendation, review-and-gates, handoff-artifacts, events, deployment, agent-guardrails) finalized.
- [ ] Data model with Opportunity Graph, evidence, decision, and analytics projections defined.
- [ ] Async operation contract (operation ID, status, terminal states, retry guidance) specified.
- [ ] API error, authorization, correlation, schema versioning, and deprecation conventions defined.
- [ ] Derived card and artifact version/staleness invariants specified.

### Post-US1 (First delivery handoff gate)

- [ ] T016–T022 complete and signed off.
- [ ] US1 quickstart flow passes independently.
- [ ] Evidence capture → conflict preservation → recommendation → decision chain is attributable, auditable, human-approved.
- [ ] No recommendations autonomously change state.
- [ ] Authorization, provenance, audit, and failure behavior covered.
- [ ] No customer evidence promoted to reusable content implicitly.

### Post-Phase 6 (Production readiness)

- [ ] T034–T045 complete and signed off.
- [ ] All spec acceptance scenarios validated.
- [ ] SC-001 through SC-009 measurement methods verified with evidence.
- [ ] Security baseline (customer isolation, least privilege, audit, OWASP ASI01/03/06/08/09) validated.
- [ ] Event replay, duplicate delivery, consumer failure, and non-authoritative semantics validated.
- [ ] Constitution re-checked and any exceptions justified.
- [ ] Full quickstart flow validated (async/audit/policy checks).
- [ ] Fabric prerequisites validated; Data Agent governance confirmed.
- [ ] Portfolio analytics metric reproducibility verified.
- [ ] Direct Lake fallback behavior observable.
- [ ] No open issues or deferred requirements.

---

## Summary and Recommendation

**Specification Status**: Complete and internally consistent.

**Task Coverage**: 100% of functional requirements and success criteria have explicit task assignments across 45 tasks organized in 6 phases with documented parallel lanes, staffing model, and gate criteria.

**Readiness for Phase 1 Start**: APPROVED. All prerequisites are met. No blocking issues, gaps, or contradictions identified. Implementation can proceed to Phase 1 planning defaults resolution and Azure infrastructure provisioning.

**Critical Path**: Phase 1 (7 days) → Phase 2 (8 days blocking) → US1 (10 days) with US2/US3 parallelization after Phase 2 handoff → Phase 6 validation (5 days final).

**Recommended Action**: Proceed to Phase 1 execution. Assign staffing per the Staffing Model (7 engineers for Phase 1, 2 for Phase 2, incremental for Phases 3–6). Validate Phase 1 handoff gate before Phase 2 begins. Execute Phase 6 validation in parallel with Phase 5 if schedule and staffing permit.
