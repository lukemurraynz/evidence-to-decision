# Service Bus dead-letter and replay runbook

## Ownership and alert

The application operations owner handles queue age, processor failures, and
dead-letter growth. Alert when ordinary work approaches two minutes or any
dead-letter count is nonzero.

## Retry behavior

Recommendation work is retried five times after 10, 30, 60, 120, and 240
seconds, then dead-lettered. Graph consumers use per-workspace sessions,
single-call ordering, a maximum delivery count of five, and atomic
`workspaceId/eventId/consumerName` claims.

## Replay

1. Record the topic, subscription or queue, workspace, message IDs, time range,
   and incident correlation ID.
2. Diagnose and fix the consumer before replay.
3. Verify the canonical graph is healthy. Events are triggers, not authority.
4. Preview the selected dead-letter messages and exclude malformed or
   unauthorized workspace payloads.
5. Obtain operator approval for the bounded replay.
6. Resubmit with the original event ID, workspace session ID, schema version,
   graph version, and correlation ID.
7. Confirm the consumer rereads current canonical state.
8. Confirm completed idempotency claims prevent duplicate side effects.
9. Record replay counts, skipped duplicates, failures, and resulting projection
   graph versions.

The Cosmos event outbox is the 30-day replay source. Service Bus entity TTL is
14 days; do not rely on the broker as the full replay archive.
