# Cosmos DB restore and deletion runbook

## Objectives

- Recovery point objective: 15 minutes
- Recovery time objective: 4 hours
- Backup mode: continuous, 30-day restore window
- Operational retention baseline: 90 days, subject to approved workspace policy
- Restore drill frequency: at least annually

## Restore procedure

1. Open an incident and record the affected workspace, engagement, time range,
   and approved recovery point.
2. Stop writes for the affected workspace or place the API behind an
   operator-controlled maintenance boundary.
3. Use the Azure portal or current Cosmos DB restore command to preview a restore
   into a new account. Never overwrite the source account.
4. Restore to the latest approved point before corruption or deletion.
5. Validate `/workspaceId` partitioning, document counts, canonical graph
   versions, append-only audits, outbox records, and projection derivation.
6. Reconcile events by event ID and canonical graph version. Consumers must
   reread canonical state and retain idempotency claims.
7. Obtain human approval before switching application configuration to the
   recovered account.
8. Re-run workspace-denial, provenance, decision-gate, event-replay, and artifact
   staleness tests.
9. Record achieved recovery point and duration against the RPO and RTO.

## Deletion

Engagement deletion is an admin-only API operation requiring the current graph
ETag and exact `DELETE <engagement-id>` confirmation. The transactional delete
retains a deletion event and durable audit record. Projections become
non-authoritative and inaccessible when their canonical engagement is absent.

Workspace retention automation is blocked until the customer-approved
classification and retention schedule are supplied. Do not infer or silently
apply a deletion schedule.
