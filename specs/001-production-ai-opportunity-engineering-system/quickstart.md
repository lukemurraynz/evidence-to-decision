# Planning Quickstart and Validation Scenarios

This is a specification validation flow, not an application startup guide. It defines the smallest production-safe journey that implementation must make executable.

## Preconditions

- A customer workspace exists with an owner, governance owner, retention policy, and role assignments.
- A reusable card library is available independently from engagement data.
- An engagement has a method version and at least one facilitator.
- The operator has validated the target tenant/subscription and configured Entra app, role-group object IDs, and workspace memberships.
- The local deployment has been previewed and applied through the ordered `azd` flow described in `contracts/deployment.md`.
- Infrastructure, agent configuration, application health, and managed-identity access checks have passed before scenario execution.

## Local deployment validation

1. Preview the infrastructure and application changes; do not apply an unreviewed production write.
2. Enforce one active deployment per environment using an environment-scoped lock.
3. Verify destructive teardown safeguards: before any `azd down --purge` operation, confirm tenant, subscription, resource group, and typed environment-name confirmation.
4. Validate the Entra app registration, facilitator/reviewer/admin group object IDs, and workspace membership mappings.
5. Provision or validate Cosmos DB, Service Bus, Container Apps, managed identities, role assignments, diagnostics, and approved secret references.
6. Provision or validate Microsoft Foundry/Agent Framework project, model/deployment, tool permissions, structured-output, multimodal, retention, and human-approval configuration.
7. Deploy the ASP.NET Core application and verify controlled HTTPS ingress, Entra authentication, health checks, audit logging, and correlation IDs.
8. Record resource identifiers, configuration versions, validation results, and any implementation exceptions requiring follow-up.

**Deployment pass condition**: identity, infrastructure, agents, and application are independently healthy and the end-to-end US1 flow is ready to run. CI/CD is not part of this validation path.

## US1: Evidence to decision

1. Create an engagement in a customer workspace.
2. Capture a participant statement and a measured observation with source, timestamp, participant, modality, and validation status.
3. Map the evidence to a workflow and problem.
4. Create an opportunity with an owner, desired outcome, KPI candidate, trust profile, and readiness profile.
5. Request a recommendation or comparison.
6. Verify the response cites evidence IDs, fit dimensions, unknowns, limitations, and required human review.
7. Add two contradictory claims about the same baseline.
8. Verify both claims remain visible, the opportunity confidence is downgraded or gated, and a validation action is proposed.
9. Record a human decision with rationale, approval point, dissent, and affected assumptions.

**Pass condition**: the full evidence-to-decision chain is traceable without relying on hidden conversation context.

## US2: Asynchronous governance review

1. Open an opportunity decision view as an authorized reviewer who did not attend the workshop.
2. Verify value, confidence, evidence, trust, readiness, owner, rationale, blockers, and current graph version are visible.
3. Remove or invalidate a required owner, KPI, privacy, security, or governance control.
4. Attempt progression to pilot or production readiness.
5. Verify progression is blocked and a durable blocker record identifies the missing control, evaluator, timestamp, and remediation path.
6. Record an approve, reject, validate, or prerequisite-required decision.

**Pass condition**: a reviewer can make an informed, auditable decision asynchronously and required controls cannot be bypassed.

## US3: Delivery handoff

1. Move an opportunity through an approved validation decision.
2. Generate a pilot brief, decision record, executive summary, or architecture handoff.
3. Verify the artifact includes problem, workflow, users, outcome, KPI/baseline/target, concept, trust, autonomy, dependencies, assumptions, owner, and decision rationale.
4. Change the canonical graph.
5. Verify a regenerated view reflects the new version and a previously exported artifact is marked with its source graph version and staleness state.

**Pass condition**: delivery can consume the handoff without recreating already-captured discovery context.

## Multimodal evidence checks

