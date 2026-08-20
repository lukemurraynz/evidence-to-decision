import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import qrcode from 'qrcode-generator'
import { useEffect, useMemo, useRef, useState } from 'react'
import { EmptyState } from '../components/AsyncStates'
import { FlipCard } from '../components/FlipCard'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import type {
  CardPinTally,
  CardVoteTally,
  DiscoveryCardSuggestionResult,
  Engagement,
} from '../api/contracts'
import {
  discoveryCardCategories,
  discoveryCards,
  filterDiscoveryCards,
  type DiscoveryCard,
} from '../data/discoveryCards'

// Categories are grouped into a small set of icon families so the deck reads with
// real visual variety at a glance, rather than one repeated glyph across 79 cards.
const ICON_FAMILY_BY_CATEGORY: Readonly<Record<string, string>> = {
  agentic: 'spark',
  'decision-making': 'spark',
  communication: 'chat',
  'speech-recognition': 'chat',
  'content-creation': 'pen',
  'text-processing': 'pen',
  'data-and-predictive-analytics': 'chart',
  'information-management': 'chart',
  'navigation-and-control': 'compass',
  'environmental-awareness': 'compass',
  'visual-perception': 'eye',
  'task-automation': 'gear',
  'process-optimization': 'gear',
}

function DiscoveryCardIcon({ categoryId }: { readonly categoryId: string }) {
  const family = ICON_FAMILY_BY_CATEGORY[categoryId] ?? 'spark'
  if (family === 'chat') {
    return (
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M4 6.5A2.5 2.5 0 0 1 6.5 4h11A2.5 2.5 0 0 1 20 6.5v7A2.5 2.5 0 0 1 17.5 16H10l-4 4v-4H6.5A2.5 2.5 0 0 1 4 13.5v-7Z"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinejoin="round"
        />
      </svg>
    )
  }
  if (family === 'pen') {
    return (
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="m14.5 5.5 4 4L8 20H4v-4l10.5-10.5Z"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinejoin="round"
        />
      </svg>
    )
  }
  if (family === 'chart') {
    return (
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M5 19V10M12 19V5M19 19v-6"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
        />
      </svg>
    )
  }
  if (family === 'compass') {
    return (
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="12" cy="12" r="8" stroke="currentColor" strokeWidth="2" />
        <path
          d="m14.5 9.5-1.8 4.7a1 1 0 0 1-.5.5L7.5 16.5l1.8-4.7a1 1 0 0 1 .5-.5l4.7-1.8Z"
          stroke="currentColor"
          strokeWidth="1.6"
          strokeLinejoin="round"
        />
      </svg>
    )
  }
  if (family === 'eye') {
    return (
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M2.5 12S6 5.5 12 5.5 21.5 12 21.5 12 18 18.5 12 18.5 2.5 12 2.5 12Z"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinejoin="round"
        />
        <circle cx="12" cy="12" r="2.6" stroke="currentColor" strokeWidth="2" />
      </svg>
    )
  }
  if (family === 'gear') {
    return (
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="12" cy="12" r="3" stroke="currentColor" strokeWidth="2" />
        <path
          d="M12 3v2.4M12 18.6V21M21 12h-2.4M5.4 12H3M18.1 5.9l-1.7 1.7M7.6 16.5l-1.7 1.7M18.1 18.1l-1.7-1.7M7.6 7.5 5.9 5.8"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
        />
      </svg>
    )
  }
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path
        d="M12 3a6 6 0 0 0-3.5 10.9c.5.36.8.9.8 1.5V17h5.4v-1.6c0-.6.3-1.14.8-1.5A6 6 0 0 0 12 3Z"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinejoin="round"
      />
      <path d="M9.5 20h5" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
  )
}

type DiscoveryCardsViewProps = {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly etag: string | null
  readonly isOnline: boolean
  readonly onUpdated: (engagement: Engagement, etag: string | null) => void
}

export type JourneyStepOption = {
  readonly id: string
  readonly label: string
}

