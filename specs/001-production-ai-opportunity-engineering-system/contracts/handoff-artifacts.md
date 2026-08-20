# Contract: Delivery Handoff Artifacts

## Artifact envelope

- `artifactId`, `artifactType`, `engagementId`, `opportunityId`
- `sourceCanonicalGraphVersion`
- `generatedAt`, `generatedBy`, `status`
- `staleness`: current | stale | unavailable
- `content`

## Required content

Pilot briefs and architecture handoffs include the problem, workflow, users, desired outcome, KPI, baseline, target, concept, scope, trust profile, autonomy, dependencies, assumptions, owner, decision, and rationale.

## Rules

- Generation is read-only with respect to the canonical graph.
- An artifact is reproducible from its source graph version, method version, and referenced card/source versions.
- Viewing a stale artifact must make its source version and current-version difference visible.
- Customer evidence remains within the workspace authorization boundary.
- Portfolio analytics projections are derived artifacts and must include source window/version metadata.
- Fabric semantic models and Data Agent experiences may consume only approved derived datasets.
- Data Agent interactions are read-only and cannot mutate canonical records, alter lifecycle state, or bypass trust/readiness gates.
