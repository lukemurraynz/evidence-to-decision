# Contract: Agent Framework Boundaries and Guardrails

## Scope

This contract defines what Microsoft Agent Framework agents can and cannot do within the Opportunity Engineering System. It ensures agents remain assistive, non-authoritative, and transparent to human decision-makers.

## Agent roles

Agents serve three scopes:

1. **Facilitator Agent** (US1): Assists during evidence capture, synthesis, and recommendation. Summaries and comparisons are non-binding.
2. **Policy/Readiness Evaluator** (US2): Readonly evaluation of trust/readiness predicates and gate blockers. Proposes remediation but cannot override gates.
3. **Notifier and Adapter** (US2/US3): Emits events on graph changes and notifies reviewers. Consumes events and triggers re-evaluation without mutating canonical state.

## Permitted agent operations

Agents MAY:

- **Read canonical context**: Retrieve evidence, problems, opportunities, concepts, assumptions, workflows, trust/readiness profiles, and gate states.
- **Synthesize and summarize**: Combine evidence into briefings, comparisons, risk/readiness assessments, and recommendation rationales without claiming authority.
- **Generate recommendations**: Propose fit dimensions, cite evidence references (IDs, not wording), express uncertainty/confidence, and recommend abstention or human review.
- **Capture structured evidence**: Record participant statements, measured observations, and multimodal inputs with source/timestamp/modality/confidence metadata.
- **Invoke transcription and vision tools**: Convert speech to text, extract structured data from visuals, and report extraction confidence.
- **Propose validation actions**: Suggest human review, conflict resolution, or remediation when evidence contradicts or confidence is low.
- **Evaluate against gate predicates**: Readonly assessment of whether trust/readiness/governance gates can be satisfied. Propose blockers and remediation paths.
- **Generate audit/correlation records**: Log agent activity with user identity, action, timestamp, correlation ID, and result for human review.

## Forbidden agent operations

Agents MUST NOT:

- **Mutate canonical graph state**: Cannot write engagement, evidence, opportunity, concept, assumption, workflow, problem, or decision records. All mutations require auditable domain operations by humans.
- **Make consequential decisions**: Cannot approve, gate-override, or progress opportunities to pilot/production. Human approval is mandatory.
- **Bypass authorization**: Cannot access workspaces the authenticated user does not have permission to access. Cannot perform actions the user's role does not permit.
- **Silence evidence or uncertainty**: Cannot hide conflicting claims, downgrade below-threshold confidence without human correction, or merge assumptions into observations.
- **Apply workspace retention/redaction policy**: Policy is workspace-owned. Agents reference the policy but do not enforce deletion or redaction without explicit human authorization and audit.
- **Overwrite human corrections**: If a human corrects a transcript or visual extraction, agents must use the corrected version, not the original extraction.
- **Act on non-authoritative events**: Agents consume events, reread canonical state, and record their result. They do not allow event processing to replace canonical source-of-truth.
- **Access or modify other workspaces**: Agents are strictly workspace-scoped.

## Structured outputs

Agent-generated content must explicitly signal:

- **Evidence citations**: Include evidence ID (never full wording without ID); source modality, confidence, and validation status.
- **Rationale transparency**: Explain fit dimensions, cite applicable KPIs or trust/readiness predicates, and acknowledge unknowns.
- **Uncertainty levels**: Express confidence ranges or abstention ("Insufficient evidence to recommend"; "This claim conflicts with recorded baseline").
- **Limitations and caveats**: Include domain assumptions, temporal validity, applicability constraints.
- **Required human review**: Always indicate where human judgment or approval is mandatory before consequential action.
- **Multimodal confidence**: Report extraction confidence for transcripts and visual structured outputs; below-0.80 confidence must be visibly flagged as unvalidated.

## Quality gates

- **Transcript confidence below 0.80**: Human correction and approval required before the transcript can be used in recommendations or decisions.
- **Visual extraction confidence below 0.80**: Human review and confirmation required; marked as "needs validation" until corrected.
- **Recommendation on conflicting evidence**: Agents must propose human resolution; cannot silently pick one claim over another.
- **Gate evaluation on missing data**: If trust/readiness/governance predicates cannot be evaluated, agents must report missing prerequisites, not guess.

## Authorization boundaries

- Agents inherit the authenticated user's authorization context. If a human user cannot access a workspace, the agent cannot access it either.
- Agents respect role-based actions: facilitator agents do not approve; reviewer agents do not mutate the canonical graph.
- All agent actions are auditable with user identity, action type, timestamp, workspace, and correlation ID.

## Event consumption semantics

- Agents consume versioned graph-change events.
- Before acting on an event, agents MUST reread canonical state (to handle replays and ensure consistency).
- Agents record their re-evaluation result in an audit/correlation record.
- Agents do not mutate canonical state as a result of event processing.
- Failed or delayed consumers do not alter canonical graph truth.

## Deployment and configuration

- Agent instructions, system prompts, tool bindings, and model/deployment selection are locked down at deployment time.
- Agents use managed identity or workload identity for Azure resource access.
- All agent tool invocations are logged and traceable.
- Agent-generated content is tagged with agent identity, model, and version for reproducibility and auditing.

## Runtime governance enforcement (fail-closed)

- Policy and guardrail bundles MUST load successfully at startup; missing, invalid, unreadable, or empty policy sets fail startup and block traffic.
- Runtime policy-evaluation failures MUST return deny/escalate behavior and MUST NOT fall back to default-allow execution.
- Enforcement points for input, model calls, tool calls, and output checks must produce explicit verdicts (allow, warn, deny, escalate, transform).
- All policy/runtime failures include correlation metadata and are routed to operational alerting.

## Policy lifecycle and promotion

- New or changed guardrail policies start in **evaluation-only** mode for a bounded validation period.
- Promotion from evaluation-only to **enforce** mode requires measured verdict telemetry and explicit operator approval.
- Policy version, effective date, approver, and rollback reference must be recorded in an auditable change log.

## Audit durability requirements

- Agent policy verdicts and tool-call decisions must be persisted to an external durable append-only audit sink.
- Local process logs are supplemental and cannot be the sole compliance evidence source.
- Audit records for denied/escalated decisions include actor, workspace, policy version, verdict reason, timestamp, and correlation ID.