export function useJourneyStepOptions(engagement: Engagement): readonly JourneyStepOption[] {
  return useMemo(() => {
    return engagement.journeyMaps.flatMap((journeyMap) => {
      const persona = engagement.personas.find((item) => item.id === journeyMap.personaId)
      return journeyMap.steps.map((step) => ({
        id: step.id,
        label: `${persona?.name ?? 'Persona'} · ${step.name}`,
      }))
    })
  }, [engagement.journeyMaps, engagement.personas])
}

function ShortlistAction({
  cardId,
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
  stepOptions,
}: DiscoveryCardsViewProps & {
  readonly cardId: string
  readonly stepOptions: readonly JourneyStepOption[]
}) {
  const [open, setOpen] = useState(false)
  const [journeyStepId, setJourneyStepId] = useState(stepOptions[0]?.id ?? '')
  const [rationale, setRationale] = useState('')

  useEffect(() => {
    const firstOption = stepOptions[0]
    if (firstOption === undefined) return
    if (stepOptions.some((option) => option.id === journeyStepId)) return
    setJourneyStepId(firstOption.id)
  }, [stepOptions, journeyStepId])
  const [state, setState] = useState<
    | { readonly status: 'idle' }
    | { readonly status: 'saving' }
    | { readonly status: 'error'; readonly message: string }
  >({ status: 'idle' })

  if (!open) {
    return (
      <button
        type="button"
        className="button-secondary card-shortlist-toggle"
        onClick={() => setOpen(true)}
        disabled={stepOptions.length === 0}
        title={stepOptions.length === 0 ? 'Map a persona and journey first' : undefined}
      >
        Shortlist for a journey step
      </button>
    )
  }

  const save = async (): Promise<void> => {
    if (journeyStepId === '' || etag === null) return
    setState({ status: 'saving' })
    try {
      const result = await client.addCardShortlistEntry(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          journeyStepId,
          discoveryCardId: cardId,
          rationale: rationale.trim(),
          rank: engagement.cardShortlist.length + 1,
          facilitatorSelected: false,
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setOpen(false)
      setRationale('')
    } catch (error: unknown) {
      setState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not save the shortlist entry.',
      })
    }
  }

  return (
    <div className="card-shortlist-form">
      <label htmlFor={`shortlist-step-${cardId}`}>Journey step</label>
      <select
        id={`shortlist-step-${cardId}`}
        value={journeyStepId}
        onChange={(event) => setJourneyStepId(event.target.value)}
      >
        {stepOptions.map((option) => (
          <option key={option.id} value={option.id}>
            {option.label}
          </option>
        ))}
      </select>
      <label htmlFor={`shortlist-rationale-${cardId}`}>Why this card fits</label>
      <input
        id={`shortlist-rationale-${cardId}`}
        value={rationale}
        onChange={(event) => setRationale(event.target.value)}
      />
      {state.status === 'error' && <p className="form-error-summary">{state.message}</p>}
      <div className="card-shortlist-form-actions">
        <button type="button" onClick={() => void save()} disabled={!isOnline || state.status === 'saving'}>
          {state.status === 'saving' ? 'Saving…' : 'Add to shortlist'}
        </button>
        <button type="button" className="button-secondary" onClick={() => setOpen(false)}>
          Cancel
        </button>
      </div>
    </div>
  )
}

type SuggestionState =
  | { readonly status: 'idle' }
  | { readonly status: 'loading' }
  | { readonly status: 'loaded'; readonly result: DiscoveryCardSuggestionResult }
  | { readonly status: 'error'; readonly message: string }

/**
 * Advisory only: the agent never writes to the canonical graph. Each suggestion goes through
 * the exact same addCardShortlistEntry call the manual ShortlistAction form uses, so a
 * facilitator adding one during a live session gets the same real-time broadcast to the room.
 */
