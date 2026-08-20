import { useState, type FormEvent } from 'react'
import { ROLE_STARTS } from '../app/names'
import { lifecycleLabel } from '../app/labels'
import { connectWorkspace } from '../app/routing'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import type { Engagement } from '../api/contracts'

export function HomeView({
  hasSelection,
  usingSameOrigin,
  client,
}: {
  readonly hasSelection: boolean
  readonly usingSameOrigin: boolean
  readonly client: OpportunityApiClient | null
}) {
  return (
    <>
      <section className="home-hero">
        <div>
          <p className="eyebrow">Evidence-led opportunity decisions</p>
          <h1>Move from workshop evidence to an accountable decision.</h1>
        </div>
        <p className="hero-note">
          One shared record connects what people said, what the organization can
          support, and what leaders approved.
        </p>
      </section>

      {!hasSelection && <WorkspaceSetup usingSameOrigin={usingSameOrigin} client={client} />}

      <section className="role-section" aria-labelledby="role-heading">
        <div className="section-heading">
          <p className="eyebrow">Start by role</p>
          <h2 id="role-heading">Choose what you need to do</h2>
        </div>
        <div className="role-list">
          {ROLE_STARTS.map((role) => (
            <article key={role.title} className="role-item">
              <div>
                <h3>{role.title}</h3>
                <p>{role.description}</p>
              </div>
              <a href={role.href}>{role.action}</a>
            </article>
          ))}
        </div>
      </section>
    </>
  )
}

type BrowseState =
  | { readonly status: 'idle' }
  | { readonly status: 'loading' }
  | { readonly status: 'loaded'; readonly engagements: readonly Engagement[] }
  | { readonly status: 'error'; readonly message: string }

export function WorkspaceSetup({
  usingSameOrigin,
  client,
}: {
  readonly usingSameOrigin: boolean
  readonly client: OpportunityApiClient | null
}) {
  const [workspaceId, setWorkspaceId] = useState('')
  const [engagementId, setEngagementId] = useState('')
  const [browse, setBrowse] = useState<BrowseState>({ status: 'idle' })
  const trimmedWorkspaceId = workspaceId.trim()

  const connect = (event: FormEvent<HTMLFormElement>): void => {
    event.preventDefault()
    connectWorkspace({
      workspaceId: trimmedWorkspaceId,
      engagementId: engagementId.trim(),
    })
  }

  const browseWorkshops = async (): Promise<void> => {
    if (client === null || trimmedWorkspaceId === '') return
    setBrowse({ status: 'loading' })
    try {
      const result = await client.listEngagements(trimmedWorkspaceId)
      setBrowse({ status: 'loaded', engagements: result.data })
    } catch (error: unknown) {
      setBrowse({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not list workshops.',
      })
    }
  }

  return (
    <>
      <section className="setup-panel" aria-labelledby="setup-heading">
        <div>
          <p className="eyebrow">Open an engagement</p>
          <h2 id="setup-heading">Find your engagement</h2>
          <p>
            Enter the references supplied by your workshop administrator, or browse
            the workshops already started for your organization. They stay in the
            page address and are not saved in browser storage.
          </p>
          {usingSameOrigin && (
            <p className="setup-note" role="status">
              This site is ready to open an engagement.
            </p>
          )}
        </div>
        <form onSubmit={connect}>
          <label htmlFor="workspace-id">Organization reference</label>
          <input
            id="workspace-id"
            value={workspaceId}
            onChange={(event) => setWorkspaceId(event.target.value)}
            autoComplete="off"
            required
          />
          <label htmlFor="engagement-id">Engagement reference</label>
          <input
            id="engagement-id"
            value={engagementId}
            onChange={(event) => setEngagementId(event.target.value)}
            autoComplete="off"
            required
          />
          <button type="submit">Open engagement</button>
          <button
            type="button"
            className="button-secondary"
            disabled={client === null || trimmedWorkspaceId === '' || browse.status === 'loading'}
            onClick={() => void browseWorkshops()}
          >
            {browse.status === 'loading' ? 'Loading…' : 'Browse workshops'}
          </button>
          {browse.status === 'error' && <p className="form-error-summary">{browse.message}</p>}
          {browse.status === 'loaded' &&
            (browse.engagements.length === 0 ? (
              <p className="setup-note">No workshops found for this organization reference yet.</p>
            ) : (
              <ul className="workshop-browse-list">
                {browse.engagements.map((engagement) => (
                  <li key={engagement.id}>
                    <div>
                      <strong>{engagement.id}</strong>
                      <span className="card-type-badge">{lifecycleLabel(engagement.lifecycleState)}</span>
                    </div>
                    <p>Owner: {engagement.owner}</p>
                    <button
                      type="button"
                      onClick={() =>
                        connectWorkspace({ workspaceId: trimmedWorkspaceId, engagementId: engagement.id })
                      }
                    >
                      Open
                    </button>
                  </li>
                ))}
              </ul>
            ))}
        </form>
      </section>
      <CreateWorkshopForm client={client} />
    </>
  )
}

