import type { GovernanceBlocker, Opportunity } from '../api/contracts'

/**
 * Missing evidence blocks pilot/production decisions (see ReviewView) but was
 * previously invisible in every "blockers" count, reading as "0 blockers"
 * while the decision was actively gated. This folds that gate into the count.
 */
export function effectiveBlockerCount(
  opportunity: Pick<Opportunity, 'id' | 'evidenceReferences'>,
  blockers: readonly GovernanceBlocker[],
): number {
  const explicit = blockers.filter(
    (blocker) => blocker.opportunityId === opportunity.id,
  ).length
  const evidenceGap = opportunity.evidenceReferences.length === 0 ? 1 : 0
  return explicit + evidenceGap
}
