import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import type { Engagement, FrameDraftCandidate, FrameDraftResult, ReadinessProfile, TrustProfile } from '../api/contracts'

const CONFIDENCE_LABELS = ['Supported', 'Limited', 'Abstain', 'Human review required']

type ActionState =
  | { readonly status: 'idle' }
  | { readonly status: 'saving' }
  | { readonly status: 'success'; readonly message: string }
  | { readonly status: 'error'; readonly message: string; readonly conflict: boolean }

type FrameViewProps = {
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

function selectedOptions(event: React.ChangeEvent<HTMLSelectElement>): readonly string[] {
  return Array.from(event.target.selectedOptions, (option) => option.value)
}

type DraftState =
  | { readonly status: 'idle' }
  | { readonly status: 'drafting' }
  | { readonly status: 'error'; readonly message: string }

export function FrameView({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
}: FrameViewProps) {
  const [draft, setDraft] = useState<FrameDraftResult | null>(null)
  const [selectedCandidateIndex, setSelectedCandidateIndex] = useState<number | null>(null)
  const [draftState, setDraftState] = useState<DraftState>({ status: 'idle' })

  const draftFrame = async (): Promise<void> => {
    setDraftState({ status: 'drafting' })
    try {
      const result = await client.draftFrame(workspaceId, engagement.id)
      setDraft(result.data)
      setSelectedCandidateIndex(result.data.candidates.length > 0 ? 0 : null)
      setDraftState({ status: 'idle' })
    } catch (error: unknown) {
      setDraftState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not draft a frame.',
      })
    }
  }

  const selectedCandidate = useMemo<FrameDraftCandidate | null>(
    () => (draft === null || selectedCandidateIndex === null ? null : draft.candidates[selectedCandidateIndex] ?? null),
    [draft, selectedCandidateIndex],
  )

  return (
    <section className="page frame-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Frame the opportunity</p>
          <h1>Turn evidence into a workflow, a problem, and an opportunity</h1>
          <p>
            Each stage builds on the last. Frame the workflow evidence describes,
            name the problem it creates, then define the opportunity to pursue.
          </p>
        </div>
      </header>

      <div className="frame-draft-action">
        <button
          type="button"
          className="button-secondary"
          onClick={() => void draftFrame()}
          disabled={!isOnline || draftState.status === 'drafting' || engagement.evidence.length === 0}
          title={engagement.evidence.length === 0 ? 'Capture evidence first' : undefined}
        >
          {draftState.status === 'drafting' ? 'Drafting…' : 'Draft workflow and problem with AI'}
        </button>
        {draftState.status === 'error' && <p className="form-error-summary">{draftState.message}</p>}
        {draft !== null && draft.candidates.length === 0 && (
          <p className="discovery-cards-count">No evidence has been captured yet to draft a frame from.</p>
        )}
        {draft !== null && draft.candidates.length > 0 && (
          <div className="ai-suggestion-panel">
            <div className="section-heading compact">
              <div>
                <p className="eyebrow">AI drafted</p>
                <h3>
                  {draft.candidates.length === 1
                    ? '1 candidate framing'
                    : `${draft.candidates.length} candidate framings`}
                </h3>
              </div>
            </div>
            <ul className="ai-suggestion-list">
              {draft.candidates.map((candidate, index) => (
                <li key={index}>
                  <strong>{candidate.problem.user || 'Untitled candidate'}</strong>
                  <p>{candidate.problem.goal}</p>
                  <p className="origin-label">{CONFIDENCE_LABELS[candidate.confidenceStatus]}</p>
                  {candidate.citationConcerns.length > 0 && (
                    <ul className="ai-suggestion-concerns">
                      {candidate.citationConcerns.map((concern) => (
                        <li key={concern}>{concern}</li>
                      ))}
                    </ul>
                  )}
                  <button
                    type="button"
                    onClick={() => setSelectedCandidateIndex(index)}
                    disabled={selectedCandidateIndex === index}
                  >
                    {selectedCandidateIndex === index ? '✓ Loaded into form' : 'Use this'}
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <WorkflowStage
        client={client}
        workspaceId={workspaceId}
        engagement={engagement}
        etag={etag}
        isOnline={isOnline}
        onUpdated={onUpdated}
        candidate={selectedCandidate}
      />
      <ProblemStage
        client={client}
        workspaceId={workspaceId}
        engagement={engagement}
        etag={etag}
        isOnline={isOnline}
        onUpdated={onUpdated}
        candidate={selectedCandidate}
      />
      <OpportunityStage
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

function WorkflowStage({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
  candidate,
}: FrameViewProps & { readonly candidate: FrameDraftCandidate | null }) {
  const [trigger, setTrigger] = useState('')
  const [actors, setActors] = useState('')
  const [inputs, setInputs] = useState('')
  const [steps, setSteps] = useState('')
  const [decisions, setDecisions] = useState('')
  const [systems, setSystems] = useState('')
  const [handoffs, setHandoffs] = useState('')
  const [exceptions, setExceptions] = useState('')
  const [outputs, setOutputs] = useState('')
  const [action, setAction] = useState<ActionState>({ status: 'idle' })

  // Prefills the form when a new candidate is selected, keyed on the candidate object
  // itself, so it fires once per "Use this" click (or once per single-candidate "Draft with
  // AI" click), not on every render, and never fights the facilitator's own edits afterward.
  useEffect(() => {
    if (candidate === null) return
    setTrigger(candidate.workflow.trigger)
    setActors(candidate.workflow.actors.join('\n'))
    setInputs(candidate.workflow.inputs.join('\n'))
    setSteps(candidate.workflow.steps.join('\n'))
    setDecisions(candidate.workflow.decisions.join('\n'))
    setSystems(candidate.workflow.systems.join('\n'))
    setHandoffs(candidate.workflow.handoffs.join('\n'))
    setExceptions(candidate.workflow.exceptions.join('\n'))
    setOutputs(candidate.workflow.outputs.join('\n'))
  }, [candidate])

  const save = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    if (trigger.trim() === '' || steps.trim() === '') {
      setAction({
        status: 'error',
        message: 'Add a trigger and at least one step.',
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
      const result = await client.addWorkflow(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          trigger: trigger.trim(),
          actors: linesToList(actors),
          inputs: linesToList(inputs),
          steps: linesToList(steps),
          decisions: linesToList(decisions),
          systems: linesToList(systems),
          handoffs: linesToList(handoffs),
          exceptions: linesToList(exceptions),
          outputs: linesToList(outputs),
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setTrigger('')
      setActors('')
      setInputs('')
      setSteps('')
      setDecisions('')
      setSystems('')
      setHandoffs('')
      setExceptions('')
      setOutputs('')
      setAction({ status: 'success', message: 'Workflow saved.' })
    } catch (error: unknown) {
      const conflict =
        error instanceof ApiRequestError && (error.status === 409 || error.status === 412)
      setAction({
        status: 'error',
        message:
          conflict
            ? 'The engagement changed before this workflow was saved. Refresh and try again.'
            : error instanceof ApiRequestError
              ? error.message
              : 'The workflow could not be saved. Check the connection and try again.',
        conflict,
      })
    }
  }

  return (
    <div className="workbench-grid frame-stage">
      <section className="capture-pane" aria-labelledby="workflow-heading">
        <p className="eyebrow">Stage 1 · Workflow</p>
        <h2 id="workflow-heading">Frame the workflow</h2>
        <form onSubmit={(event) => void save(event)} noValidate>
          {action.status === 'error' && (
            <div className="form-error-summary" role="alert">
              <p>{action.message}</p>
            </div>
          )}
          <fieldset>
            <legend>What sets this workflow in motion?</legend>
            <label htmlFor="workflow-trigger">
              Trigger <span aria-hidden="true">*</span>
            </label>
            <input
              id="workflow-trigger"
              value={trigger}
              onChange={(event) => setTrigger(event.target.value)}
              required
            />

            <label htmlFor="workflow-steps">
              Steps, one per line <span aria-hidden="true">*</span>
            </label>
            <textarea
              id="workflow-steps"
              rows={4}
              value={steps}
              onChange={(event) => setSteps(event.target.value)}
              required
            />

            <label htmlFor="workflow-actors">
              Actors, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="workflow-actors"
              rows={2}
              value={actors}
              onChange={(event) => setActors(event.target.value)}
            />

            <label htmlFor="workflow-inputs">
              Inputs, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="workflow-inputs"
              rows={2}
              value={inputs}
              onChange={(event) => setInputs(event.target.value)}
            />

            <label htmlFor="workflow-decisions">
              Decisions, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="workflow-decisions"
              rows={2}
              value={decisions}
              onChange={(event) => setDecisions(event.target.value)}
            />

            <label htmlFor="workflow-systems">
              Systems touched, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="workflow-systems"
              rows={2}
              value={systems}
              onChange={(event) => setSystems(event.target.value)}
            />

            <label htmlFor="workflow-handoffs">
              Handoffs, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="workflow-handoffs"
              rows={2}
              value={handoffs}
              onChange={(event) => setHandoffs(event.target.value)}
            />

            <label htmlFor="workflow-exceptions">
              Exceptions, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="workflow-exceptions"
              rows={2}
              value={exceptions}
              onChange={(event) => setExceptions(event.target.value)}
            />

            <label htmlFor="workflow-outputs">
              Outputs, one per line <span className="optional">(optional)</span>
            </label>
            <textarea
              id="workflow-outputs"
              rows={2}
              value={outputs}
              onChange={(event) => setOutputs(event.target.value)}
            />
          </fieldset>
          <button type="submit" disabled={!isOnline || action.status === 'saving' || etag === null}>
            {action.status === 'saving' ? 'Saving workflow…' : 'Save workflow'}
          </button>
          {action.status === 'success' && (
            <p className="success-message" role="status">
              {action.message}
            </p>
          )}
        </form>
      </section>
      <section className="evidence-pane" aria-labelledby="workflow-list-heading">
        <div className="section-heading compact">
          <div>
            <p className="eyebrow">Framed so far</p>
            <h2 id="workflow-list-heading">Workflows</h2>
          </div>
          <span>{engagement.workflows.length} recorded</span>
        </div>
        {engagement.workflows.length === 0 ? (
          <p>No workflows framed yet. The first one you save appears here.</p>
        ) : (
          <ul className="frame-item-list">
            {engagement.workflows.map((workflow) => (
              <li key={workflow.id}>
                <p className="origin-label">Triggered by</p>
                <blockquote>{workflow.trigger}</blockquote>
                <p>{workflow.steps.length} steps · {workflow.actors.length} actors</p>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

function ProblemStage({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
  candidate,
}: FrameViewProps & { readonly candidate: FrameDraftCandidate | null }) {
  const [workflowId, setWorkflowId] = useState(engagement.workflows[0]?.id ?? '')
  const [user, setUser] = useState('')
  const [goal, setGoal] = useState('')
  const [constraint, setConstraint] = useState('')
  const [impact, setImpact] = useState('')
  const [evidenceReferences, setEvidenceReferences] = useState<readonly string[]>([])
  const [confidence, setConfidence] = useState('0.5')

  // Prefills the form when a new candidate is selected, keyed on the candidate object
  // itself, so it fires once per "Use this" click, not on every render, and never fights the
  // facilitator's own edits afterward. workflowId is deliberately left alone: the candidate's
  // workflow has no real Id yet until the facilitator saves it above.
  useEffect(() => {
    if (candidate === null) return
    setUser(candidate.problem.user)
    setGoal(candidate.problem.goal)
    setConstraint(candidate.problem.constraint)
    setImpact(candidate.problem.impact)
    setEvidenceReferences(candidate.problem.evidenceReferences)
    setConfidence(String(candidate.problem.confidence))
  }, [candidate])
  const [action, setAction] = useState<ActionState>({ status: 'idle' })

  // Re-syncs only when the available workflow list changes (a fresh save unlocks the
  // stage below with a real default), not on every workflowId edit. The facilitator's
  // own selection should never be overridden by this effect.
  useEffect(() => {
    if (!engagement.workflows.some((workflow) => workflow.id === workflowId)) {
      setWorkflowId(engagement.workflows[0]?.id ?? '')
    }
  }, [engagement.workflows])

  const save = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    const confidenceValue = Number(confidence)
    if (
      workflowId.trim() === '' ||
      user.trim() === '' ||
      goal.trim() === '' ||
      !Number.isFinite(confidenceValue) ||
      confidenceValue < 0 ||
      confidenceValue > 1
    ) {
      setAction({
        status: 'error',
        message: 'Choose a workflow, name the user and goal, and enter confidence from 0 to 1.',
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
      const result = await client.addProblem(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          workflowId: workflowId.trim(),
          user: user.trim(),
          goal: goal.trim(),
          constraint: constraint.trim(),
          impact: impact.trim(),
          evidenceReferences,
          confidence: confidenceValue,
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setUser('')
      setGoal('')
      setConstraint('')
      setImpact('')
      setEvidenceReferences([])
      setConfidence('0.5')
      setAction({ status: 'success', message: 'Problem saved.' })
    } catch (error: unknown) {
      const conflict =
        error instanceof ApiRequestError && (error.status === 409 || error.status === 412)
      setAction({
        status: 'error',
        message:
          conflict
            ? 'The engagement changed before this problem was saved. Refresh and try again.'
            : error instanceof ApiRequestError
              ? error.message
              : 'The problem could not be saved. Check the connection and try again.',
        conflict,
      })
    }
  }

  return (
    <div className="workbench-grid frame-stage">
      <section className="capture-pane" aria-labelledby="problem-heading">
        <p className="eyebrow">Stage 2 · Problem</p>
        <h2 id="problem-heading">Name the problem</h2>
        {engagement.workflows.length === 0 ? (
          <p className="inline-notice">Frame a workflow first. Problems attach to a workflow.</p>
        ) : (
          <form onSubmit={(event) => void save(event)} noValidate>
            {action.status === 'error' && (
              <div className="form-error-summary" role="alert">
                <p>{action.message}</p>
              </div>
            )}
            <fieldset>
              <legend>Whose problem, and what stands in the way?</legend>
              <label htmlFor="problem-workflow">
                Workflow <span aria-hidden="true">*</span>
              </label>
              <select
                id="problem-workflow"
                value={workflowId}
                onChange={(event) => setWorkflowId(event.target.value)}
                required
              >
                {engagement.workflows.map((workflow) => (
                  <option key={workflow.id} value={workflow.id}>
                    {workflow.trigger}
                  </option>
                ))}
              </select>

              <label htmlFor="problem-user">
                User <span aria-hidden="true">*</span>
              </label>
              <input
                id="problem-user"
                value={user}
                onChange={(event) => setUser(event.target.value)}
                required
              />

              <label htmlFor="problem-goal">
                Goal <span aria-hidden="true">*</span>
              </label>
              <textarea
                id="problem-goal"
                rows={2}
                value={goal}
                onChange={(event) => setGoal(event.target.value)}
                required
              />

              <label htmlFor="problem-constraint">
                Constraint <span className="optional">(optional)</span>
              </label>
              <textarea
                id="problem-constraint"
                rows={2}
                value={constraint}
                onChange={(event) => setConstraint(event.target.value)}
              />

              <label htmlFor="problem-impact">
                Impact <span className="optional">(optional)</span>
              </label>
              <textarea
                id="problem-impact"
                rows={2}
                value={impact}
                onChange={(event) => setImpact(event.target.value)}
              />

              {engagement.evidence.length > 0 && (
                <>
                  <label htmlFor="problem-evidence">
                    Supporting evidence <span className="optional">(optional)</span>
                  </label>
                  <select
                    id="problem-evidence"
                    multiple
                    size={Math.min(4, engagement.evidence.length)}
                    value={[...evidenceReferences]}
                    onChange={(event) => setEvidenceReferences(selectedOptions(event))}
                  >
                    {engagement.evidence.map((evidence) => (
                      <option key={evidence.id} value={evidence.id}>
                        {evidence.statement.slice(0, 60)}
                      </option>
                    ))}
                  </select>
                </>
              )}

              <label htmlFor="problem-confidence">
                Confidence <span aria-hidden="true">*</span>
              </label>
              <input
                id="problem-confidence"
                type="number"
                min="0"
                max="1"
                step="0.05"
                value={confidence}
                onChange={(event) => setConfidence(event.target.value)}
                required
              />
            </fieldset>
            <button type="submit" disabled={!isOnline || action.status === 'saving' || etag === null}>
              {action.status === 'saving' ? 'Saving problem…' : 'Save problem'}
            </button>
            {action.status === 'success' && (
              <p className="success-message" role="status">
                {action.message}
              </p>
            )}
          </form>
        )}
      </section>
      <section className="evidence-pane" aria-labelledby="problem-list-heading">
        <div className="section-heading compact">
          <div>
            <p className="eyebrow">Framed so far</p>
            <h2 id="problem-list-heading">Problems</h2>
          </div>
          <span>{engagement.problems.length} recorded</span>
        </div>
        {engagement.problems.length === 0 ? (
          <p>No problems named yet. The first one you save appears here.</p>
        ) : (
          <ul className="frame-item-list">
            {engagement.problems.map((problem) => (
              <li key={problem.id}>
                <p className="origin-label">{problem.user}</p>
                <blockquote>{problem.goal}</blockquote>
                <p>Confidence {Math.round(problem.confidence * 100)}%</p>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}

function OpportunityStage({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
}: FrameViewProps) {
  const [problemId, setProblemId] = useState(engagement.problems[0]?.id ?? '')
  const [desiredOutcome, setDesiredOutcome] = useState('')
  const [kpiReference, setKpiReference] = useState('')
  const [owner, setOwner] = useState('')
  const [valueProfile, setValueProfile] = useState('')
  const [confidenceProfile, setConfidenceProfile] = useState('')
  const [trust, setTrust] = useState<TrustProfile>({
    privacyApproved: false,
    securityApproved: false,
    governanceApproved: false,
    humanOversightDefined: false,
    dataSensitivity: '',
    auditability: '',
    modelRisk: '',
    operationalRisk: '',
  })
  const [readiness, setReadiness] = useState<ReadinessProfile>({
    ownerDefined: false,
    kpiDefined: false,
    baselineDefined: false,
    targetDefined: false,
    dataReady: false,
    processStable: false,
    integrationReady: false,
    changeCapacityReady: false,
  })
  const [evidenceReferences, setEvidenceReferences] = useState<readonly string[]>([])
  const [interventionType, setInterventionType] = useState('')
  const [capability, setCapability] = useState('')
  const [workflowChange, setWorkflowChange] = useState('')
  const [technologyPattern, setTechnologyPattern] = useState('')
  const [autonomyLevel, setAutonomyLevel] = useState('human-in-the-loop')
  const [trustImplications, setTrustImplications] = useState('')
  const [dependencies, setDependencies] = useState('')
  const [validationPlan, setValidationPlan] = useState('')
  const [action, setAction] = useState<ActionState>({ status: 'idle' })

  // Re-syncs only when the available problem list changes, not on every problemId
  // edit. The facilitator's own selection should never be overridden by this effect.
  useEffect(() => {
    if (!engagement.problems.some((problem) => problem.id === problemId)) {
      setProblemId(engagement.problems[0]?.id ?? '')
    }
  }, [engagement.problems])

  const selectedProblem = engagement.problems.find((problem) => problem.id === problemId)

  const save = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    if (
      problemId.trim() === '' ||
      selectedProblem === undefined ||
      desiredOutcome.trim() === '' ||
      owner.trim() === ''
    ) {
      setAction({
        status: 'error',
        message: 'Choose a problem and name the desired outcome and owner.',
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
      const concepts =
        capability.trim() === ''
          ? []
          : [
              {
                id: crypto.randomUUID(),
                interventionType: interventionType.trim(),
                capability: capability.trim(),
                workflowChange: workflowChange.trim(),
                technologyPattern: technologyPattern.trim(),
                autonomyLevel: autonomyLevel.trim(),
                trustImplications: linesToList(trustImplications),
                dependencies: linesToList(dependencies),
                assumptionReferences: [],
                validationPlan: validationPlan.trim(),
              },
            ]
      const result = await client.addOpportunity(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          problemId: selectedProblem.id,
          workflowId: selectedProblem.workflowId,
          desiredOutcome: desiredOutcome.trim(),
          kpiReference: kpiReference.trim(),
          owner: owner.trim(),
          valueProfile: valueProfile.trim(),
          confidenceProfile: confidenceProfile.trim(),
          trustProfile: trust,
          readinessProfile: readiness,
          evidenceReferences,
          concepts,
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setDesiredOutcome('')
      setKpiReference('')
      setOwner('')
      setValueProfile('')
      setConfidenceProfile('')
      setEvidenceReferences([])
      setInterventionType('')
      setCapability('')
      setWorkflowChange('')
      setTechnologyPattern('')
      setTrustImplications('')
      setDependencies('')
      setValidationPlan('')
      setAction({ status: 'success', message: 'Opportunity saved.' })
    } catch (error: unknown) {
      const conflict =
        error instanceof ApiRequestError && (error.status === 409 || error.status === 412)
      setAction({
        status: 'error',
        message:
          conflict
            ? 'The engagement changed before this opportunity was saved. Refresh and try again.'
            : error instanceof ApiRequestError
              ? error.message
              : 'The opportunity could not be saved. Check the connection and try again.',
        conflict,
      })
    }
  }

  const trustFields = [
    ['privacyApproved', 'Privacy approved'],
    ['securityApproved', 'Security approved'],
    ['governanceApproved', 'Governance approved'],
    ['humanOversightDefined', 'Human oversight defined'],
  ] as const
  const readinessFields = [
    ['ownerDefined', 'Owner defined'],
    ['kpiDefined', 'KPI defined'],
    ['baselineDefined', 'Baseline defined'],
    ['targetDefined', 'Target defined'],
    ['dataReady', 'Data ready'],
    ['processStable', 'Process stable'],
    ['integrationReady', 'Integration ready'],
    ['changeCapacityReady', 'Change capacity ready'],
  ] as const

  return (
    <div className="workbench-grid frame-stage">
      <section className="capture-pane" aria-labelledby="opportunity-heading">
        <p className="eyebrow">Stage 3 · Opportunity</p>
        <h2 id="opportunity-heading">Define the opportunity</h2>
        {engagement.problems.length === 0 ? (
          <p className="inline-notice">Name a problem first. Opportunities attach to a problem.</p>
        ) : (
          <form onSubmit={(event) => void save(event)} noValidate>
            {action.status === 'error' && (
              <div className="form-error-summary" role="alert">
                <p>{action.message}</p>
              </div>
            )}
            <fieldset>
              <legend>What outcome, and who owns it?</legend>
              <label htmlFor="opportunity-problem">
                Problem <span aria-hidden="true">*</span>
              </label>
              <select
                id="opportunity-problem"
                value={problemId}
                onChange={(event) => setProblemId(event.target.value)}
                required
              >
                {engagement.problems.map((problem) => (
                  <option key={problem.id} value={problem.id}>
                    {problem.goal}
                  </option>
                ))}
              </select>

              <label htmlFor="opportunity-outcome">
                Desired outcome <span aria-hidden="true">*</span>
              </label>
              <textarea
                id="opportunity-outcome"
                rows={2}
                value={desiredOutcome}
                onChange={(event) => setDesiredOutcome(event.target.value)}
                required
              />

              <label htmlFor="opportunity-kpi">
                KPI reference <span className="optional">(optional)</span>
              </label>
              <input
                id="opportunity-kpi"
                value={kpiReference}
                onChange={(event) => setKpiReference(event.target.value)}
              />

              <label htmlFor="opportunity-owner">
                Accountable owner <span aria-hidden="true">*</span>
              </label>
              <input
                id="opportunity-owner"
                value={owner}
                onChange={(event) => setOwner(event.target.value)}
                required
              />

              <label htmlFor="opportunity-value">
                Value profile <span className="optional">(optional)</span>
              </label>
              <input
                id="opportunity-value"
                value={valueProfile}
                onChange={(event) => setValueProfile(event.target.value)}
              />

              <label htmlFor="opportunity-confidence-profile">
                Confidence profile <span className="optional">(optional)</span>
              </label>
              <input
                id="opportunity-confidence-profile"
                value={confidenceProfile}
                onChange={(event) => setConfidenceProfile(event.target.value)}
              />

              {engagement.evidence.length > 0 && (
                <>
                  <label htmlFor="opportunity-evidence">
                    Supporting evidence <span className="optional">(optional)</span>
                  </label>
                  <select
                    id="opportunity-evidence"
                    multiple
                    size={Math.min(4, engagement.evidence.length)}
                    value={[...evidenceReferences]}
                    onChange={(event) => setEvidenceReferences(selectedOptions(event))}
                  >
                    {engagement.evidence.map((evidence) => (
                      <option key={evidence.id} value={evidence.id}>
                        {evidence.statement.slice(0, 60)}
                      </option>
                    ))}
                  </select>
                </>
              )}
            </fieldset>

            <fieldset>
              <legend>Candidate concept</legend>
              <p className="field-hint">
                Naming at least one candidate lets the AI review brief cite it. Leave
                capability blank to skip and frame concepts later.
              </p>
              <label htmlFor="concept-capability">
                Capability <span className="optional">(optional)</span>
              </label>
              <input
                id="concept-capability"
                value={capability}
                onChange={(event) => setCapability(event.target.value)}
              />

              <label htmlFor="concept-intervention-type">
                Intervention type <span className="optional">(optional)</span>
              </label>
              <input
                id="concept-intervention-type"
                value={interventionType}
                onChange={(event) => setInterventionType(event.target.value)}
              />

              <label htmlFor="concept-workflow-change">
                Workflow change <span className="optional">(optional)</span>
              </label>
              <input
                id="concept-workflow-change"
                value={workflowChange}
                onChange={(event) => setWorkflowChange(event.target.value)}
              />

              <label htmlFor="concept-technology-pattern">
                Technology pattern <span className="optional">(optional)</span>
              </label>
              <input
                id="concept-technology-pattern"
                value={technologyPattern}
                onChange={(event) => setTechnologyPattern(event.target.value)}
              />

              <label htmlFor="concept-autonomy-level">
                Autonomy level <span className="optional">(optional)</span>
              </label>
              <input
                id="concept-autonomy-level"
                value={autonomyLevel}
                onChange={(event) => setAutonomyLevel(event.target.value)}
              />

              <label htmlFor="concept-trust-implications">
                Trust implications, one per line{' '}
                <span className="optional">(optional)</span>
              </label>
              <textarea
                id="concept-trust-implications"
                rows={2}
                value={trustImplications}
                onChange={(event) => setTrustImplications(event.target.value)}
              />

              <label htmlFor="concept-dependencies">
                Dependencies, one per line <span className="optional">(optional)</span>
              </label>
              <textarea
                id="concept-dependencies"
                rows={2}
                value={dependencies}
                onChange={(event) => setDependencies(event.target.value)}
              />

              <label htmlFor="concept-validation-plan">
                Validation plan <span className="optional">(optional)</span>
              </label>
              <textarea
                id="concept-validation-plan"
                rows={2}
                value={validationPlan}
                onChange={(event) => setValidationPlan(event.target.value)}
              />
            </fieldset>

            <fieldset>
              <legend>Trust controls</legend>
              <div className="checkbox-grid">
                {trustFields.map(([field, label]) => (
                  <label key={field} htmlFor={`trust-${field}`}>
                    <input
                      id={`trust-${field}`}
                      type="checkbox"
                      checked={trust[field]}
                      onChange={(event) =>
                        setTrust((current) => ({ ...current, [field]: event.target.checked }))
                      }
                    />
                    {label}
                  </label>
                ))}
              </div>
              <label htmlFor="trust-data-sensitivity">Data sensitivity</label>
              <input
                id="trust-data-sensitivity"
                value={trust.dataSensitivity}
                onChange={(event) =>
                  setTrust((current) => ({ ...current, dataSensitivity: event.target.value }))
                }
              />
              <label htmlFor="trust-auditability">Auditability</label>
              <input
                id="trust-auditability"
                value={trust.auditability}
                onChange={(event) =>
                  setTrust((current) => ({ ...current, auditability: event.target.value }))
                }
              />
              <label htmlFor="trust-model-risk">Model risk</label>
              <input
                id="trust-model-risk"
                value={trust.modelRisk}
                onChange={(event) =>
                  setTrust((current) => ({ ...current, modelRisk: event.target.value }))
                }
              />
              <label htmlFor="trust-operational-risk">Operational risk</label>
              <input
                id="trust-operational-risk"
                value={trust.operationalRisk}
                onChange={(event) =>
                  setTrust((current) => ({ ...current, operationalRisk: event.target.value }))
                }
              />
            </fieldset>

            <fieldset>
              <legend>Delivery readiness</legend>
              <div className="checkbox-grid">
                {readinessFields.map(([field, label]) => (
                  <label key={field} htmlFor={`readiness-${field}`}>
                    <input
                      id={`readiness-${field}`}
                      type="checkbox"
                      checked={readiness[field]}
                      onChange={(event) =>
                        setReadiness((current) => ({ ...current, [field]: event.target.checked }))
                      }
                    />
                    {label}
                  </label>
                ))}
              </div>
            </fieldset>

            <button type="submit" disabled={!isOnline || action.status === 'saving' || etag === null}>
              {action.status === 'saving' ? 'Saving opportunity…' : 'Save opportunity'}
            </button>
            {action.status === 'success' && (
              <p className="success-message" role="status">
                {action.message}
              </p>
            )}
          </form>
        )}
      </section>
      <section className="evidence-pane" aria-labelledby="opportunity-list-heading">
        <div className="section-heading compact">
          <div>
            <p className="eyebrow">Framed so far</p>
            <h2 id="opportunity-list-heading">Opportunities</h2>
          </div>
          <span>{engagement.opportunities.length} recorded</span>
        </div>
        {engagement.opportunities.length === 0 ? (
          <p>No opportunities defined yet. The first one you save appears here.</p>
        ) : (
          <ul className="frame-item-list">
            {engagement.opportunities.map((opportunity) => (
              <li key={opportunity.id}>
                <p className="origin-label">{opportunity.owner}</p>
                <blockquote>{opportunity.desiredOutcome}</blockquote>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
