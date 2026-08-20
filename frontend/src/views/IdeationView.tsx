import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import qrcode from 'qrcode-generator'
import { useEffect, useMemo, useRef, useState } from 'react'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import type { Engagement, LiveIdeationNote } from '../api/contracts'

type IdeationViewProps = {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly etag: string | null
  readonly isOnline: boolean
  readonly onUpdated: (engagement: Engagement, etag: string | null) => void
}

type IdeationSessionHandle = {
  readonly id: string
  readonly joinCode: string
}

type IdeationSessionState =
  | { readonly status: 'idle' }
  | { readonly status: 'starting' }
  | { readonly status: 'live' }
  | { readonly status: 'curating'; readonly noteId: string }
  | { readonly status: 'error'; readonly message: string }

/**
 * Mirrors DiscoveryCardsView's useLiveVoteSession/LiveVoteSessionPanel shape: same
 * start/QR/reconnect/stop mechanics, an engagement-wide round (no journey step) instead of a
 * per-step vote, and "curate a note" instead of "promote a card."
 */
function useLiveIdeationSession({
  client,
  workspaceId,
  engagement,
  etag,
  onUpdated,
}: IdeationViewProps) {
  const [session, setSession] = useState<IdeationSessionHandle | null>(null)
  const [notes, setNotes] = useState<readonly LiveIdeationNote[]>([])
  const [presenceCount, setPresenceCount] = useState(0)
  const [curatedNoteIds, setCuratedNoteIds] = useState<ReadonlySet<string>>(new Set())
  const [state, setState] = useState<IdeationSessionState>({ status: 'idle' })
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => () => void connectionRef.current?.stop(), [])

  const start = async (): Promise<void> => {
    setState({ status: 'starting' })
    try {
      const result = await client.startIdeationSession(workspaceId, engagement.id)
      const token = await client.getAccessToken()
      const connection = new HubConnectionBuilder()
        .withUrl(new URL('/hubs/collaboration', client.apiBaseUrl).toString(), {
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build()
      connection.on('IdeationBoardUpdated', (nextNotes: readonly LiveIdeationNote[]) => setNotes(nextNotes))
      connection.on('PresenceUpdated', (count: number) => setPresenceCount(count))
      connection.onreconnected(() => void connection.invoke('JoinSession', result.data.id))
      await connection.start()
      await connection.invoke('JoinSession', result.data.id)
      connectionRef.current = connection
      setSession({ id: result.data.id, joinCode: result.data.joinCode })
      setNotes([])
      setPresenceCount(0)
      setCuratedNoteIds(new Set())
      setState({ status: 'live' })
    } catch (error: unknown) {
      setState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not start an ideation round.',
      })
    }
  }

  const stop = async (): Promise<void> => {
    const closingSessionId = session?.id
    void connectionRef.current?.stop()
    connectionRef.current = null
    setSession(null)
    setNotes([])
    setPresenceCount(0)
    setState({ status: 'idle' })
    if (closingSessionId !== undefined) {
      try {
        await client.closeLiveSession(workspaceId, engagement.id, closingSessionId)
      } catch {
        // Best-effort: the facilitator's own view has already moved on.
      }
    }
  }

  const curate = async (note: LiveIdeationNote): Promise<void> => {
    if (session === null || etag === null) return
    setState({ status: 'curating', noteId: note.id })
    try {
      const result = await client.curateIdeationNote(workspaceId, engagement.id, session.id, note.id, etag)
      onUpdated(result.data, result.etag)
      setCuratedNoteIds((current) => new Set(current).add(note.id))
      setState({ status: 'live' })
    } catch (error: unknown) {
      setState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not curate this idea.',
      })
    }
  }

  return { session, notes, presenceCount, curatedNoteIds, state, start, stop, curate }
}

