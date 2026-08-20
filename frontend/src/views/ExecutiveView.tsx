import type { Engagement } from '../api/contracts'
import { effectiveBlockerCount } from '../app/blockers'
import { lifecycleLabel } from '../app/labels'
import { EmptyState } from '../components/AsyncStates'

export function ExecutiveView({
  engagement,
}: {
  readonly engagement: Engagement
}) {
  const validatedEvidence = engagement.evidence.filter(
    (evidence) => evidence.validationStatus === 2,
  ).length
  const openBlockers = engagement.opportunities.reduce(
    (sum, opportunity) =>
      sum + effectiveBlockerCount(opportunity, engagement.blockers),
    0,
  )

  return (
    <section className="page executive-page">
      <header className="executive-header">
        <div>
          <p className="eyebrow">Executive summary</p>
          <h1>Review outcomes and readiness</h1>
        </div>
        <p>
          This summary reflects the latest engagement record. Conclusions remain
          tied to their supporting evidence and recorded decisions.
        </p>
      </header>
      <p className="origin-label">Current engagement record · version {engagement.objectVersion}</p>
      {openBlockers > 0 && (
        <div className="blocked-banner" role="status">
          <strong>Delivery readiness is blocked.</strong>
          <span>
            {openBlockers} open {openBlockers === 1 ? 'item requires' : 'items require'} resolution before production readiness.
          </span>
        </div>
      )}

      <dl className="outcome-strip" aria-label="Engagement summary">
        <div>
          <dt>Current status</dt>
          <dd>{lifecycleLabel(engagement.lifecycleState)}</dd>
        </div>
        <div>
          <dt>Opportunities</dt>
          <dd>{engagement.opportunities.length}</dd>
        </div>
        <div>
          <dt>Validated evidence</dt>
          <dd>{validatedEvidence}</dd>
        </div>
        <div>
          <dt>Open blockers</dt>
          <dd>{openBlockers}</dd>
        </div>
      </dl>

      <section className="executive-objective" aria-labelledby="objective-heading">
        <div>
          <p className="eyebrow">Workshop intent</p>
          <h2 id="objective-heading">Objectives</h2>
        </div>
        {engagement.objectives.length === 0 ? (
          <p>No objectives are recorded.</p>
        ) : (
          <ul>
            {engagement.objectives.map((objective) => (
              <li key={objective}>{objective}</li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="outcome-heading">
        <div className="section-heading">
          <div>
            <p className="eyebrow">Decisions and outcomes</p>
            <h2 id="outcome-heading">Opportunity outcomes</h2>
          </div>
          <span>{engagement.opportunities.length} total</span>
        </div>
        {engagement.opportunities.length === 0 ? (
          <EmptyState
            title="No outcomes are available"
            message="Outcomes appear after an opportunity is supported by workshop evidence."
          />
        ) : (
          <div className="outcome-list">
            {engagement.opportunities.map((opportunity) => {
              const decision = [...engagement.decisions]
                .reverse()
                .find((item) => item.opportunityId === opportunity.id)
              const blockerCount = effectiveBlockerCount(
                opportunity,
                engagement.blockers,
              )
              return (
                <article key={opportunity.id}>
                  <div>
                    <p className="origin-label">Workshop record · user supplied</p>
                    <p className="status-line">
                      {lifecycleLabel(opportunity.lifecycleState)}
                    </p>
                    <h3>{opportunity.desiredOutcome}</h3>
                    <p>{opportunity.valueProfile}</p>
                  </div>
                  <dl>
                    <div>
                      <dt>Owner</dt>
                      <dd>{opportunity.owner}</dd>
                    </div>
                    <div>
                      <dt>Confidence</dt>
                      <dd>{opportunity.confidenceProfile}</dd>
                    </div>
                    <div>
                      <dt>Decision</dt>
                      <dd>
                        {decision === undefined
                          ? 'Awaiting review'
                          : `Recorded decision: ${decision.rationale}`}
                      </dd>
                    </div>
                    <div>
                      <dt>Blockers</dt>
                      <dd>{blockerCount}</dd>
                    </div>
                  </dl>
                </article>
              )
            })}
          </div>
        )}
      </section>
    </section>
  )
}
