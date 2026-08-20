import {
  useEffect,
  useRef,
  useState,
  type Dispatch,
  type FormEvent,
  type SetStateAction,
} from 'react'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import {
  EvidenceModality,
  EvidenceType,
  ValidationStatus,
  type Engagement,
  type Evidence,
  type EvidenceQualityAssessment,
  type EvidenceTypeValue,
  isEvidenceTypeValue,
} from '../api/contracts'
import {
  emptyEvidenceDraft,
  type EvidenceDraft,
} from '../app/evidenceDraft'
import {
  dateTimeLabel,
  evidenceModalityLabel,
  evidenceTypeLabel,
  lifecycleLabel,
  validationLabel,
} from '../app/labels'
import { EmptyState } from '../components/AsyncStates'

type DetailsSaveState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'error'; readonly message: string }

function linesToList(value: string): readonly string[] {
  return value
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line !== '')
}

function EngagementDetailsPanel({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
}: {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly etag: string | null
  readonly isOnline: boolean
  readonly onUpdated: (engagement: Engagement, etag: string | null) => void
}) {
  const [editing, setEditing] = useState(false)
  const [objectives, setObjectives] = useState('')
  const [participants, setParticipants] = useState('')
  const [save, setSave] = useState<DetailsSaveState>({ status: 'idle' })

  const startEditing = (): void => {
    setObjectives(engagement.objectives.join('\n'))
    setParticipants(engagement.participants.join('\n'))
    setSave({ status: 'idle' })
    setEditing(true)
  }

  const cancelEditing = (): void => {
    setEditing(false)
    setSave({ status: 'idle' })
  }

  const submit = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    if (etag === null) {
      setSave({
        status: 'error',
        message: 'This engagement could not be verified. Reload before saving.',
      })
      return
    }
    setSave({ status: 'saving' })
    try {
      const result = await client.updateEngagementDetails(
        workspaceId,
        engagement.id,
        { objectives: linesToList(objectives), participants: linesToList(participants) },
        etag,
      )
      onUpdated(result.data, result.etag)
      setEditing(false)
      setSave({ status: 'idle' })
    } catch (error: unknown) {
      const conflict =
        error instanceof ApiRequestError && (error.status === 409 || error.status === 412)
      setSave({
        status: 'error',
        message: conflict
          ? 'The engagement changed before this was saved. Reload and try again.'
          : error instanceof ApiRequestError
            ? error.message
            : 'Could not save. Check the connection and try again.',
      })
    }
  }

  return (
    <section className="engagement-details-panel" aria-labelledby="details-heading">
      <div className="section-heading compact">
        <div>
          <p className="eyebrow">Workshop details</p>
          <h2 id="details-heading">Objectives and participants</h2>
        </div>
        {!editing && (
          <button type="button" className="button-secondary" onClick={startEditing}>
            Edit details
          </button>
        )}
      </div>
      {editing ? (
        <form onSubmit={(event) => void submit(event)}>
          <label htmlFor="details-objectives">Objectives (one per line)</label>
          <textarea
            id="details-objectives"
            value={objectives}
            onChange={(event) => setObjectives(event.target.value)}
            disabled={save.status === 'saving'}
          />
          <label htmlFor="details-participants">Participants (one per line)</label>
          <textarea
            id="details-participants"
            value={participants}
            onChange={(event) => setParticipants(event.target.value)}
            disabled={save.status === 'saving'}
          />
          {save.status === 'error' && <p className="form-error-summary">{save.message}</p>}
          <div className="card-shortlist-form-actions">
            <button type="submit" disabled={!isOnline || save.status === 'saving'}>
              {save.status === 'saving' ? 'Saving…' : 'Save details'}
            </button>
            <button type="button" className="button-secondary" onClick={cancelEditing}>
              Cancel
            </button>
          </div>
        </form>
      ) : (
        <dl className="engagement-details-summary">
          <div>
            <dt>Objectives</dt>
            <dd>
              {engagement.objectives.length === 0 ? 'None recorded' : engagement.objectives.join(', ')}
            </dd>
          </div>
          <div>
            <dt>Participants</dt>
            <dd>
              {engagement.participants.length === 0
                ? 'None recorded'
                : engagement.participants.join(', ')}
            </dd>
          </div>
        </dl>
      )}
    </section>
  )
}

type QualityCheckState =
  | { readonly status: 'idle' }
  | { readonly status: 'checking' }
  | { readonly status: 'checked'; readonly assessment: EvidenceQualityAssessment }
  | { readonly status: 'error'; readonly message: string }