function IdeationSessionPanel({
  session,
  notes,
  presenceCount,
  curatedNoteIds,
  state,
  start,
  stop,
  curate,
  isOnline,
}: {
  readonly session: IdeationSessionHandle | null
  readonly notes: readonly LiveIdeationNote[]
  readonly presenceCount: number
  readonly curatedNoteIds: ReadonlySet<string>
  readonly state: IdeationSessionState
  readonly start: () => void | Promise<void>
  readonly stop: () => void | Promise<void>
  readonly curate: (note: LiveIdeationNote) => void | Promise<void>
  readonly isOnline: boolean
}) {
  const [copied, setCopied] = useState(false)
  const joinUrl =
    session === null ? null : `${window.location.origin}${window.location.pathname}#/join/${session.joinCode}`
  const joinQrDataUrl = useMemo(() => {
    if (joinUrl === null) return null
    const qr = qrcode(0, 'M')
    qr.addData(joinUrl)
    qr.make()
    return qr.createDataURL(6, 8)
  }, [joinUrl])

  const copyJoinUrl = async (): Promise<void> => {
    if (joinUrl === null) return
    try {
      await navigator.clipboard.writeText(joinUrl)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard access can be denied by the browser sandbox; the code is still visible to read aloud.
    }
  }

  if (session === null) {
    return (
      <div className="live-vote-session-panel">
        <p className="discovery-cards-count">
          Start a round and share the join link: participants add ideas live, and you pick
          which ones seed the Frame draft.
        </p>
        {state.status === 'error' && <p className="form-error-summary">{state.message}</p>}
        <button type="button" onClick={() => void start()} disabled={!isOnline || state.status === 'starting'}>
          {state.status === 'starting' ? 'Starting…' : 'Start an ideation round'}
        </button>
      </div>
    )
  }

  return (
    <div className="live-vote-session-panel">
      <p className="discovery-cards-count">
        Live ideation round · join code <strong>{session.joinCode}</strong> ·{' '}
        {presenceCount} {presenceCount === 1 ? 'person' : 'people'} joined
      </p>
      {joinUrl !== null && (
        <div className="live-vote-join-block">
          {joinQrDataUrl !== null && (
            <img
              className="live-vote-join-qr"
              src={joinQrDataUrl}
              alt={`QR code that opens the join link for code ${session.joinCode}`}
              width={132}
              height={132}
            />
          )}
          <div className="live-vote-join-row">
            <input readOnly value={joinUrl} onFocus={(event) => event.target.select()} />
            <button type="button" className="button-secondary" onClick={() => void copyJoinUrl()}>
              {copied ? 'Copied' : 'Copy link'}
            </button>
          </div>
        </div>
      )}
      {notes.length === 0 ? (
        <p className="discovery-cards-count">No ideas yet. They'll appear here as the room submits them.</p>
      ) : (
        <ul className="live-vote-leaderboard">
          {notes.map((note) => {
            const curated = curatedNoteIds.has(note.id)
            return (
              <li key={note.id}>
                <span>{note.text}</span>
                <button
                  type="button"
                  onClick={() => void curate(note)}
                  disabled={!isOnline || curated || (state.status === 'curating' && state.noteId === note.id)}
                >
                  {curated
                    ? '✓ Curated'
                    : state.status === 'curating' && state.noteId === note.id
                      ? 'Curating…'
                      : 'Curate'}
                </button>
              </li>
            )
          })}
        </ul>
      )}
      {state.status === 'error' && <p className="form-error-summary">{state.message}</p>}
      <button type="button" className="button-secondary" onClick={() => void stop()}>
        Close ideation round
      </button>
    </div>
  )
}

export function IdeationView(props: IdeationViewProps) {
  const { engagement, isOnline } = props
  const live = useLiveIdeationSession(props)

  return (
    <section className="page discovery-cards-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Ideation</p>
          <h1>Brainstorm before you frame it</h1>
          <p>
            Open a round and let the room add raw ideas off the evidence you've captured. Curate
            the ones worth keeping: they carry forward as framing context when you draft the
            workflow and problem.
          </p>
        </div>
      </header>

      <IdeationSessionPanel
        session={live.session}
        notes={live.notes}
        presenceCount={live.presenceCount}
        curatedNoteIds={live.curatedNoteIds}
        state={live.state}
        start={live.start}
        stop={live.stop}
        curate={live.curate}
        isOnline={isOnline}
      />

      <div className="section-heading compact">
        <div>
          <p className="eyebrow">Curated so far</p>
          <h3>Ideas informing the frame</h3>
        </div>
        <span>{engagement.ideationNotes.length} total</span>
      </div>
      {engagement.ideationNotes.length === 0 ? (
        <p className="discovery-cards-count">
          No ideas curated yet. Start a round above, or move on to Frame whenever you're ready.
        </p>
      ) : (
        <ul className="live-vote-leaderboard">
          {engagement.ideationNotes.map((note) => (
            <li key={note.id}>
              <span>{note.text}</span>
              <span className="origin-label">{note.submittedBy}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