- A transcript record must retain speaker attribution, timestamps, source asset, extraction confidence, and human correction status before it contributes to a consequential recommendation.
- A visual artifact must retain its source, capture time, engagement, modality, and extraction/validation state.
- Low-confidence or ambiguous extraction must remain visibly unvalidated and must not silently become fact.

## Event adapter checks

- A canonical graph change emits a versioned event with event ID, aggregate/object ID, graph version, event type, actor, timestamp, and correlation ID.
- Replaying the same event does not duplicate a downstream action.
- A consumer rereads canonical state before acting and records its result.
- A failed or delayed consumer does not alter canonical graph truth or hide the completed domain change.

## Asynchronous operation-status contract checks (SC-007, T039)

1. Submit one agentic workflow request and one multimodal processing request that complete asynchronously.
2. Verify each initial response returns a durable operation identifier and a status endpoint reference.
3. Poll each status endpoint using retry guidance until terminal state is reached.
4. Verify lifecycle progression is observable (`queued`/`running`/`succeeded|failed|canceled`) with timestamps and correlation ID continuity.
5. Repeat one submission with an idempotency key and confirm duplicate processing does not occur.

**Pass condition**: 100% of tested asynchronous operations are traceable from submission to terminal state with operation ID and correlation metadata.

## Policy decision audit-evidence checks (SC-008, T039)

1. Trigger at least one allowed tool call, one denied or escalated tool call, and one consequential workflow evaluation requiring human review.
2. Verify each decision writes a durable audit record to the external append-only sink.
3. Confirm each audit record contains actor identity, workspace ID, policy/version reference, verdict, reason, timestamp, and correlation ID.
4. Verify local process logs are supplemental and not the only evidence source.
5. Validate retrieval of audit records for incident/compliance review without relying on ephemeral runtime logs.

**Pass condition**: 100% of tested policy/tool-call/consequential decisions are present in durable audit evidence with complete required fields.

## Derived portfolio analytics checks (SC-009, T042)

1. Generate a derived analytics projection from canonical records/events for a defined source window.
2. Verify metrics include engagement type demand, technology preference frequency, blocker-category distribution, and stage progression outcomes.
3. Verify each metric is reproducible from source version metadata (canonical graph/event versions and time window).
4. Validate that changing canonical inputs results in a new projection version while preserving historical projection reproducibility.

**Pass condition**: portfolio metrics are reproducible, versioned, and demonstrably derived from canonical sources.

## Fabric Data Agent governance checks (T043)

1. Query approved analytics datasets using Data Agent-style natural-language prompts.
2. Verify responses are scoped to authorized workspace/aggregation boundaries.
3. Attempt write-like prompts (update decision, approve progression, change blocker status) and verify they are denied.
4. Verify audit records capture query actor, scope, timestamp, and correlation metadata for governed analytics access.

**Pass condition**: Data Agent behavior is read-only, scope-governed, and non-authoritative relative to canonical decision workflows.

## Fabric/Data Agent readiness gate (T044)

1. Verify target Fabric capacity and workload readiness meet the minimum approved profile for Data Agent usage.
2. Verify required tenant settings for AI/data-agent processing are enabled in the target tenant and documented in deployment evidence.
3. Verify workspace/OneLake security posture is configured for least privilege and governed analytics access.
4. If any prerequisite fails, block Data Agent enablement and record remediation ownership.

**Pass condition**: Data Agent is enabled only when capacity, tenant settings, and security prerequisites are demonstrably satisfied.

## Portfolio semantic query-mode trust checks (T045)

1. Execute representative portfolio analytics queries and capture query-mode evidence for the semantic model path.
2. Where Direct Lake is used, verify and record whether queries run in expected mode or fallback mode.
3. Confirm metric outputs remain reproducible from projection source versions regardless of query-mode behavior.
4. Record any fallback or mode-shift observations as trust caveats in validation evidence.

**Pass condition**: Portfolio analytics include explicit query-mode evidence and no unqualified metric claims are published without trust caveats.
