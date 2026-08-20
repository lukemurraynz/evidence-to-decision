import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
} from 'react'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import {
  DecisionClass,
  EngagementLifecycle,
  type DecisionClassValue,
  type Engagement,
  type EngagementLifecycleValue,
  type Opportunity,
  type OpportunityReview,
} from '../api/contracts'
import { dateTimeLabel, lifecycleLabel } from '../app/labels'
import { navigateTo } from '../app/routing'
import { EmptyState, PageLoading } from '../components/AsyncStates'

type ReviewState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly review: OpportunityReview }
  | {
      readonly status: 'error'
      readonly message: string
      readonly retryable: boolean
    }

type ActionState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving'; readonly message: string }
  | { readonly status: 'success'; readonly message: string }
  | {
      readonly status: 'error'
      readonly message: string
      readonly fields: readonly string[]
      readonly conflict: boolean
    }

type DecisionChoice = {
  readonly label: string
  readonly decisionClass: DecisionClassValue
  readonly newState: (current: EngagementLifecycleValue) => EngagementLifecycleValue
}

const decisionChoices = {
  validate: {
    label: 'Continue validation',
    decisionClass: DecisionClass.Validate,
    newState: () => EngagementLifecycle.Validation,
  },
  pilot: {
    label: 'Approve pilot',
    decisionClass: DecisionClass.Pilot,
    newState: () => EngagementLifecycle.Pilot,
  },
  production: {
    label: 'Confirm production readiness',
    decisionClass: DecisionClass.ProductionReady,
    newState: () => EngagementLifecycle.ProductionReadiness,
  },
  prerequisites: {
    label: 'Require prerequisites',
    decisionClass: DecisionClass.PrerequisitesRequired,
    newState: (current) => current,
  },
  reject: {
    label: 'Do not proceed',
    decisionClass: DecisionClass.Reject,
    newState: () => EngagementLifecycle.Rejected,
  },
  park: {
    label: 'Park for later',
    decisionClass: DecisionClass.Park,
    newState: () => EngagementLifecycle.Parked,
  },
} satisfies Record<string, DecisionChoice>

type DecisionChoiceKey = keyof typeof decisionChoices

function isDecisionChoiceKey(value: string): value is DecisionChoiceKey {
  return Object.hasOwn(decisionChoices, value)
}

type ReviewViewProps = {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly etag: string | null
  readonly isOnline: boolean
  readonly onUpdated: (engagement: Engagement, etag: string | null) => void
}

export function ReviewView({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
}: ReviewViewProps) {
  const [selectedId, setSelectedId] = useState(
    engagement.opportunities[0]?.id ?? '',
  )
  const selectedOpportunity = engagement.opportunities.find(
    (opportunity) => opportunity.id === selectedId,
  )
  const [reviewState, setReviewState] = useState<ReviewState>({
    status: 'loading',
  })
  const [reviewAttempt, setReviewAttempt] = useState(0)

  useEffect(() => {
    if (selectedId === '') return
    const controller = new AbortController()
    setReviewState((current) =>
      current.status === 'ready' &&
      current.review.opportunityId === selectedId
        ? current
        : { status: 'loading' },
    )
    void client
      .getOpportunityReview(
        workspaceId,
        engagement.id,
        selectedId,
        controller.signal,
      )
      .then((result) => {
        setReviewState({ status: 'ready', review: result.data })
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        const retryable =
          !(error instanceof ApiRequestError) ||
          ![401, 403, 404, 422].includes(error.status)
        setReviewState({
          status: 'error',
          message:
            error instanceof ApiRequestError && error.status === 403
              ? 'You can open the engagement, but this review requires additional access. Ask the engagement owner for decision-review access.'
              : error instanceof ApiRequestError
              ? error.message
              : 'The review could not be loaded. Check the connection and try again.',
          retryable,
        })
      })
    return () => controller.abort()
  }, [
    client,
    engagement.id,
    engagement.objectVersion,
    reviewAttempt,
    selectedId,
    workspaceId,
  ])

  if (selectedOpportunity === undefined) {
    return (
      <section className="page">
        <header className="page-header">
          <div>
            <p className="eyebrow">Decision review</p>
            <h1>Review an opportunity</h1>
          </div>
        </header>
        <EmptyState
          title="No opportunities are ready for review"
          message="Capture and frame an opportunity before starting a decision review."
        />
      </section>
    )
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Decision review</p>
          <h1>Record an accountable decision</h1>
          <p>
            Compare the claimed value with evidence, controls, and delivery
            readiness before changing its status.
          </p>
        </div>
        {engagement.opportunities.length > 1 && (
          <div className="opportunity-picker">
            <label htmlFor="opportunity-select">Opportunity</label>
            <select
              id="opportunity-select"
              value={selectedId}
              onChange={(event) => setSelectedId(event.target.value)}
            >
              {engagement.opportunities.map((opportunity) => (
                <option key={opportunity.id} value={opportunity.id}>
                  {opportunity.desiredOutcome}
                </option>
              ))}
            </select>
          </div>
        )}
      </header>

      {reviewState.status === 'loading' && (
        <PageLoading label="Loading the decision review" />
      )}
      {reviewState.status === 'error' && (
        <div className="inline-error" role="alert">
          <h2>Decision review unavailable</h2>
          <p>{reviewState.message}</p>
          {reviewState.retryable && (
            <button
              type="button"
              onClick={() => setReviewAttempt((attempt) => attempt + 1)}
            >
              Try loading the review again
            </button>
          )}
        </div>
      )}
      {reviewState.status === 'ready' && (
        <ReviewWorkspace
          client={client}
          workspaceId={workspaceId}
          engagement={engagement}
          opportunity={selectedOpportunity}
          review={reviewState.review}
          etag={etag}
          isOnline={isOnline}
          onUpdated={onUpdated}
        />
      )}
    </section>
  )
}

