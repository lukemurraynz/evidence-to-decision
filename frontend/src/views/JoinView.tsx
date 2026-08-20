import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { FlipCard } from '../components/FlipCard'
import { BoardView, randomScatterPosition } from '../components/BoardView'
import { redeemJoinCode } from '../api/participantClient'
import type {
  CardPinTally,
  CardVoteTally,
  LiveBoardCard,
  LiveIdeationNote,
  PinToggleResult,
} from '../api/contracts'
import {
  discoveryCardCategories,
  discoveryCards,
  filterDiscoveryCards,
  type DiscoveryCard,
} from '../data/discoveryCards'

type JoinViewProps = {
  readonly apiBaseUrl: string
  readonly joinCode: string
}

type ConnectedSession = {
  readonly displayName: string
  readonly journeyStepId: string | null
  readonly journeyStepName: string | null
  readonly journeyStepPainPoint: string | null
}

function CardVoteRow({
  card,
  categoryLabel,
  count,
  voted,
  casting,
  closed,
  errorMessage,
  onVote,
  pinned,
  pinning,
  pinCount,
  pinErrorMessage,
  onTogglePin,
}: {
  readonly card: DiscoveryCard
  readonly categoryLabel: string
  readonly count: number | undefined
  readonly voted: boolean
  readonly casting: boolean
  readonly closed: boolean
  readonly errorMessage: string | undefined
  readonly onVote: () => void
  readonly pinned: boolean
  readonly pinning: boolean
  readonly pinCount: number | undefined
  readonly pinErrorMessage: string | undefined
  readonly onTogglePin: () => void
}) {
  return (
    <li>
      <div className="card-list-heading">
        <span className="card-type-badge">{categoryLabel}</span>
        {count !== undefined && count > 0 && (
          <span className="discovery-card-vote-badge">
            {count} vote{count === 1 ? '' : 's'}
          </span>
        )}
        {pinCount !== undefined && pinCount > 0 && (
          <span className="discovery-card-vote-badge">
            {pinCount} pin{pinCount === 1 ? '' : 's'}
          </span>
        )}
      </div>
      <h3>{card.displayName}</h3>
      <p className="card-highlight">{card.description}</p>
      <div className="card-shortlist-form-actions">
        <button type="button" onClick={onVote} disabled={voted || casting || closed} aria-pressed={voted}>
          {voted
            ? '✓ Voted'
            : closed
              ? 'Voting closed'
              : casting
                ? 'Voting…'
                : errorMessage !== undefined
                  ? 'Try again'
                  : 'Vote for this card'}
        </button>
        <button
          type="button"
          className="button-secondary"
          onClick={onTogglePin}
          disabled={pinning}
          aria-pressed={pinned}
        >
          {pinning ? 'Updating…' : pinned ? '✓ Pinned' : 'Pin'}
        </button>
      </div>
      {errorMessage !== undefined && (
        <p className="form-error-summary" role="alert">
          {errorMessage}
        </p>
      )}
      {pinErrorMessage !== undefined && (
        <p className="form-error-summary" role="alert">
          {pinErrorMessage}
        </p>
      )}
    </li>
  )
}

