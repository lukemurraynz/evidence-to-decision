import { useState, type FormEvent } from 'react'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import {
  ArtifactType,
  isArtifactTypeValue,
  type ArtifactEnvelope,
  type ArtifactTypeValue,
  type Engagement,
} from '../api/contracts'
import { dateTimeLabel } from '../app/labels'
import { EmptyState } from '../components/AsyncStates'

type ArtifactState =
  | { readonly status: 'idle' }
  | { readonly status: 'generating' }
  | { readonly status: 'ready'; readonly artifact: ArtifactEnvelope }
  | { readonly status: 'error'; readonly message: string }

const artifactChoices = [
  [ArtifactType.PilotBrief, 'Pilot plan'],
  [ArtifactType.DecisionRecord, 'Decision record'],
  [ArtifactType.ExecutiveSummary, 'Executive summary'],
  [ArtifactType.ArchitectureHandoff, 'Technical delivery brief'],
  [ArtifactType.ExperimentDefinition, 'Test plan'],
] as const

export function HandoffView({
  client,
  workspaceId,
  engagement,
  isOnline,
}: {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly isOnline: boolean
}) {
  const [opportunityId, setOpportunityId] = useState(
    engagement.opportunities[0]?.id ?? '',
  )
  const [artifactType, setArtifactType] = useState<ArtifactTypeValue>(
    ArtifactType.ArchitectureHandoff,
  )
  const [artifactState, setArtifactState] = useState<ArtifactState>({
    status: 'idle',
  })

  if (engagement.opportunities.length === 0) {
    return (
      <section className="page">
        <header className="page-header">
          <div>
            <p className="eyebrow">Delivery documents</p>
            <h1>Prepare approved work for delivery</h1>
          </div>
        </header>
        <EmptyState
          title="No delivery document can be created yet"
          message="Frame an opportunity and record its decision before creating a delivery document."
        />
      </section>
    )
  }

  const generate = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    setArtifactState({ status: 'generating' })
    try {
      const result = await client.generateArtifact(
        workspaceId,
        engagement.id,
        opportunityId,
        artifactType,
      )
      setArtifactState({ status: 'ready', artifact: result.data })
    } catch (error: unknown) {
      setArtifactState({
        status: 'error',
        message:
          error instanceof ApiRequestError
            ? error.message
            : 'The delivery document could not be created. Try again.',
      })
    }
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Delivery documents</p>
          <h1>Prepare approved work for delivery</h1>
          <p>
            Create a document from the current opportunity record. Older documents
            remain visibly tied to the record version used to create them.
          </p>
        </div>
      </header>

      <div className="handoff-grid">
        <section className="handoff-builder" aria-labelledby="handoff-builder-heading">
          <p className="eyebrow">Create a document</p>
          <h2 id="handoff-builder-heading">Choose a document</h2>
          <form onSubmit={(event) => void generate(event)}>
            <fieldset>
              <legend>Document source</legend>
              <label htmlFor="handoff-opportunity">Opportunity</label>
              <select
                id="handoff-opportunity"
                value={opportunityId}
                onChange={(event) => setOpportunityId(event.target.value)}
              >
                {engagement.opportunities.map((opportunity) => (
                  <option key={opportunity.id} value={opportunity.id}>
                    {opportunity.desiredOutcome}
                  </option>
                ))}
              </select>

              <label htmlFor="artifact-type">Document type</label>
              <select
                id="artifact-type"
                value={artifactType}
                onChange={(event) => {
                  const value = Number(event.target.value)
                  if (isArtifactTypeValue(value)) {
                    setArtifactType(value)
                  }
                }}
              >
                {artifactChoices.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </fieldset>

            <button
              type="submit"
              disabled={!isOnline || artifactState.status === 'generating'}
            >
              {artifactState.status === 'generating'
                ? 'Preparing document…'
                : 'Create document'}
            </button>
            {!isOnline && (
              <p className="inline-notice">
                You are offline. Reconnect to create a document from the current
                engagement record.
              </p>
            )}
          </form>
          {artifactState.status === 'error' && (
            <p className="error-message" role="alert">
              {artifactState.message}
            </p>
          )}
        </section>

        <section className="artifact-preview" aria-labelledby="artifact-heading">
          <p className="eyebrow">Document preview</p>
          <h2 id="artifact-heading">
            {artifactState.status === 'ready'
              ? artifactLabel(artifactState.artifact.artifactType)
              : 'No document created yet'}
          </h2>
          {artifactState.status !== 'ready' ? (
            <p>
              Choose an opportunity and document type. The completed document will
              appear here.
            </p>
          ) : (
            <ArtifactPreview
              artifact={artifactState.artifact}
              currentVersion={engagement.objectVersion}
            />
          )}
        </section>
      </div>
    </section>
  )
}