/**
 * Advisory only: flags evidence-capture problems while the facilitator can still fix them,
 * rather than only discovering unusable evidence later when a recommendation agent abstains
 * on it. Never rewrites Statement itself; a facilitator who agrees with the suggestion has to
 * act on it through the existing correction flow.
 */
function EvidenceQualityCheck({
  client,
  workspaceId,
  engagementId,
  evidence,
  isOnline,
}: {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagementId: string
  readonly evidence: Evidence
  readonly isOnline: boolean
}) {
  const [state, setState] = useState<QualityCheckState>({ status: 'idle' })

  const check = async (): Promise<void> => {
    setState({ status: 'checking' })
    try {
      const result = await client.assessEvidenceQuality(workspaceId, engagementId, evidence.id)
      setState({ status: 'checked', assessment: result.data })
    } catch (error: unknown) {
      setState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not assess this evidence.',
      })
    }
  }

  if (state.status === 'idle' || state.status === 'error') {
    return (
      <div className="evidence-quality-check">
        <button
          type="button"
          className="button-secondary"
          onClick={() => void check()}
          disabled={!isOnline}
        >
          Check evidence quality
        </button>
        {state.status === 'error' && <p className="form-error-summary">{state.message}</p>}
      </div>
    )
  }

  if (state.status === 'checking') {
    return (
      <div className="evidence-quality-check">
        <p className="discovery-cards-count">Checking…</p>
      </div>
    )
  }

  const { assessment } = state
  return (
    <div className="evidence-quality-check">
      <p className="origin-label">AI quality check · not verified</p>
      {assessment.concerns.length === 0 ? (
        <p className="evidence-quality-ok">No concerns. This statement looks specific and factual.</p>
      ) : (
        <ul className="evidence-quality-concerns">
          {assessment.concerns.map((concern) => (
            <li key={concern}>{concern}</li>
          ))}
        </ul>
      )}
      {assessment.concerns.length > 0 && (
        <p className="evidence-quality-suggestion">
          Suggested rewording: <em>{assessment.suggestion}</em>
        </p>
      )}
    </div>
  )
}

type ActionState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'success'; readonly message: string }
  | {
      readonly status: 'error'
      readonly message: string
      readonly fields: readonly string[]
      readonly conflict: boolean
    }

type EvidenceWorkbenchProps = {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly etag: string | null
  readonly isOnline: boolean
  readonly draft: EvidenceDraft
  readonly setDraft: Dispatch<SetStateAction<EvidenceDraft>>
  readonly onUpdated: (engagement: Engagement, etag: string | null) => void
}

const evidenceTypes = Object.entries(EvidenceType) as readonly [
  string,
  EvidenceTypeValue,
][]