function slugify(value: string): string {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-+|-+$)/g, '')
}

function linesToList(value: string): readonly string[] {
  return value
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line !== '')
}

type CreateWorkshopState =
  | { readonly status: 'idle' }
  | { readonly status: 'creating' }
  | { readonly status: 'error'; readonly message: string }

function CreateWorkshopForm({ client }: { readonly client: OpportunityApiClient | null }) {
  const [workspaceId, setWorkspaceId] = useState('')
  const [name, setName] = useState('')
  const [owner, setOwner] = useState('')
  const [governanceOwner, setGovernanceOwner] = useState('')
  const [objectives, setObjectives] = useState('')
  const [participants, setParticipants] = useState('')
  const [state, setState] = useState<CreateWorkshopState>({ status: 'idle' })

  const trimmedWorkspaceId = workspaceId.trim()
  const trimmedName = name.trim()
  const trimmedOwner = owner.trim()
  const trimmedGovernanceOwner = governanceOwner.trim()
  const canSubmit =
    trimmedWorkspaceId !== '' && trimmedName !== '' && trimmedOwner !== '' && trimmedGovernanceOwner !== ''

  const create = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    if (client === null || !canSubmit) return
    setState({ status: 'creating' })
    try {
      const engagementId = `${slugify(trimmedName)}-${Date.now()}`
      const result = await client.createEngagement(trimmedWorkspaceId, {
        engagementId,
        methodVersion: '1.0',
        owner: trimmedOwner,
        governanceOwner: trimmedGovernanceOwner,
        objectives: linesToList(objectives),
        participants: linesToList(participants),
      })
      connectWorkspace({ workspaceId: trimmedWorkspaceId, engagementId: result.data.id })
    } catch (error: unknown) {
      setState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not start the workshop.',
      })
    }
  }

  return (
    <section className="setup-panel" aria-labelledby="create-heading">
      <div>
        <p className="eyebrow">Start fresh</p>
        <h2 id="create-heading">Start a new workshop</h2>
        <p>
          Mint a new engagement and open it right away. Objectives and participants
          can only be set here: there's no way to add them later, so capture what
          you already know now and refine the rest as the workshop unfolds.
        </p>
      </div>
      <form onSubmit={(event) => void create(event)}>
        <label htmlFor="create-workspace-id">Organization reference</label>
        <input
          id="create-workspace-id"
          value={workspaceId}
          onChange={(event) => setWorkspaceId(event.target.value)}
          autoComplete="off"
          required
        />
        <label htmlFor="create-name">Workshop name</label>
        <input
          id="create-name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          autoComplete="off"
          required
        />
        <label htmlFor="create-owner">Owner</label>
        <input
          id="create-owner"
          value={owner}
          onChange={(event) => setOwner(event.target.value)}
          autoComplete="off"
          required
        />
        <label htmlFor="create-governance-owner">Governance owner</label>
        <input
          id="create-governance-owner"
          value={governanceOwner}
          onChange={(event) => setGovernanceOwner(event.target.value)}
          autoComplete="off"
          required
        />
        <label htmlFor="create-objectives">Objectives (optional, one per line)</label>
        <textarea
          id="create-objectives"
          value={objectives}
          onChange={(event) => setObjectives(event.target.value)}
        />
        <label htmlFor="create-participants">Participants (optional, one per line)</label>
        <textarea
          id="create-participants"
          value={participants}
          onChange={(event) => setParticipants(event.target.value)}
        />
        {state.status === 'error' && <p className="form-error-summary">{state.message}</p>}
        <button type="submit" disabled={client === null || state.status === 'creating' || !canSubmit}>
          {state.status === 'creating' ? 'Starting…' : 'Create workshop'}
        </button>
      </form>
    </section>
  )
}
