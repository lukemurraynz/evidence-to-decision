import { useEffect, useState } from 'react'
import {
  ApiRequestError,
  OperationPollingError,
  type OpportunityApiClient,
} from '../api/client'
import {
  OperationStatus,
  type DurableOperation,
  type Engagement,
} from '../api/contracts'
import { dateTimeLabel, operationStatusLabel } from '../app/labels'

type ProgressState =
  | { readonly status: 'checking' }
  | { readonly status: 'active'; readonly operation: DurableOperation }
  | { readonly status: 'complete'; readonly operation: DurableOperation }
  | {
      readonly status: 'error'
      readonly message: string
      readonly retryable: boolean
    }

export function ProgressView({
  client,
  workspaceId,
  operationId,
  engagement,
  opportunityId,
}: {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly operationId: string
  readonly engagement: Engagement
  readonly opportunityId?: string
}) {
  const [progress, setProgress] = useState<ProgressState>({
    status: 'checking',
  })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    void client
      .pollOperation(workspaceId, operationId, {
        signal: controller.signal,
        onProgress: (operation) => {
          setProgress({ status: 'active', operation })
        },
      })
      .then((operation) => {
        setProgress({ status: 'complete', operation })
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        setProgress({
          status: 'error',
          message:
            error instanceof ApiRequestError ||
            error instanceof OperationPollingError
              ? error.message
              : 'The review brief status could not be checked. Return to decision review and try again.',
          retryable:
            !(error instanceof ApiRequestError) ||
            ![401, 403, 404, 422].includes(error.status),
        })
      })
    return () => controller.abort()
  }, [attempt, client, operationId, workspaceId])

  const operation =
    progress.status === 'active' || progress.status === 'complete'
      ? progress.operation
      : null
  const opportunity = engagement.opportunities.find(
    (item) => item.id === opportunityId,
  )
  const evidence =
    opportunity?.evidenceReferences
      .map((reference) =>
        engagement.evidence.find((item) => item.id === reference),
      )
      .filter((item) => item !== undefined) ?? []
  const blockers =
    opportunity === undefined
      ? []
      : engagement.blockers.filter(
          (blocker) => blocker.opportunityId === opportunity.id,
        )

  return (
    <section className="state-page progress-page">
      <p className="eyebrow">Review brief status</p>
      <h1 aria-live="polite" aria-atomic="true">
        {operation === null
          ? 'Checking progress'
          : operationStatusLabel(operation.status)}
      </h1>
      {(progress.status === 'checking' ||
        (operation !== null &&
          (operation.status === OperationStatus.Queued ||
            operation.status === OperationStatus.Running))) && (
        <>
          <div className="progress-track" aria-label="In progress">
            <span />
          </div>
          <p>
            You can leave this page. The work continues and can be checked again
            from the same address.
          </p>
        </>
      )}
      {progress.status === 'complete' &&
        progress.operation.status === OperationStatus.Succeeded && (
          <div
            className="assisted-brief"
            data-content-origin="generated"
            aria-labelledby="assisted-brief-heading"
          >
            <p className="origin-label">AI-assisted · reviewer approval required</p>
            <h2 id="assisted-brief-heading">
              Suggested next step: assess the brief and record the human decision
            </h2>
            <p>
              The review brief is ready. It cannot approve or change this
              opportunity.
            </p>

            <section aria-labelledby="brief-evidence-heading">
              <h3 id="brief-evidence-heading">Evidence to check</h3>
              {evidence.length === 0 ? (
                <p>
                  No linked evidence is available in the current record. Treat the
                  brief as unsupported until evidence is attached.
                </p>
              ) : (
                <ul>
                  {evidence.map((item) => (
                    <li key={item.id}>
                      <blockquote>{item.statement}</blockquote>
                      <p>
                        {item.sourceReference} · captured{' '}
                        {dateTimeLabel(item.capturedAt)}
                      </p>
                    </li>
                  ))}
                </ul>
              )}
            </section>

            <section aria-labelledby="brief-limits-heading">
              <h3 id="brief-limits-heading">Known limitations</h3>
              {blockers.length === 0 ? (
                <p>
                  No governance blockers are recorded. The decision reviewer must
                  still check evidence quality, trust controls, and delivery
                  readiness.
                </p>
              ) : (
                <ul>
                  {blockers.map((blocker) => (
                    <li key={blocker.id}>
                      {blocker.rationale}. {blocker.remediationPath}
                    </li>
                  ))}
                </ul>
              )}
              <p>
                The completed brief cannot be opened in this interface. Return to
                decision review and use the current engagement record before
                deciding.
              </p>
            </section>

            <div className="approval-notice" role="status">
              <strong>Human approval is required.</strong>
              <span>
                A decision reviewer owns the final rationale, approval point, and
                escalation path.
              </span>
            </div>

            <details>
              <summary>Show review brief details</summary>
              <dl className="technical-details">
                <div>
                  <dt>Prepared</dt>
                  <dd>{dateTimeLabel(progress.operation.updatedAt)}</dd>
                </div>
                <div>
                  <dt>Source record version</dt>
                  <dd>{engagement.objectVersion}</dd>
                </div>
                <div>
                  <dt>Status</dt>
                  <dd>{operationStatusLabel(progress.operation.status)}</dd>
                </div>
                <div>
                  <dt>Brief reference</dt>
                  <dd>{progress.operation.resultReference ?? 'Not returned'}</dd>
                </div>
              </dl>
            </details>
            <a className="button-link" href="#/review">
              Back to decision review
            </a>
          </div>
        )}
      {progress.status === 'complete' &&
        progress.operation.status !== OperationStatus.Succeeded && (
          <>
            <p>The review brief was not added to the engagement record.</p>
            <a className="button-link" href="#/review">
              Back to decision review
            </a>
          </>
        )}
      {progress.status === 'error' && (
        <div role="alert">
          <p>{progress.message}</p>
          {progress.retryable && (
            <button
              type="button"
              onClick={() => {
                setProgress({ status: 'checking' })
                setAttempt((current) => current + 1)
              }}
            >
              Check status again
            </button>
          )}
          <a className="button-link" href="#/review">
            Back to decision review
          </a>
        </div>
      )}
    </section>
  )
}