function ReviewWorkspace({
  client,
  workspaceId,
  engagement,
  opportunity,
  review,
  etag,
  isOnline,
  onUpdated,
}: {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly opportunity: Opportunity
  readonly review: OpportunityReview
  readonly etag: string | null
  readonly isOnline: boolean
  readonly onUpdated: (engagement: Engagement, etag: string | null) => void
}) {
  const [choice, setChoice] = useState<DecisionChoiceKey>('validate')
  const [rationale, setRationale] = useState('')
  const [owner, setOwner] = useState(review.owner)
  const [approvalPoint, setApprovalPoint] = useState('')
  const [escalationPath, setEscalationPath] = useState('')
  const [action, setAction] = useState<ActionState>({ status: 'idle' })
  const errorSummaryRef = useRef<HTMLDivElement | null>(null)
  const recommendationKeyRef = useRef<string | null>(null)
  const isReviewStale =
    review.canonicalGraphVersion !== engagement.objectVersion
  const hasDecisionBlockers =
    review.blockers.length > 0 || review.evidenceReferences.length === 0

  const recordDecision = async (
    event: FormEvent<HTMLFormElement>,
  ): Promise<void> => {
    event.preventDefault()
    const missingFields = [
      rationale.trim() === '' ? 'decision-rationale' : null,
      owner.trim() === '' ? 'decision-owner' : null,
      approvalPoint.trim() === '' ? 'approval-point' : null,
      escalationPath.trim() === '' ? 'escalation-path' : null,
    ].filter((field): field is string => field !== null)

    if (missingFields.length > 0) {
      setAction({
        status: 'error',
        message: 'Check the highlighted decision fields.',
        fields: missingFields,
        conflict: false,
      })
      requestAnimationFrame(() => errorSummaryRef.current?.focus())
      return
    }
    if (etag === null) {
      setAction({
        status: 'error',
        message:
          'This engagement could not be verified. Reload before saving a decision.',
        fields: [],
        conflict: true,
      })
      requestAnimationFrame(() => errorSummaryRef.current?.focus())
      return
    }
    if (isReviewStale) {
      setAction({
        status: 'error',
        message:
          'The evidence record changed after this review loaded. Your edits are still here. Refresh before saving.',
        fields: [],
        conflict: true,
      })
      requestAnimationFrame(() => errorSummaryRef.current?.focus())
      return
    }
    const selectedChoice = decisionChoices[choice]
    setAction({ status: 'saving', message: 'Saving decision…' })
    try {
      const result = await client.recordDecision(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          opportunityId: opportunity.id,
          previousState: opportunity.lifecycleState,
          newState: selectedChoice.newState(opportunity.lifecycleState),
          decisionClass: selectedChoice.decisionClass,
          rationale: rationale.trim(),
          evidenceReferences: review.evidenceReferences,
          dissent: [],
          owner: owner.trim(),
          approvalPoint: approvalPoint.trim(),
          escalationPath: escalationPath.trim(),
          timestamp: new Date().toISOString(),
          affectedAssumptions: [],
          objectVersion: engagement.objectVersion,
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setAction({ status: 'success', message: 'Decision saved.' })
    } catch (error: unknown) {
      const conflict =
        error instanceof ApiRequestError &&
        (error.status === 409 || error.status === 412)
      setAction({
        status: 'error',
        message:
          conflict
            ? 'The engagement changed before this decision was saved. Your edits are still here. Refresh the record, review your decision, then save again.'
            : error instanceof ApiRequestError
            ? error.message
            : 'The decision could not be saved. Check the connection and try again.',
        fields: [],
        conflict,
      })
      requestAnimationFrame(() => errorSummaryRef.current?.focus())
    }
  }

  const refreshRecord = async (): Promise<void> => {
    setAction({ status: 'saving', message: 'Refreshing the record…' })
    try {
      const result = await client.getEngagement(workspaceId, engagement.id)
      onUpdated(result.data, result.etag)
      setAction({
        status: 'success',
        message: 'Record refreshed. Your decision edits are still here.',
      })
    } catch (error: unknown) {
      setAction({
        status: 'error',
        message:
          error instanceof ApiRequestError
            ? error.message
            : 'The engagement could not be refreshed. Keep this tab open and try again.',
        fields: [],
        conflict: true,
      })
    }
  }

  const prepareRecommendation = async (): Promise<void> => {
    setAction({ status: 'saving', message: 'Starting the review brief…' })
    // Generate the key once per logical submission and reuse it across retries
    // so a lost success response does not queue a duplicate Foundry operation.
    recommendationKeyRef.current ??= crypto.randomUUID()
    try {
      const result = await client.requestRecommendation(
        workspaceId,
        engagement.id,
        opportunity.id,
        recommendationKeyRef.current,
      )
      recommendationKeyRef.current = null
      navigateTo(
        `/progress/${encodeURIComponent(result.data.id)}?opportunity=${encodeURIComponent(opportunity.id)}`,
      )
    } catch (error: unknown) {
      setAction({
        status: 'error',
        message:
          error instanceof ApiRequestError
            ? error.message
            : 'The review brief could not be started. Try again.',
        fields: [],
        conflict: false,
      })
    }
  }

  const trustChecks = [
    ['Privacy approved', review.trust.privacyApproved],
    ['Security approved', review.trust.securityApproved],
    ['Governance approved', review.trust.governanceApproved],
    ['Human oversight defined', review.trust.humanOversightDefined],
  ] as const
  const readinessChecks = [
    ['Owner defined', review.readiness.ownerDefined],
    ['KPI defined', review.readiness.kpiDefined],
    ['Baseline defined', review.readiness.baselineDefined],
    ['Target defined', review.readiness.targetDefined],
    ['Data ready', review.readiness.dataReady],
    ['Integration ready', review.readiness.integrationReady],
  ] as const
  const supportingEvidence = review.evidenceReferences.map((reference) => ({
    reference,
    evidence: engagement.evidence.find((item) => item.id === reference),
  }))
  const fieldHasError = (id: string): boolean =>
    action.status === 'error' && action.fields.includes(id)

  return (
    <div className="review-grid">
      <div className="review-brief record-content" data-content-origin="user">
        <p className="origin-label">Workshop record · user supplied</p>
        {isReviewStale && (
          <div className="stale-banner" role="status">
            <strong>This review is out of date.</strong>
            <span>
              It uses record version {review.canonicalGraphVersion}; the current
              record is version {engagement.objectVersion}. Refresh before making a
              decision.
            </span>
            <button
              type="button"
              className="button-secondary"
              onClick={() => void refreshRecord()}
              disabled={!isOnline || action.status === 'saving'}
            >
              Refresh review and keep edits
            </button>
          </div>
        )}
        {hasDecisionBlockers && (
          <div className="blocked-banner" role="status">
            <strong>Pilot and production decisions are blocked.</strong>
            <span>
              Resolve the open evidence and governance items below. A decision
              reviewer may still continue validation, require prerequisites, pause,
              or stop the opportunity.
            </span>
          </div>
        )}
        <section>
          <p className="eyebrow">Opportunity</p>
          <h2>{opportunity.desiredOutcome}</h2>
          <dl className="brief-facts">
            <div>
              <dt>Value</dt>
              <dd>{review.value}</dd>
            </div>
            <div>
              <dt>Confidence</dt>
              <dd>{review.confidence}</dd>
            </div>
            <div>
              <dt>Current status</dt>
              <dd>{lifecycleLabel(opportunity.lifecycleState)}</dd>
            </div>
            <div>
              <dt>Accountable owner</dt>
              <dd>{review.owner}</dd>
            </div>
          </dl>
          <p className="record-freshness">
            Review based on record version {review.canonicalGraphVersion} ·{' '}
            {review.evidenceReferences.length} evidence references
          </p>
        </section>

        <section aria-labelledby="supporting-evidence-heading">
          <h3 id="supporting-evidence-heading">Evidence used</h3>
          {supportingEvidence.length === 0 ? (
            <p>
              No evidence is attached to this review. Do not approve pilot or
              production until supporting evidence is linked.
            </p>
          ) : (
            <ul className="review-evidence-list">
              {supportingEvidence.map(({ reference, evidence }) => (
                <li key={reference}>
                  {evidence === undefined ? (
                    <p>Evidence reference {reference} is not in the current record.</p>
                  ) : (
                    <>
                      <blockquote>{evidence.statement}</blockquote>
                      <p>
                        {evidence.sourceReference} · captured{' '}
                        {dateTimeLabel(evidence.capturedAt)} · confidence{' '}
                        {Math.round(evidence.confidence * 100)}%
                      </p>
                    </>
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>

        <section>
          <h3>Trust checks</h3>
          <ul className="check-list">
            {trustChecks.map(([label, passed]) => (
              <li key={label} className={passed ? 'is-passed' : 'is-open'}>
                <span aria-hidden="true">{passed ? '✓' : '○'}</span>
                {label}: {passed ? 'complete' : 'open'}
              </li>
            ))}
          </ul>
        </section>

        <section>
          <h3>Delivery readiness</h3>
          <ul className="check-list">
            {readinessChecks.map(([label, passed]) => (
              <li key={label} className={passed ? 'is-passed' : 'is-open'}>
                <span aria-hidden="true">{passed ? '✓' : '○'}</span>
                {label}: {passed ? 'complete' : 'open'}
              </li>
            ))}
          </ul>
        </section>

        <section>
          <h3>Blockers</h3>
          {!hasDecisionBlockers ? (
            <p>No governance blockers are recorded.</p>
          ) : (
            <ul className="blocker-list">
              {review.evidenceReferences.length === 0 && (
                <li>
                  <strong>No supporting evidence linked</strong>
                  <span>
                    Attach evidence to this opportunity before approving pilot
                    or production.
                  </span>
                </li>
              )}
              {review.blockers.map((blocker) => (
                <li key={blocker.id}>
                  <strong>{blocker.rationale}</strong>
                  <span>{blocker.remediationPath}</span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <button
          type="button"
          className="button-secondary"
          onClick={() => void prepareRecommendation()}
          disabled={
            !isOnline || action.status === 'saving' || isReviewStale
          }
        >
          Prepare review brief
        </button>
        <p className="disclosure">
          AI-assisted. The brief summarizes the current record and cannot approve
          or change the opportunity. A decision reviewer must assess the evidence
          and record the decision.
        </p>
      </div>

      <section className="decision-pane" aria-labelledby="decision-heading">
        <p className="eyebrow">Decision record</p>
        <h2 id="decision-heading">What happens next?</h2>
        <form onSubmit={(event) => void recordDecision(event)} noValidate>
          {action.status === 'error' && (
            <div
              className="form-error-summary"
              ref={errorSummaryRef}
              tabIndex={-1}
              role="alert"
            >
              <h3>Decision not saved</h3>
              <p>{action.message}</p>
              {action.fields.length > 0 && (
                <ul>
                  {action.fields.map((field) => (
                    <li key={field}>
                      <a href={`#${field}`}>
                        {field === 'decision-rationale'
                          ? 'Add a decision rationale'
                          : field === 'decision-owner'
                            ? 'Add an accountable owner'
                            : field === 'approval-point'
                              ? 'Add an approval point'
                              : 'Add an escalation path'}
                      </a>
                    </li>
                  ))}
                </ul>
              )}
              {action.conflict && isOnline && (
                <button
                  type="button"
                  className="button-secondary"
                  onClick={() => void refreshRecord()}
                >
                  Refresh record and keep edits
                </button>
              )}
            </div>
          )}
          <fieldset>
            <legend>Decision and accountability</legend>
            <label htmlFor="decision-choice">Decision</label>
            <select
              id="decision-choice"
              value={choice}
              onChange={(event) => {
                const value = event.target.value
                if (isDecisionChoiceKey(value)) {
                  setChoice(value)
                }
              }}
              aria-describedby={
                hasDecisionBlockers ? 'decision-blocker-hint' : undefined
              }
            >
              {Object.entries(decisionChoices).map(([value, option]) => (
                <option
                  key={value}
                  value={value}
                  disabled={
                    hasDecisionBlockers &&
                    (value === 'pilot' || value === 'production')
                  }
                >
                  {option.label}
                </option>
              ))}
            </select>
            {hasDecisionBlockers && (
              <p id="decision-blocker-hint" className="field-hint">
                Pilot and production choices remain unavailable until blockers are
                resolved. The service validates this again when you save.
              </p>
            )}

            <label htmlFor="decision-rationale">
              Rationale <span aria-hidden="true">*</span>
            </label>
            <textarea
              id="decision-rationale"
              rows={5}
              value={rationale}
              onChange={(event) => setRationale(event.target.value)}
              aria-invalid={fieldHasError('decision-rationale')}
              aria-describedby={
                fieldHasError('decision-rationale')
                  ? 'decision-rationale-error'
                  : undefined
              }
              required
            />
            {fieldHasError('decision-rationale') && (
              <p id="decision-rationale-error" className="field-error">
                Explain why this decision follows from the evidence and controls.
              </p>
            )}

            <label htmlFor="decision-owner">
              Accountable owner <span aria-hidden="true">*</span>
            </label>
            <input
              id="decision-owner"
              value={owner}
              onChange={(event) => setOwner(event.target.value)}
              aria-invalid={fieldHasError('decision-owner')}
              aria-describedby={
                fieldHasError('decision-owner')
                  ? 'decision-owner-error'
                  : undefined
              }
              required
            />
            {fieldHasError('decision-owner') && (
              <p id="decision-owner-error" className="field-error">
                Name the person accountable for the decision.
              </p>
            )}

            <label htmlFor="approval-point">
              Approval point <span aria-hidden="true">*</span>
            </label>
            <input
              id="approval-point"
              value={approvalPoint}
              onChange={(event) => setApprovalPoint(event.target.value)}
              aria-invalid={fieldHasError('approval-point')}
              aria-describedby={
                fieldHasError('approval-point')
                  ? 'approval-point-error'
                  : undefined
              }
              required
            />
            {fieldHasError('approval-point') && (
              <p id="approval-point-error" className="field-error">
                Name the review or approval checkpoint.
              </p>
            )}

            <label htmlFor="escalation-path">
              Escalation path <span aria-hidden="true">*</span>
            </label>
            <input
              id="escalation-path"
              value={escalationPath}
              onChange={(event) => setEscalationPath(event.target.value)}
              aria-invalid={fieldHasError('escalation-path')}
              aria-describedby={
                fieldHasError('escalation-path')
                  ? 'escalation-path-error'
                  : undefined
              }
              required
            />
            {fieldHasError('escalation-path') && (
              <p id="escalation-path-error" className="field-error">
                Name where unresolved concerns must go.
              </p>
            )}
          </fieldset>

          <button
            type="submit"
            disabled={
              !isOnline ||
              action.status === 'saving' ||
              etag === null ||
              isReviewStale
            }
          >
            {action.status === 'saving' ? action.message : 'Save decision'}
          </button>
          {!isOnline && (
            <p className="inline-notice">
              You are offline. Your edits remain in this tab; reconnect before
              saving the decision.
            </p>
          )}
          {action.status === 'success' && (
            <p className="success-message" role="status">
              {action.message}
            </p>
          )}
        </form>
      </section>
    </div>
  )
}