export function EvidenceWorkbench({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  draft,
  setDraft,
  onUpdated,
}: EvidenceWorkbenchProps) {
  const [action, setAction] = useState<ActionState>({ status: 'idle' })
  const errorSummaryRef = useRef<HTMLDivElement | null>(null)
  const hasDraft =
    draft.statement.trim() !== '' ||
    draft.sourceReference.trim() !== '' ||
    draft.participantReference.trim() !== '' ||
    draft.interpretation.trim() !== ''

  useEffect(() => {
    if (!hasDraft) return
    const protectDraft = (event: BeforeUnloadEvent): void => {
      event.preventDefault()
    }
    window.addEventListener('beforeunload', protectDraft)
    return () => window.removeEventListener('beforeunload', protectDraft)
  }, [hasDraft])

  const saveEvidence = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    const missingFields = [
      draft.statement.trim() === '' ? 'evidence-statement' : null,
      draft.sourceReference.trim() === '' ? 'evidence-source' : null,
      draft.confidence.trim() !== '' &&
      Number.isFinite(Number(draft.confidence)) &&
      Number(draft.confidence) >= 0 &&
      Number(draft.confidence) <= 1
        ? null
        : 'evidence-confidence',
    ].filter((field): field is string => field !== null)
    if (missingFields.length > 0) {
      setAction({
        status: 'error',
        message: 'Check the highlighted evidence fields.',
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
          'This engagement could not be verified. Reload before saving evidence.',
        fields: [],
        conflict: true,
      })
      requestAnimationFrame(() => errorSummaryRef.current?.focus())
      return
    }
    setAction({ status: 'saving' })
    try {
      const result = await client.captureEvidence(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          type: draft.type,
          statement: draft.statement.trim(),
          sourceReference: draft.sourceReference.trim(),
          capturedAt: new Date().toISOString(),
          modality: EvidenceModality.Text,
          confidence: Number(draft.confidence),
          validationStatus: ValidationStatus.Unvalidated,
          ...(draft.participantReference.trim()
            ? { participantReference: draft.participantReference.trim() }
            : {}),
          ...(draft.interpretation.trim()
            ? { interpretation: draft.interpretation.trim() }
            : {}),
        },
        etag,
      )
      setDraft(emptyEvidenceDraft)
      onUpdated(result.data, result.etag)
      setAction({
        status: 'success',
        message: 'Evidence saved. It is ready for validation.',
      })
    } catch (error: unknown) {
      const conflict =
        error instanceof ApiRequestError &&
        (error.status === 409 || error.status === 412)
      setAction({
        status: 'error',
        message:
          conflict
            ? 'The engagement changed before this evidence was saved. Your draft is still here. Refresh the record, review your entry, then save again.'
            : error instanceof ApiRequestError
            ? error.message
            : 'Evidence could not be saved. Check the connection and try again.',
        fields: [],
        conflict,
      })
      requestAnimationFrame(() => errorSummaryRef.current?.focus())
    }
  }

  const refreshRecord = async (): Promise<void> => {
    setAction({ status: 'saving' })
    try {
      const result = await client.getEngagement(workspaceId, engagement.id)
      onUpdated(result.data, result.etag)
      setAction({
        status: 'success',
        message: 'Record refreshed. Your evidence draft is still here.',
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

  const fieldHasError = (id: string): boolean =>
    action.status === 'error' && action.fields.includes(id)

  return (
    <div className="workbench">
      <header className="page-header">
        <div>
          <p className="eyebrow">Workshop evidence</p>
          <h1>Build the evidence trail</h1>
          <p>
            Keep source wording separate from interpretation. Each statement
            remains attributable as the opportunity develops.
          </p>
        </div>
        <dl className="header-facts">
          <div>
            <dt>Readiness</dt>
            <dd>{lifecycleLabel(engagement.lifecycleState)}</dd>
          </div>
          <div>
            <dt>Evidence version</dt>
            <dd>{engagement.objectVersion}</dd>
          </div>
          <div>
            <dt>Evidence</dt>
            <dd>{engagement.evidence.length}</dd>
          </div>
        </dl>
      </header>

      <EngagementDetailsPanel
        client={client}
        workspaceId={workspaceId}
        engagement={engagement}
        etag={etag}
        isOnline={isOnline}
        onUpdated={onUpdated}
      />

      <div className="workbench-grid">
        <section className="capture-pane" aria-labelledby="capture-heading">
          <p className="eyebrow">Capture</p>
          <h2 id="capture-heading">Add source evidence</h2>
          <form onSubmit={(event) => void saveEvidence(event)} noValidate>
            {action.status === 'error' && (
              <div
                className="form-error-summary"
                ref={errorSummaryRef}
                tabIndex={-1}
                role="alert"
              >
                <h3>Evidence not saved</h3>
                <p>{action.message}</p>
                {action.fields.length > 0 && (
                  <ul>
                    {action.fields.map((field) => (
                      <li key={field}>
                        <a href={`#${field}`}>
                          {field === 'evidence-statement'
                            ? 'Add what was observed or said'
                            : field === 'evidence-source'
                              ? 'Add a source reference'
                              : 'Enter confidence from 0 to 1'}
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
                    Refresh record and keep draft
                  </button>
                )}
              </div>
            )}
            <fieldset>
              <legend>Evidence details</legend>
              <label htmlFor="evidence-type">Evidence type</label>
              <select
                id="evidence-type"
                value={draft.type}
                onChange={(event) => {
                  const value = Number(event.target.value)
                  if (isEvidenceTypeValue(value)) {
                    setDraft((current) => ({ ...current, type: value }))
                  }
                }}
              >
                {evidenceTypes.map(([name, value]) => (
                  <option key={name} value={value}>
                    {evidenceTypeLabel(value)}
                  </option>
                ))}
              </select>

              <label htmlFor="evidence-statement">
                What was observed or said? <span aria-hidden="true">*</span>
              </label>
              <textarea
                id="evidence-statement"
                rows={5}
                value={draft.statement}
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    statement: event.target.value,
                  }))
                }
                aria-invalid={fieldHasError('evidence-statement')}
                aria-describedby={
                  fieldHasError('evidence-statement')
                    ? 'evidence-statement-error'
                    : undefined
                }
                required
              />
              {fieldHasError('evidence-statement') && (
                <p id="evidence-statement-error" className="field-error">
                  Add the source wording without interpretation.
                </p>
              )}

              <label htmlFor="evidence-source">
                Source reference <span aria-hidden="true">*</span>
              </label>
              <input
                id="evidence-source"
                value={draft.sourceReference}
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    sourceReference: event.target.value,
                  }))
                }
                aria-invalid={fieldHasError('evidence-source')}
                aria-describedby={
                  fieldHasError('evidence-source')
                    ? 'evidence-source-error'
                    : undefined
                }
                required
              />
              {fieldHasError('evidence-source') && (
                <p id="evidence-source-error" className="field-error">
                  Name the interview, document, measure, or other source.
                </p>
              )}

              <label htmlFor="evidence-participant">
                Participant reference <span className="optional">(optional)</span>
              </label>
              <input
                id="evidence-participant"
                value={draft.participantReference}
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    participantReference: event.target.value,
                  }))
                }
              />

              <label htmlFor="evidence-interpretation">
                Facilitator interpretation <span className="optional">(optional)</span>
              </label>
              <textarea
                id="evidence-interpretation"
                rows={3}
                value={draft.interpretation}
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    interpretation: event.target.value,
                  }))
                }
              />

              <label htmlFor="evidence-confidence">
                Confidence <span aria-hidden="true">*</span>
              </label>
              <input
                id="evidence-confidence"
                type="number"
                min="0"
                max="1"
                step="0.05"
                value={draft.confidence}
                onChange={(event) =>
                  setDraft((current) => ({
                    ...current,
                    confidence: event.target.value,
                  }))
                }
                aria-invalid={fieldHasError('evidence-confidence')}
                aria-describedby={
                  fieldHasError('evidence-confidence')
                    ? 'evidence-confidence-hint evidence-confidence-error'
                    : 'evidence-confidence-hint'
                }
                required
              />
              <p id="evidence-confidence-hint" className="field-hint">
                Use 0 for uncertain and 1 for confirmed.
              </p>
              {fieldHasError('evidence-confidence') && (
                <p id="evidence-confidence-error" className="field-error">
                  Enter a number from 0 to 1.
                </p>
              )}
            </fieldset>

            <button
              type="submit"
              disabled={!isOnline || action.status === 'saving' || etag === null}
            >
              {action.status === 'saving' ? 'Saving evidence…' : 'Save evidence'}
            </button>
            {!isOnline && (
              <p className="inline-notice">
                You are offline. Keep this tab open; your draft remains here until
                you reconnect.
              </p>
            )}
            {action.status === 'success' && (
              <p className="success-message" role="status">
                {action.message}
              </p>
            )}
          </form>
        </section>

        <section className="evidence-pane" aria-labelledby="evidence-heading">
          <div className="section-heading compact">
            <div>
              <p className="eyebrow">Evidence thread</p>
              <h2 id="evidence-heading">What the record supports</h2>
            </div>
            <span>{engagement.evidence.length} records</span>
          </div>
          {engagement.evidence.length === 0 ? (
            <EmptyState
              title="No evidence captured"
              message="Add the first attributed statement to begin the evidence trail."
            />
          ) : (
            <ol className="evidence-thread">
              {engagement.evidence.map((evidence) => (
                <li key={evidence.id}>
                  <p className="origin-label">Workshop record · participant supplied</p>
                  <div className="evidence-meta">
                    <span>{evidenceTypeLabel(evidence.type)}</span>
                    <span>{validationLabel(evidence.validationStatus)}</span>
                    <span>{evidenceModalityLabel(evidence.modality)}</span>
                  </div>
                  <blockquote>{evidence.statement}</blockquote>
                  {evidence.interpretation && (
                    <p className="evidence-interpretation">
                      Facilitator interpretation: {evidence.interpretation}
                    </p>
                  )}
                  <EvidenceQualityCheck
                    client={client}
                    workspaceId={workspaceId}
                    engagementId={engagement.id}
                    evidence={evidence}
                    isOnline={isOnline}
                  />
                  <dl className="evidence-provenance">
                    <div>
                      <dt>Source</dt>
                      <dd>{evidence.sourceReference}</dd>
                    </div>
                    <div>
                      <dt>Participant</dt>
                      <dd>{evidence.participantReference ?? 'Not recorded'}</dd>
                    </div>
                    <div>
                      <dt>Captured</dt>
                      <dd>{dateTimeLabel(evidence.capturedAt)}</dd>
                    </div>
                    <div>
                      <dt>Confidence</dt>
                      <dd>{Math.round(evidence.confidence * 100)}%</dd>
                    </div>
                    <div>
                      <dt>Record version</dt>
                      <dd>{evidence.objectVersion}</dd>
                    </div>
                  </dl>
                </li>
              ))}
            </ol>
          )}
        </section>
      </div>
    </div>
  )
}