function artifactLabel(value: ArtifactTypeValue): string {
  return (
    artifactChoices.find(([artifactValue]) => artifactValue === value)?.[1] ??
    'Delivery document'
  )
}

function readableLabel(value: string): string {
  const spaced = value.replace(/([a-z])([A-Z])/g, '$1 $2')
  return `${spaced.charAt(0).toUpperCase()}${spaced.slice(1)}`
}

function readableValue(value: unknown): string {
  if (typeof value === 'string' || typeof value === 'number') return String(value)
  if (typeof value === 'boolean') return value ? 'Yes' : 'No'
  if (Array.isArray(value)) {
    const readableItems = value.filter(
      (item): item is string | number =>
        typeof item === 'string' || typeof item === 'number',
    )
    return readableItems.length > 0
      ? readableItems.join(', ')
      : 'Structured details included in download'
  }
  return 'Structured details included in download'
}

function ArtifactPreview({
  artifact,
  currentVersion,
}: {
  readonly artifact: ArtifactEnvelope
  readonly currentVersion: number
}) {
  const download = (): void => {
    const blob = new Blob([JSON.stringify(artifact, null, 2)], {
      type: 'application/json',
    })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `handoff-${artifact.artifactId}.json`
    link.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div className="generated-content" data-content-origin="generated">
      <p className="origin-label">Generated document · review before use</p>
      <div
        className={
          artifact.staleness === 0 ? 'freshness-banner' : 'stale-banner'
        }
        role="status"
      >
        <strong>
          {artifact.staleness === 0
            ? 'Current document'
            : artifact.staleness === 1
              ? 'Older document'
              : 'Source record unavailable'}
        </strong>
        <span>
          {artifact.staleness === 0
            ? `Matches engagement record version ${currentVersion}.`
            : artifact.staleness === 1
              ? `Created from version ${artifact.sourceCanonicalGraphVersion}; the current record is version ${currentVersion}. Create a new document before delivery.`
              : 'The source version cannot be checked. Do not use this document for delivery.'}
        </span>
      </div>
      <p className="artifact-version">
        Prepared {dateTimeLabel(artifact.generatedAt)} · source record version{' '}
        {artifact.sourceCanonicalGraphVersion}
      </p>
      {artifact.narrativeSummary !== null && (
        <div className="artifact-narrative">
          <p className="origin-label">AI-written summary · not verified</p>
          <p className="artifact-narrative-summary">{artifact.narrativeSummary.summary}</p>
          <p className="artifact-narrative-review">{artifact.narrativeSummary.requiredReview}</p>
        </div>
      )}
      <dl className="artifact-content">
        {Object.entries(artifact.content)
          .filter(([key]) => key !== 'contentType')
          .map(([key, value]) => (
            <div key={key}>
              <dt>{readableLabel(key)}</dt>
              <dd>{readableValue(value)}</dd>
            </div>
          ))}
      </dl>
      <details>
        <summary>Show document details</summary>
        <dl className="technical-details">
          <div>
            <dt>Prepared by</dt>
            <dd>{artifact.generatedBy}</dd>
          </div>
          <div>
            <dt>Method version</dt>
            <dd>{artifact.methodVersion}</dd>
          </div>
          <div>
            <dt>Document reference</dt>
            <dd>{artifact.artifactId}</dd>
          </div>
        </dl>
      </details>
      <button type="button" onClick={download}>
        Download document
      </button>
    </div>
  )
}
