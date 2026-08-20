# Contract: Review and Trust/Readiness Gates

## Review decision

A review records `reviewer`, `role`, `decision`, `rationale`, `blockers`, `evidenceReferences`, `approvalPoint`, `timestamp`, and `canonicalGraphVersion`.

Allowed decisions include `validate`, `pilot`, `production_ready`, `prerequisites_required`, `reject`, and `park`.

## Gate behavior

- Pilot or production progression requires an accountable owner, KPI/baseline/target, and applicable trust/readiness controls.
- Missing or failed privacy, security, governance, data, integration, or oversight controls produce a durable blocker.
- A reviewer may override a recommendation only through an auditable decision record with rationale and role authorization.
- Gate evaluation is non-destructive and repeatable against a specified graph version.