function CardPinRow({
  card,
  categoryLabel,
  pinned,
  pinning,
  pinCount,
  errorMessage,
  onTogglePin,
  onPlace,
}: {
  readonly card: DiscoveryCard
  readonly categoryLabel: string
  readonly pinned: boolean
  readonly pinning: boolean
  readonly pinCount: number | undefined
  readonly errorMessage: string | undefined
  readonly onTogglePin: () => void
  readonly onPlace: () => void
}) {
  return (
    <li>
      <div className="card-list-heading">
        <span className="card-type-badge">{categoryLabel}</span>
        {pinCount !== undefined && pinCount > 0 && (
          <span className="discovery-card-vote-badge">
            {pinCount} pin{pinCount === 1 ? '' : 's'}
          </span>
        )}
      </div>
      <h3>{card.displayName}</h3>
      <p className="card-highlight">{card.description}</p>
      {card.examples.length > 0 && (
        <FlipCard
          flipLabel="Flip for details"
          front={<p className="discovery-cards-count">{card.examples.length} example use cases</p>}
          back={
            <dl className="discovery-card-examples">
              {card.examples.slice(0, 3).map((example) => {
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
        />
      )}
      <div className="card-shortlist-form-actions">
        <button type="button" onClick={onTogglePin} disabled={pinning} aria-pressed={pinned}>
          {pinning ? 'Updating…' : pinned ? '✓ Pinned' : 'Pin for later'}
        </button>
        <button type="button" className="button-secondary" onClick={onPlace}>
          Place on board
        </button>
      </div>
      {errorMessage !== undefined && (
        <p className="form-error-summary" role="alert">
          {errorMessage}
        </p>
      )}
    </li>
  )
}

function JoinStickyNoteAction({
  placeCard,
}: {
  readonly placeCard: (discoveryCardId: string | null, x: number, y: number, rationale: string) => void | Promise<void>
}) {
  const [open, setOpen] = useState(false)
  const [text, setText] = useState('')
  const [placing, setPlacing] = useState(false)

  if (!open) {
    return (
      <button type="button" className="button-secondary" onClick={() => setOpen(true)}>
        + Add a sticky note
      </button>
    )
  }

  const place = async (): Promise<void> => {
    if (text.trim() === '') return
    setPlacing(true)
    try {
      const { x, y } = randomScatterPosition()
      await placeCard(null, x, y, text.trim())
      setOpen(false)
      setText('')
    } finally {
      setPlacing(false)
    }
  }

  return (
    <div className="card-shortlist-form">
      <label htmlFor="join-sticky-note-text">
        Note: for anything the catalog doesn't cover <span aria-hidden="true">*</span>
      </label>
      <input
        id="join-sticky-note-text"
        value={text}
        onChange={(event) => setText(event.target.value)}
        maxLength={500}
      />
      <div className="card-shortlist-form-actions">
        <button type="button" onClick={() => void place()} disabled={placing || text.trim() === ''}>
          {placing ? 'Adding…' : 'Add to board'}
        </button>
        <button type="button" className="button-secondary" onClick={() => setOpen(false)}>
          Cancel
        </button>
      </div>
    </div>
  )
}

type JoinState =
  | { readonly status: 'prompting' }
  | { readonly status: 'joining' }
  | { readonly status: 'connected'; readonly session: ConnectedSession }
  | { readonly status: 'error'; readonly message: string }

export function JoinView({ apiBaseUrl, joinCode }: JoinViewProps) {
  const [displayName, setDisplayName] = useState('')
  const [state, setState] = useState<JoinState>({ status: 'prompting' })
  const [tally, setTally] = useState<readonly CardVoteTally[]>([])
  // Live-updating, not just the join-time snapshot. A facilitator can shortlist a new card
  // while the round is already running, and the room should see it appear without rejoining.
  const [shortlistedDiscoveryCardIds, setShortlistedDiscoveryCardIds] = useState<readonly string[]>([])
  // The room's aggregate tally over SignalR is a secondary display. A participant's own
  // "voted" confirmation comes from this local set, flipped the instant CastVote resolves, so
  // it never depends on a broadcast echoing back to the same connection that sent it.
  const [votedCardIds, setVotedCardIds] = useState<ReadonlySet<string>>(new Set())
  const [castingCardIds, setCastingCardIds] = useState<ReadonlySet<string>>(new Set())
  const [voteErrors, setVoteErrors] = useState<ReadonlyMap<string, string>>(new Map())
  const [sessionClosed, setSessionClosed] = useState(false)
  const [presenceCount, setPresenceCount] = useState(1)
  // Ideation mode (session.journeyStepId === null): everyone's submitted ideas, live.
  const [ideationNotes, setIdeationNotes] = useState<readonly LiveIdeationNote[]>([])
  const [ideaText, setIdeaText] = useState('')
  const [submittingIdea, setSubmittingIdea] = useState(false)
  const [ideaError, setIdeaError] = useState<string | undefined>(undefined)
  // Pinning: catalog-wide and personal, unlike voting which stays scoped to the shortlist;
  // same "own state comes from my own invoke resolving, not the broadcast" rule as voting.
  const [pinTally, setPinTally] = useState<readonly CardPinTally[]>([])
  const [pinnedCardIds, setPinnedCardIds] = useState<ReadonlySet<string>>(new Set())
  const [pinningCardIds, setPinningCardIds] = useState<ReadonlySet<string>>(new Set())
  const [pinErrors, setPinErrors] = useState<ReadonlyMap<string, string>>(new Map())
  const [browseCategoryId, setBrowseCategoryId] = useState('')
  const [browseSearch, setBrowseSearch] = useState('')
  const [board, setBoard] = useState<readonly LiveBoardCard[]>([])
  const [boardRevealed, setBoardRevealed] = useState(true)
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => () => void connectionRef.current?.stop(), [])

  const join = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    const trimmedName = displayName.trim()
    if (trimmedName === '') return
    setState({ status: 'joining' })
    try {
      const session = await redeemJoinCode(apiBaseUrl, joinCode, trimmedName)
      const connection = new HubConnectionBuilder()
        .withUrl(new URL('/hubs/collaboration', apiBaseUrl).toString(), {
          accessTokenFactory: () => session.token,
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build()
      connection.on('VoteTallyUpdated', (nextTally: readonly CardVoteTally[]) => {
        setTally(nextTally)
      })
      connection.on('ShortlistUpdated', (nextCardIds: readonly string[]) => {
        setShortlistedDiscoveryCardIds(nextCardIds)
      })
      connection.on('SessionClosed', () => {
        setSessionClosed(true)
      })
      connection.on('PresenceUpdated', (count: number) => {
        setPresenceCount(count)
      })
      connection.on('IdeationBoardUpdated', (nextNotes: readonly LiveIdeationNote[]) => {
        setIdeationNotes(nextNotes)
      })
      connection.on('PinTallyUpdated', (nextTally: readonly CardPinTally[]) => {
        setPinTally(nextTally)
      })
      connection.on('BoardUpdated', (nextBoard: readonly LiveBoardCard[], nextRevealed: boolean) => {
        setBoard(nextBoard)
        setBoardRevealed(nextRevealed)
      })
      // Automatic reconnect gets a new underlying connection with empty group membership.
      // without re-joining here, a participant silently stops receiving any broadcast after
      // the first reconnect (Azure SignalR recycles long-lived connections periodically, so
      // this isn't rare over the length of a real workshop).
      connection.onreconnected(() => void connection.invoke('JoinSession', session.joinSessionId, null, null))
      await connection.start()
      await connection.invoke('JoinSession', session.joinSessionId, null, null)
      connectionRef.current = connection
      setShortlistedDiscoveryCardIds(session.shortlistedDiscoveryCardIds)
      setState({
        status: 'connected',
        session: {
          displayName: trimmedName,
          journeyStepId: session.journeyStepId,
          journeyStepName: session.journeyStepName,
          journeyStepPainPoint: session.journeyStepPainPoint,
        },
      })
    } catch (error: unknown) {
      setState({
        status: 'error',
        message:
          error instanceof Error
            ? error.message
            : 'Could not join the session. Check the code and try again.',
      })
    }
  }

  const castVote = async (discoveryCardId: string, journeyStepId: string): Promise<void> => {
    const connection = connectionRef.current
    if (connection === null) return
    setCastingCardIds((current) => new Set(current).add(discoveryCardId))
    setVoteErrors((current) => {
      if (!current.has(discoveryCardId)) return current
      const next = new Map(current)
      next.delete(discoveryCardId)
      return next
    })
    try {
      await connection.invoke('CastVote', discoveryCardId, journeyStepId)
      setVotedCardIds((current) => new Set(current).add(discoveryCardId))
    } catch {
      // The hub connection can be mid-reconnect (a phone locking or backgrounding the tab is
      // routine, not an edge case). SignalR throws rather than queuing, so without this the
      // participant would see nothing at all: no confirmation, no error, no reason to retry.
      setVoteErrors((current) =>
        new Map(current).set(discoveryCardId, 'Could not cast this vote. Check your connection and try again.'),
      )
    } finally {
      setCastingCardIds((current) => {
        const next = new Set(current)
        next.delete(discoveryCardId)
        return next
      })
    }
  }

  const togglePin = async (discoveryCardId: string, journeyStepId: string): Promise<void> => {
    const connection = connectionRef.current
    if (connection === null) return
    setPinningCardIds((current) => new Set(current).add(discoveryCardId))
    setPinErrors((current) => {
      if (!current.has(discoveryCardId)) return current
      const next = new Map(current)
      next.delete(discoveryCardId)
      return next
    })
    try {
      const result = await connection.invoke<PinToggleResult>('TogglePin', discoveryCardId, journeyStepId)
      setPinTally(result.tally)
      setPinnedCardIds((current) => {
        const next = new Set(current)
        if (result.pinned) next.add(discoveryCardId)
        else next.delete(discoveryCardId)
        return next
      })
    } catch {
      setPinErrors((current) =>
        new Map(current).set(discoveryCardId, 'Could not update this pin. Check your connection and try again.'),
      )
    } finally {
      setPinningCardIds((current) => {
        const next = new Set(current)
        next.delete(discoveryCardId)
        return next
      })
    }
  }

  const placeCard = async (discoveryCardId: string | null, x: number, y: number, rationale: string): Promise<void> => {
    await connectionRef.current?.invoke('PlaceBoardCard', discoveryCardId, x, y, rationale)
  }

  const moveCard = async (placementId: string, x: number, y: number): Promise<void> => {
    await connectionRef.current?.invoke('MoveBoardCard', placementId, x, y)
  }

  const removeCard = async (placementId: string): Promise<void> => {
    await connectionRef.current?.invoke('RemoveBoardCard', placementId)
  }

  const editCard = async (placementId: string, rationale: string): Promise<void> => {
    await connectionRef.current?.invoke('EditBoardCard', placementId, rationale)
  }

  const submitIdea = async (event: FormEvent<HTMLFormElement>): Promise<void> => {
    event.preventDefault()
    const connection = connectionRef.current
    const trimmed = ideaText.trim()
    if (connection === null || trimmed === '') return
    setSubmittingIdea(true)
    setIdeaError(undefined)
    try {
      await connection.invoke('SubmitIdea', trimmed)
      setIdeaText('')
    } catch {
      setIdeaError('Could not submit this idea. Check your connection and try again.')
    } finally {
      setSubmittingIdea(false)
    }
  }

  const categoryById = useMemo(
    () => new Map(discoveryCardCategories.map((category) => [category.id, category])),
    [],
  )
  const browsableCards = useMemo(
    () => filterDiscoveryCards(discoveryCards, browseCategoryId, browseSearch),
    [browseCategoryId, browseSearch],
  )

  if (state.status === 'connected') {
    const { session } = state

    if (session.journeyStepId === null) {
      return (
        <section className="page join-page">
          <header className="page-header">
            <div>
              <p className="eyebrow">Live workshop session</p>
              <h1>You're in, {session.displayName}</h1>
              <p>Add any idea that comes to mind, short is fine. Everyone sees the board live.</p>
            </div>
          </header>

          <p className="discovery-cards-count">
            {presenceCount} {presenceCount === 1 ? 'person' : 'people'} in this round, including you
          </p>

          {sessionClosed && (
            <p className="setup-note" role="status">
              The facilitator has closed this round. Thanks for taking part, you can close this page.
            </p>
          )}

          <form onSubmit={(event) => void submitIdea(event)} noValidate>
            {ideaError !== undefined && (
              <div className="form-error-summary" role="alert">
                <p>{ideaError}</p>
              </div>
            )}
            <label htmlFor="idea-text">Your idea</label>
            <input
              id="idea-text"
              value={ideaText}
              onChange={(event) => setIdeaText(event.target.value)}
              maxLength={500}
              disabled={sessionClosed}
            />
            <button type="submit" disabled={sessionClosed || submittingIdea || ideaText.trim() === ''}>
              {submittingIdea ? 'Submitting…' : 'Submit idea'}
            </button>
          </form>

          {ideationNotes.length === 0 ? (
            <p className="discovery-cards-count">No ideas yet. Be the first to add one.</p>
          ) : (
            <ul className="live-vote-leaderboard">
              {ideationNotes.map((note) => (
                <li key={note.id}>
                  <span>{note.text}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      )
    }

    const journeyStepId = session.journeyStepId
    const shortlistedCards = shortlistedDiscoveryCardIds
      .map((id) => discoveryCards.find((card) => card.id === id))
      .filter((card): card is DiscoveryCard => card !== undefined)
    return (
      <section className="page join-page">
        <header className="page-header">
          <div>
            <p className="eyebrow">Live workshop session</p>
            <h1>You're in, {session.displayName}</h1>
            <p>
              Voting on <strong>{session.journeyStepName ?? 'the step the facilitator picked'}</strong>
              {session.journeyStepPainPoint !== null && <>: {session.journeyStepPainPoint}</>}. Pick as
              many cards as feel right; you can change your mind.
            </p>
          </div>
        </header>

        <p className="discovery-cards-count">
          {presenceCount} {presenceCount === 1 ? 'person' : 'people'} in this round, including you
        </p>

        {sessionClosed && (
          <p className="setup-note" role="status">
            The facilitator has closed voting for this round. Thanks for taking part,
            you can close this page.
          </p>
        )}

        {shortlistedCards.length === 0 ? (
          <p className="discovery-cards-count">
            The facilitator hasn't added any cards to vote on yet. Hang tight, they'll appear
            here the moment they're added.
          </p>
        ) : (
          <ul className="card-list">
            {shortlistedCards.map((card) => (
              <CardVoteRow
                key={card.id}
                card={card}
                categoryLabel={categoryById.get(card.categoryId)?.displayName ?? card.categoryId}
                count={
                  tally.find(
                    (item) => item.discoveryCardId === card.id && item.journeyStepId === journeyStepId,
                  )?.count
                }
                voted={votedCardIds.has(card.id)}
                casting={castingCardIds.has(card.id)}
                closed={sessionClosed}
                errorMessage={voteErrors.get(card.id)}
                onVote={() => void castVote(card.id, journeyStepId)}
                pinned={pinnedCardIds.has(card.id)}
                pinning={pinningCardIds.has(card.id)}
                pinCount={
                  pinTally.find(
                    (item) => item.discoveryCardId === card.id && item.journeyStepId === journeyStepId,
                  )?.count
                }
                pinErrorMessage={pinErrors.get(card.id)}
                onTogglePin={() => void togglePin(card.id, journeyStepId)}
              />
            ))}
          </ul>
        )}

        <div className="section-heading compact">
          <div>
            <p className="eyebrow">Shared board</p>
            <h3>Drag cards into lanes together</h3>
          </div>
        </div>
        {!boardRevealed && (
          <p className="board-route-private-hint">
            Your facilitator started this board privately: you only see your own cards until
            they reveal the board to everyone.
          </p>
        )}
        <BoardView
          board={board}
          onMove={moveCard}
          onRemove={removeCard}
          onEdit={editCard}
          onRestore={placeCard}
          isOnline
        />
        <JoinStickyNoteAction placeCard={placeCard} />

        <div className="section-heading compact">
          <div>
            <p className="eyebrow">Explore more</p>
            <h3>Browse the full catalog and pin what's worth a second look</h3>
          </div>
        </div>
        <div className="cards-filters">
          <div>
            <label htmlFor="join-browse-category">Category</label>
            <select
              id="join-browse-category"
              value={browseCategoryId}
              onChange={(event) => setBrowseCategoryId(event.target.value)}
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
            <label htmlFor="join-browse-search">Search</label>
            <input
              id="join-browse-search"
              type="search"
              value={browseSearch}
              onChange={(event) => setBrowseSearch(event.target.value)}
              placeholder="Search capability or description"
            />
          </div>
        </div>
        {pinnedCardIds.size > 0 && (
          <p className="discovery-cards-count">
            You've pinned {pinnedCardIds.size} card{pinnedCardIds.size === 1 ? '' : 's'}. Scroll the list
            below to compare them.
          </p>
        )}
        <ul className="card-list">
          {browsableCards.map((card) => (
            <CardPinRow
              key={card.id}
              card={card}
              categoryLabel={categoryById.get(card.categoryId)?.displayName ?? card.categoryId}
              pinned={pinnedCardIds.has(card.id)}
              pinning={pinningCardIds.has(card.id)}
              pinCount={
                pinTally.find(
                  (item) => item.discoveryCardId === card.id && item.journeyStepId === journeyStepId,
                )?.count
              }
              errorMessage={pinErrors.get(card.id)}
              onTogglePin={() => void togglePin(card.id, journeyStepId)}
              onPlace={() => {
                const { x, y } = randomScatterPosition()
                void placeCard(card.id, x, y, '')
              }}
            />
          ))}
        </ul>
      </section>
    )
  }

  return (
    <section className="page join-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Live workshop session</p>
          <h1>Join the session</h1>
          <p>Enter your name to join code {joinCode} and vote alongside the room.</p>
        </div>
      </header>
      <form onSubmit={(event) => void join(event)} noValidate>
        {state.status === 'error' && (
          <div className="form-error-summary" role="alert">
            <p>{state.message}</p>
          </div>
        )}
        <label htmlFor="join-display-name">Your name</label>
        <input
          id="join-display-name"
          value={displayName}
          onChange={(event) => setDisplayName(event.target.value)}
          required
          maxLength={60}
        />
        <button type="submit" disabled={state.status === 'joining' || displayName.trim() === ''}>
          {state.status === 'joining' ? 'Joining…' : 'Join'}
        </button>
      </form>
    </section>
  )
}