function AiCardSuggestionPanel({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  journeyStepId,
  onUpdated,
}: DiscoveryCardsViewProps & { readonly journeyStepId: string }) {
  const [state, setState] = useState<SuggestionState>({ status: 'idle' })
  const [addingCardId, setAddingCardId] = useState<string | null>(null)
  const [addedCardIds, setAddedCardIds] = useState<ReadonlySet<string>>(new Set())

  const ask = async (): Promise<void> => {
    if (journeyStepId === '') return
    setState({ status: 'loading' })
    try {
      const candidates = discoveryCards.map((card) => ({
        id: card.id,
        displayName: card.displayName,
        categoryId: card.categoryId,
        description: card.description,
      }))
      const result = await client.suggestDiscoveryCards(workspaceId, engagement.id, journeyStepId, candidates)
      setAddedCardIds(new Set())
      setState({ status: 'loaded', result: result.data })
    } catch (error: unknown) {
      setState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not get suggestions.',
      })
    }
  }

  const addSuggestion = async (discoveryCardId: string, rationale: string): Promise<void> => {
    if (etag === null) return
    setAddingCardId(discoveryCardId)
    try {
      const result = await client.addCardShortlistEntry(
        workspaceId,
        engagement.id,
        {
          id: crypto.randomUUID(),
          journeyStepId,
          discoveryCardId,
          rationale,
          rank: engagement.cardShortlist.length + 1,
          facilitatorSelected: false,
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setAddedCardIds((current) => new Set(current).add(discoveryCardId))
    } catch {
      // Best-effort: the suggestion stays visible so the facilitator can just try again.
    } finally {
      setAddingCardId(null)
    }
  }

  if (state.status === 'idle' || state.status === 'error') {
    return (
      <div className="ai-suggestion-panel">
        <button
          type="button"
          className="button-secondary"
          onClick={() => void ask()}
          disabled={!isOnline || journeyStepId === ''}
        >
          Ask AI for card suggestions
        </button>
        {state.status === 'error' && <p className="form-error-summary">{state.message}</p>}
      </div>
    )
  }

  if (state.status === 'loading') {
    return (
      <div className="ai-suggestion-panel">
        <p className="discovery-cards-count">Asking for suggestions…</p>
      </div>
    )
  }

  const { result } = state
  return (
    <div className="ai-suggestion-panel">
      <div className="section-heading compact">
        <div>
          <p className="eyebrow">AI suggested</p>
          <h3>Cards worth considering</h3>
        </div>
        <button type="button" className="button-secondary" onClick={() => void ask()} disabled={!isOnline}>
          Ask again
        </button>
      </div>
      {result.suggestions.length === 0 ? (
        <p className="discovery-cards-count">No suggestions this time. {result.requiredReview}</p>
      ) : (
        <ul className="ai-suggestion-list">
          {result.suggestions.map((suggestion) => {
            const card = discoveryCards.find((item) => item.id === suggestion.discoveryCardId)
            if (card === undefined) return null
            const added = addedCardIds.has(suggestion.discoveryCardId)
            return (
              <li key={suggestion.discoveryCardId}>
                <strong>{card.displayName}</strong>
                <p>{suggestion.rationale}</p>
                <button
                  type="button"
                  onClick={() => void addSuggestion(suggestion.discoveryCardId, suggestion.rationale)}
                  disabled={!isOnline || added || addingCardId === suggestion.discoveryCardId}
                >
                  {added ? '✓ Added' : addingCardId === suggestion.discoveryCardId ? 'Adding…' : 'Add to shortlist'}
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}

type LiveVoteSessionHandle = {
  readonly id: string
  readonly joinCode: string
  readonly journeyStepId: string
}

type LiveVoteSessionState =
  | { readonly status: 'idle' }
  | { readonly status: 'starting' }
  | { readonly status: 'live' }
  | { readonly status: 'promoting'; readonly discoveryCardId: string }
  | { readonly status: 'promoted'; readonly cardName: string; readonly stepLabel: string }
  | { readonly status: 'error'; readonly message: string }

/**
 * Session-level live voting: the facilitator picks a journey step once, and every Discovery
 * Card becomes votable against it for the life of the session. Replaces the old per-card
 * entry point, where starting a vote meant picking one card up front and only that card was
 * ever votable.
 */
function useLiveVoteSession({
  client,
  workspaceId,
  engagement,
  etag,
  onUpdated,
  stepOptions,
}: DiscoveryCardsViewProps & { readonly stepOptions: readonly JourneyStepOption[] }) {
  const [journeyStepId, setJourneyStepId] = useState(stepOptions[0]?.id ?? '')
  const [session, setSession] = useState<LiveVoteSessionHandle | null>(null)
  const [tally, setTally] = useState<readonly CardVoteTally[]>([])
  const [pinTally, setPinTally] = useState<readonly CardPinTally[]>([])
  const [presenceCount, setPresenceCount] = useState(0)
  const [state, setState] = useState<LiveVoteSessionState>({ status: 'idle' })
  const [startPrivate, setStartPrivate] = useState(false)
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    const firstOption = stepOptions[0]
    if (firstOption === undefined) return
    if (stepOptions.some((option) => option.id === journeyStepId)) return
    setJourneyStepId(firstOption.id)
  }, [stepOptions, journeyStepId])

  useEffect(() => () => void connectionRef.current?.stop(), [])

  const start = async (): Promise<void> => {
    if (journeyStepId === '') return
    setState({ status: 'starting' })
    try {
      const result = await client.startLiveSession(workspaceId, engagement.id, journeyStepId, startPrivate)
      const token = await client.getAccessToken()
      const connection = new HubConnectionBuilder()
        .withUrl(new URL('/hubs/collaboration', client.apiBaseUrl).toString(), {
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build()
      connection.on('VoteTallyUpdated', (nextTally: readonly CardVoteTally[]) => setTally(nextTally))
      connection.on('PinTallyUpdated', (nextTally: readonly CardPinTally[]) => setPinTally(nextTally))
      connection.on('PresenceUpdated', (count: number) => setPresenceCount(count))
      // Automatic reconnect gets a new underlying connection with empty group membership;
      // without re-joining here, the facilitator's own leaderboard silently stops updating
      // after the first reconnect over the course of a longer live session.
      connection.onreconnected(
        () => void connection.invoke('JoinSession', result.data.id, workspaceId, engagement.id),
      )
      await connection.start()
      await connection.invoke('JoinSession', result.data.id, workspaceId, engagement.id)
      connectionRef.current = connection
      setSession({ id: result.data.id, joinCode: result.data.joinCode, journeyStepId })
      setTally([])
      setPinTally([])
      setPresenceCount(0)
      setState({ status: 'live' })
    } catch (error: unknown) {
      setState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not start a live session.',
      })
    }
  }

  const stop = async (): Promise<void> => {
    const closingSessionId = session?.id
    void connectionRef.current?.stop()
    connectionRef.current = null
    setSession(null)
    setTally([])
    setPinTally([])
    setPresenceCount(0)
    setState({ status: 'idle' })
    if (closingSessionId !== undefined) {
      try {
        await client.closeLiveSession(workspaceId, engagement.id, closingSessionId)
      } catch {
        // Best-effort: the facilitator's own view has already moved on, and an
        // already-expired or unreachable session leaves nothing further to close.
      }
    }
  }

  const promote = async (discoveryCardId: string, cardName: string, stepLabel: string): Promise<void> => {
    if (session === null || etag === null) return
    const count = tally.find((item) => item.discoveryCardId === discoveryCardId)?.count ?? 0
    setState({ status: 'promoting', discoveryCardId })
    try {
      const result = await client.promoteLiveVote(
        workspaceId,
        engagement.id,
        session.id,
        {
          discoveryCardId,
          journeyStepId: session.journeyStepId,
          rationale: `${count} vote${count === 1 ? '' : 's'} from the live session.`,
          rank: engagement.cardShortlist.length + 1,
        },
        etag,
      )
      onUpdated(result.data, result.etag)
      setState({ status: 'promoted', cardName, stepLabel })
    } catch (error: unknown) {
      setState({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not promote this card.',
      })
    }
  }

  return {
    journeyStepId,
    setJourneyStepId,
    session,
    tally,
    pinTally,
    presenceCount,
    state,
    startPrivate,
    setStartPrivate,
    start,
    stop,
    promote,
  }
}

function LiveVoteSessionPanel({
  journeyStepId,
  setJourneyStepId,
  session,
  tally,
  pinTally,
  presenceCount,
  state,
  startPrivate,
  setStartPrivate,
  start,
  stop,
  promote,
  stepOptions,
  shortlistedForStep,
  isOnline,
}: {
  readonly journeyStepId: string
  readonly setJourneyStepId: (value: string) => void
  readonly session: LiveVoteSessionHandle | null
  readonly tally: readonly CardVoteTally[]
  readonly pinTally: readonly CardPinTally[]
  readonly presenceCount: number
  readonly state: LiveVoteSessionState
  readonly startPrivate: boolean
  readonly setStartPrivate: (value: boolean) => void
  readonly start: () => void | Promise<void>
  readonly stop: () => void | Promise<void>
  readonly promote: (discoveryCardId: string, cardName: string, stepLabel: string) => void | Promise<void>
  readonly stepOptions: readonly JourneyStepOption[]
  readonly shortlistedForStep: readonly DiscoveryCard[]
  readonly isOnline: boolean
}) {
  const [copied, setCopied] = useState(false)
  const currentStepLabel = stepOptions.find((option) => option.id === journeyStepId)?.label ?? 'this step'
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

  if (state.status === 'promoted') {
    return (
      <div className="live-vote-session-panel live-vote-session-confirmation">
        <p>
          Promoted <strong>{state.cardName}</strong> for {state.stepLabel}.
        </p>
        <button type="button" className="button-secondary" onClick={() => void stop()}>
          Done
        </button>
      </div>
    )
  }

  if (session === null) {
    return (
      <div className="live-vote-session-panel">
        <label htmlFor="live-vote-step">Start a live vote for</label>
        <select
          id="live-vote-step"
          value={journeyStepId}
          onChange={(event) => setJourneyStepId(event.target.value)}
          disabled={stepOptions.length === 0}
        >
          {stepOptions.map((option) => (
            <option key={option.id} value={option.id}>
              {option.label}
            </option>
          ))}
        </select>
        {shortlistedForStep.length > 0 ? (
          <ul className="live-vote-leaderboard">
            {shortlistedForStep.map((card) => (
              <li key={card.id}>
                <span>{card.displayName}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="discovery-cards-count">
            No cards shortlisted for this step yet. Shortlist one or two below first, or start
            the vote and add candidates live while the room watches.
          </p>
        )}
        <label className="live-vote-private-toggle">
          <input
            type="checkbox"
            checked={startPrivate}
            onChange={(event) => setStartPrivate(event.target.checked)}
          />
          Start the board privately: cards stay hidden from each other until you reveal them
        </label>
        {state.status === 'error' && <p className="form-error-summary">{state.message}</p>}
        <button
          type="button"
          onClick={() => void start()}
          disabled={!isOnline || state.status === 'starting' || stepOptions.length === 0}
          title={stepOptions.length === 0 ? 'Map a persona and journey first' : undefined}
        >
          {state.status === 'starting' ? 'Starting…' : 'Start a live vote'}
        </button>
      </div>
    )
  }

  // The vote is scoped to the shortlist, not the full 79-card catalog. This is the room
  // deciding among candidates someone's judgment already narrowed down, not re-running the
  // exploration live. Every shortlisted card shows here, whether or not it has votes yet, so
  // the facilitator sees the whole candidate set, not just whatever happened to get clicked.
  const candidates = shortlistedForStep
    .map((card) => ({
      card,
      count: tally.find((item) => item.discoveryCardId === card.id)?.count ?? 0,
      pinCount: pinTally.find((item) => item.discoveryCardId === card.id)?.count ?? 0,
    }))
    .sort((a, b) => b.count + b.pinCount - (a.count + a.pinCount))

  return (
    <div className="live-vote-session-panel">
      <p className="discovery-cards-count">
        Live for <strong>{currentStepLabel}</strong> · join code <strong>{session.joinCode}</strong> ·{' '}
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
      {candidates.length === 0 ? (
        <p className="discovery-cards-count">
          No cards shortlisted yet. Shortlist a card below to give the room something to vote
          on. It'll appear here and on their screens right away.
        </p>
      ) : (
        <ul className="live-vote-leaderboard">
          {candidates.map(({ card, count, pinCount }) => (
            <li key={card.id}>
              <span>{card.displayName}</span>
              {count > 0 && (
                <span className="discovery-card-vote-badge">
                  {count} vote{count === 1 ? '' : 's'}
                </span>
              )}
              {pinCount > 0 && (
                <span className="discovery-card-vote-badge">
                  {pinCount} pin{pinCount === 1 ? '' : 's'}
                </span>
              )}
              <button
                type="button"
                onClick={() => void promote(card.id, card.displayName, currentStepLabel)}
                disabled={!isOnline || state.status === 'promoting' || (count === 0 && pinCount === 0)}
              >
                {state.status === 'promoting' && state.discoveryCardId === card.id ? 'Promoting…' : 'Promote'}
              </button>
            </li>
          ))}
        </ul>
      )}
      {state.status === 'error' && <p className="form-error-summary">{state.message}</p>}

      <p className="discovery-cards-count">
        Want the full shared mural, everyone placing, moving, and discussing cards on an open
        canvas? Open <a href="#/board">the board</a> on a shared screen; it stays connected to
        this same live session.
      </p>

      <button type="button" className="button-secondary" onClick={() => void stop()}>
        Close voting
      </button>
    </div>
  )
}

export function DiscoveryCardsView({
  client,
  workspaceId,
  engagement,
  etag,
  isOnline,
  onUpdated,
}: DiscoveryCardsViewProps) {
  const [categoryId, setCategoryId] = useState('')
  const [search, setSearch] = useState('')
  const stepOptions = useJourneyStepOptions(engagement)
  const liveVote = useLiveVoteSession({ client, workspaceId, engagement, etag, isOnline, onUpdated, stepOptions })

  const categoryById = useMemo(
    () => new Map(discoveryCardCategories.map((category) => [category.id, category])),
    [],
  )

  const filteredCards = useMemo(
    () => filterDiscoveryCards(discoveryCards, categoryId, search),
    [categoryId, search],
  )

  // Cheap, honest "already relevant" signal built from history that already exists: every
  // card ever shortlisted for this step in a past round, rather than a new relevance model.
  const shortlistedForStep = useMemo(() => {
    if (liveVote.journeyStepId === '') return []
    const ids = [
      ...new Set(
        engagement.cardShortlist
          .filter((entry) => entry.journeyStepId === liveVote.journeyStepId)
          .map((entry) => entry.discoveryCardId),
      ),
    ]
    return ids
      .map((id) => discoveryCards.find((card) => card.id === id))
      .filter((card): card is DiscoveryCard => card !== undefined)
  }, [engagement.cardShortlist, liveVote.journeyStepId])

  return (
    <section className="page discovery-cards-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">AI Discovery Cards</p>
          <h1>Spark the workshop conversation</h1>
          <p>
            Browse AI capabilities by category. Share this list during a live
            discussion, then capture what resonates as evidence.
          </p>
        </div>
        <div className="cards-filters">
          <div>
            <label htmlFor="discovery-category">Category</label>
            <select
              id="discovery-category"
              value={categoryId}
              onChange={(event) => setCategoryId(event.target.value)}
            >
              <option value="">All categories</option>
              {discoveryCardCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.displayName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="discovery-search">Search</label>
            <input
              id="discovery-search"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search capability or description"
            />
          </div>
        </div>
      </header>

      <LiveVoteSessionPanel
        journeyStepId={liveVote.journeyStepId}
        setJourneyStepId={liveVote.setJourneyStepId}
        session={liveVote.session}
        tally={liveVote.tally}
        pinTally={liveVote.pinTally}
        presenceCount={liveVote.presenceCount}
        state={liveVote.state}
        startPrivate={liveVote.startPrivate}
        setStartPrivate={liveVote.setStartPrivate}
        start={liveVote.start}
        stop={liveVote.stop}
        promote={liveVote.promote}
        stepOptions={stepOptions}
        shortlistedForStep={shortlistedForStep}
        isOnline={isOnline}
      />

      {liveVote.journeyStepId !== '' && (
        <AiCardSuggestionPanel
          client={client}
          workspaceId={workspaceId}
          engagement={engagement}
          etag={etag}
          isOnline={isOnline}
          onUpdated={onUpdated}
          journeyStepId={liveVote.journeyStepId}
        />
      )}

      <p className="discovery-cards-count">
        {filteredCards.length} of {discoveryCards.length} cards
      </p>

      {filteredCards.length === 0 && (
        <EmptyState
          title="No cards match"
          message="This category has no cards in the source deck yet, or your search didn't match anything. Try a different category or clear the search."
        />
      )}

      <ul className="card-list">
        {filteredCards.map((card) => {
          const category = categoryById.get(card.categoryId)
          return (
            <li key={card.id}>
              <div className="card-list-heading">
                <span className="card-type-badge">
                  {category?.displayName ?? card.categoryId}
                </span>
                {card.microsoftServices.length > 0 && (
                  <span className="discovery-card-service-count">
                    {card.microsoftServices.length} service
                    {card.microsoftServices.length === 1 ? '' : 's'}
                  </span>
                )}
              </div>
              <div className="card-medallion">
                <DiscoveryCardIcon categoryId={card.categoryId} />
              </div>
              <div>
                <h3>{card.displayName}</h3>
                {card.microsoftServices.length > 0 && (
                  <ul className="card-tags">
                    {card.microsoftServices.map((service) => (
                      <li key={service}>{service}</li>
                    ))}
                  </ul>
                )}
              </div>
              <p className="card-highlight">{card.description}</p>
              {card.examples.length > 0 && (
                <FlipCard
                  flipLabel="Flip for the full stat sheet"
                  front={
                    <dl className="discovery-card-examples">
                      {card.examples.slice(0, 2).map((example) => {
                        const [label, ...rest] = example.split(' - ')
                        const detail = rest.join(' - ')
                        return (
                          <div key={example}>
                            <dt>{label}</dt>
                            {detail !== '' && <dd>{detail}</dd>}
                          </div>
                        )
                      })}
                    </dl>
                  }
                  back={
                    <dl className="card-stats">
                      <div>
                        <dt>Use cases</dt>
                        <dd>{card.examples.length}</dd>
                      </div>
                      <div>
                        <dt>Microsoft services</dt>
                        <dd>{card.microsoftServices.length}</dd>
                      </div>
                      {liveVote.journeyStepId !== '' && (
                        <>
                          <div>
                            <dt>Votes for this step</dt>
                            <dd>
                              {liveVote.tally.find((item) => item.discoveryCardId === card.id)?.count ?? 0}
                            </dd>
                          </div>
                          <div>
                            <dt>Pins for this step</dt>
                            <dd>
                              {liveVote.pinTally.find((item) => item.discoveryCardId === card.id)?.count ?? 0}
                            </dd>
                          </div>
                        </>
                      )}
                    </dl>
                  }
                />
              )}
              <ShortlistAction
                cardId={card.id}
                client={client}
                workspaceId={workspaceId}
                engagement={engagement}
                etag={etag}
                isOnline={isOnline}
                onUpdated={onUpdated}
                stepOptions={stepOptions}
              />
            </li>
          )
        })}
      </ul>
    </section>
  )
}
