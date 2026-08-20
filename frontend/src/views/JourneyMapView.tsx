import { useEffect, useState, type FormEvent } from 'react'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import type { Engagement } from '../api/contracts'

type ActionState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'success'; readonly message: string }
  | { readonly status: 'error'; readonly message: string; readonly conflict: boolean }

type JourneyMapViewProps = {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly etag: string | null
  readonly isOnline: boolean
  readonly onUpdated: (engagement: Engagement, etag: string | null) => void
}

function linesToList(value: string): readonly string[] {
  return value
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line !== '')
}

type StepDraft = {
  readonly name: string
  readonly painPoint: string
  readonly opportunityArea: string
  readonly successMetric: string
}

const emptyStepDraft: StepDraft = {
  name: '',
  painPoint: '',
  opportunityArea: '',
  successMetric: '',
}

export function JourneyMapView({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
}: JourneyMapViewProps) {
  return (
    <section className="page journey-map-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Persona and journey mapping</p>
          <h1>Map the persona's journey</h1>
          <p>
            Name who this is for, then lay out their journey step by step: the
            pain point, the opportunity, and how you would measure success at
            each stage.
          </p>
        </div>
      </header>

      <PersonaStage
        client={client}
        workspaceId={workspaceId}
        engagement={engagement}
        etag={etag}
        isOnline={isOnline}
        onUpdated={onUpdated}
      />
      <JourneyMapStage
        client={client}
        workspaceId={workspaceId}
        engagement={engagement}
        etag={etag}
        isOnline={isOnline}
        onUpdated={onUpdated}
      />
    </section>
  )
}

function PersonaStage({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
}: JourneyMapViewProps) {
  const [name, setName] = useState('')
  const [role, setRole] = useState('')
  const [goals, setGoals] = useState('')
  const [painPoints, setPainPoints] = useState('')
  const [characteristics, setCharacteristics] = useState('')
  const [action, setAction] = useState<ActionState>({ status: 'idle' })

  const save = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    if (name.trim() === '' || role.trim() === '') {
      setAction({
        status: 'error',
        message: 'Name the persona and their role.',
        conflict: false,
      })
      return
    }
    if (etag === null) {
      setAction({
        status: 'error',
        message: 'This engagement could not be verified. Reload before saving.',
        conflict: true,
      })
      return
    }
    setAction({ status: 'saving' })
    try {
      const result = await client.addPersona(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          name: name.trim(),
          role: role.trim(),
          goals: linesToList(goals),
          painPoints: linesToList(painPoints),
          characteristics: linesToList(characteristics),
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setName('')
      setRole('')
      setGoals('')
      setPainPoints('')
      setCharacteristics('')
      setAction({ status: 'success', message: 'Persona saved.' })
    } catch (error: unknown) {
      const conflict =
        error instanceof ApiRequestError && (error.status === 409 || error.status === 412)
      setAction({
        status: 'error',
        message: conflict
          ? 'The engagement changed before this persona was saved. Refresh and try again.'
          : error instanceof ApiRequestError
            ? error.message
            : 'The persona could not be saved. Check the connection and try again.',
        conflict,
      })
    }
  }

  return (
    <div className="workbench-grid frame-stage">
      <section className="capture-pane" aria-labelledby="persona-heading">
        <p className="eyebrow">Stage 1 · Persona</p>
        <h2 id="persona-heading">Who is this for?</h2>
        <form onSubmit={(event) => void save(event)} noValidate>
          {action.status === 'error' && (
            <div className="form-error-summary" role="alert">
              <p>{action.message}</p>
            </div>
          )}
          <fieldset>
            <legend>Persona</legend>
            <label htmlFor="persona-name">
              Name <span aria-hidden="true">*</span>
            </label>
            <input
              id="persona-name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              required
            />

            <label htmlFor="persona-role">
              Role <span aria-hidden="true">*</span>
            </label>
            <input
              id="persona-role"
              value={role}
              onChange={(event) => setRole(event.target.value)}
              required
            />

            <label htmlFor="persona-goals">
              Goals, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="persona-goals"
              rows={2}
              value={goals}
              onChange={(event) => setGoals(event.target.value)}
            />

            <label htmlFor="persona-pain-points">
              Pain points, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="persona-pain-points"
              rows={2}
              value={painPoints}
              onChange={(event) => setPainPoints(event.target.value)}
            />

            <label htmlFor="persona-characteristics">
              Characteristics, one per line{' '}
              <span className="optional">(optional)</span>
            </label>
            <textarea
              id="persona-characteristics"
              rows={2}
              value={characteristics}
              onChange={(event) => setCharacteristics(event.target.value)}
            />
          </fieldset>
          <button type="submit" disabled={!isOnline || action.status === 'saving' || etag === null}>
            {action.status === 'saving' ? 'Saving persona…' : 'Save persona'}
          </button>
          {action.status === 'success' && (
            <p className="success-message" role="status">
              {action.message}
            </p>
          )}
        </form>
      </section>
      <section className="evidence-pane" aria-labelledby="persona-list-heading">
        <div className="section-heading compact">
          <div>
            <p className="eyebrow">Framed so far</p>
            <h2 id="persona-list-heading">Personas</h2>
          </div>
          <span>{engagement.personas.length} recorded</span>
        </div>
        {engagement.personas.length === 0 ? (
          <p>No personas framed yet. The first one you save appears here.</p>
        ) : (
          <ul className="frame-item-list">
            {engagement.personas.map((persona) => (
              <li key={persona.id}>
                <p className="origin-label">{persona.role}</p>
                <blockquote>{persona.name}</blockquote>
                <p>
                  {persona.goals.length} goals · {persona.painPoints.length} pain points
                </p>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

function JourneyMapStage({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
}: JourneyMapViewProps) {
  const [personaId, setPersonaId] = useState(engagement.personas[0]?.id ?? '')
  const [workflowId, setWorkflowId] = useState('')
  const [steps, setSteps] = useState<readonly StepDraft[]>([emptyStepDraft])
  const [action, setAction] = useState<ActionState>({ status: 'idle' })

  useEffect(() => {
    const firstPersona = engagement.personas[0]
    if (firstPersona === undefined) return
    if (engagement.personas.some((persona) => persona.id === personaId)) return
    setPersonaId(firstPersona.id)
  }, [engagement.personas, personaId])

  const updateStep = (index: number, patch: Partial<StepDraft>): void => {
    setSteps((current) =>
      current.map((step, stepIndex) => (stepIndex === index ? { ...step, ...patch } : step)),
    )
  }

  const removeStep = (index: number): void => {
    setSteps((current) => current.filter((_, stepIndex) => stepIndex !== index))
  }

  const save = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    const filledSteps = steps.filter((step) => step.name.trim() !== '')
    if (personaId.trim() === '' || filledSteps.length === 0) {
      setAction({
        status: 'error',
        message: 'Choose a persona and name at least one journey step.',
        conflict: false,
      })
      return
    }
    if (etag === null) {
      setAction({
        status: 'error',
        message: 'This engagement could not be verified. Reload before saving.',
        conflict: true,
      })
      return
    }
    setAction({ status: 'saving' })
    try {
      const result = await client.addJourneyMap(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          personaId,
          workflowId: workflowId.trim() === '' ? null : workflowId,
          steps: filledSteps.map((step, index) => ({
            id: crypto.randomUUID(),
            order: index + 1,
            name: step.name.trim(),
            painPoint: step.painPoint.trim(),
            opportunityArea: step.opportunityArea.trim(),
            successMetric: step.successMetric.trim(),
          })),
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setSteps([emptyStepDraft])
      setAction({ status: 'success', message: 'Journey map saved.' })
    } catch (error: unknown) {
      const conflict =
        error instanceof ApiRequestError && (error.status === 409 || error.status === 412)
      setAction({
        status: 'error',
        message: conflict
          ? 'The engagement changed before this journey map was saved. Refresh and try again.'
          : error instanceof ApiRequestError
            ? error.message
            : 'The journey map could not be saved. Check the connection and try again.',
        conflict,
      })
    }
  }

  return (
    <div className="workbench-grid frame-stage">
      <section className="capture-pane" aria-labelledby="journey-map-heading">
        <p className="eyebrow">Stage 2 · Journey map</p>
        <h2 id="journey-map-heading">Lay out the journey</h2>
        {engagement.personas.length === 0 ? (
          <p className="inline-notice">
            Frame a persona first. Journey maps attach to a persona.
          </p>
        ) : (
          <form onSubmit={(event) => void save(event)} noValidate>
            {action.status === 'error' && (
              <div className="form-error-summary" role="alert">
                <p>{action.message}</p>
              </div>
            )}
            <fieldset>
              <legend>Whose journey, and against which workflow?</legend>
              <label htmlFor="journey-persona">
                Persona <span aria-hidden="true">*</span>
              </label>
              <select
                id="journey-persona"
                value={personaId}
                onChange={(event) => setPersonaId(event.target.value)}
                required
              >
                {engagement.personas.map((persona) => (
                  <option key={persona.id} value={persona.id}>
                    {persona.name} · {persona.role}
                  </option>
                ))}
              </select>

              {engagement.workflows.length > 0 && (
                <>
                  <label htmlFor="journey-workflow">
                    As-is workflow <span className="optional">(optional)</span>
                  </label>
                  <select
                    id="journey-workflow"
                    value={workflowId}
                    onChange={(event) => setWorkflowId(event.target.value)}
                  >
                    <option value="">Not linked to a workflow</option>
                    {engagement.workflows.map((workflow) => (
                      <option key={workflow.id} value={workflow.id}>
                        {workflow.trigger}
                      </option>
                    ))}
                  </select>
                </>
              )}
            </fieldset>

            {steps.map((step, index) => (
              <fieldset key={index}>
                <legend>
                  Step {index + 1}
                  {steps.length > 1 && (
                    <button
                      type="button"
                      className="button-secondary journey-step-remove"
                      onClick={() => removeStep(index)}
                    >
                      Remove step
                    </button>
                  )}
                </legend>
                <label htmlFor={`journey-step-name-${index}`}>
                  Step name{index === 0 && <span aria-hidden="true"> *</span>}
                </label>
                <input
                  id={`journey-step-name-${index}`}
                  value={step.name}
                  onChange={(event) => updateStep(index, { name: event.target.value })}
                />

                <label htmlFor={`journey-step-pain-${index}`}>Pain point</label>
                <input
                  id={`journey-step-pain-${index}`}
                  value={step.painPoint}
                  onChange={(event) => updateStep(index, { painPoint: event.target.value })}
                />

                <label htmlFor={`journey-step-opportunity-${index}`}>
                  Opportunity area
                </label>
                <input
                  id={`journey-step-opportunity-${index}`}
                  value={step.opportunityArea}
                  onChange={(event) =>
                    updateStep(index, { opportunityArea: event.target.value })
                  }
                />

                <label htmlFor={`journey-step-metric-${index}`}>Success metric</label>
                <input
                  id={`journey-step-metric-${index}`}
                  value={step.successMetric}
                  onChange={(event) =>
                    updateStep(index, { successMetric: event.target.value })
                  }
                />
              </fieldset>
            ))}
            <button
              type="button"
              className="button-secondary"
              onClick={() => setSteps((current) => [...current, emptyStepDraft])}
            >
              Add another step
            </button>

            <button
              type="submit"
              disabled={!isOnline || action.status === 'saving' || etag === null}
            >
              {action.status === 'saving' ? 'Saving journey map…' : 'Save journey map'}
            </button>
            {action.status === 'success' && (
              <p className="success-message" role="status">
                {action.message}
              </p>
            )}
          </form>
        )}
      </section>
      <section className="evidence-pane" aria-labelledby="journey-map-list-heading">
        <div className="section-heading compact">
          <div>
            <p className="eyebrow">Framed so far</p>
            <h2 id="journey-map-list-heading">Journey maps</h2>
          </div>
          <span>{engagement.journeyMaps.length} recorded</span>
        </div>
        {engagement.journeyMaps.length === 0 ? (
          <p>No journey maps framed yet. The first one you save appears here.</p>
        ) : (
          <ul className="frame-item-list">
            {engagement.journeyMaps.map((journeyMap) => {
              const persona = engagement.personas.find(
                (item) => item.id === journeyMap.personaId,
              )
              return (
                <li key={journeyMap.id}>
                  <p className="origin-label">{persona?.name ?? 'Persona'}</p>
                  <blockquote>
                    {journeyMap.steps.length} step
                    {journeyMap.steps.length === 1 ? '' : 's'}
                  </blockquote>
                  <p>{journeyMap.steps.map((step) => step.name).join(' → ')}</p>
                </li>
              )
            })}
          </ul>
        )}
      </section>
    </div>
  )
}
