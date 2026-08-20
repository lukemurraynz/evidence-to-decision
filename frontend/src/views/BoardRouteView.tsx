import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useEffect, useMemo, useRef, useState } from 'react'
import { BoardView, CATALOG_DRAG_MIME, randomScatterPosition, zoneLabelFor, type LiveCursor } from '../components/BoardView'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import type {
  BoardClusterCardInput,
  BoardClusterResult,
  BoardSnapshotItem,
  CardPinTally,
  CardVoteTally,
  Engagement,
  LiveBoardCard,
} from '../api/contracts'
import { discoveryCardCategories, discoveryCards, filterDiscoveryCards } from '../data/discoveryCards'
import { useJourneyStepOptions } from './DiscoveryCardsView'

type BoardRouteViewProps = {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
  readonly isOnline: boolean
}

type ConnectionState =
  | { readonly status: 'idle' }
  | { readonly status: 'looking' }
  | { readonly status: 'no-session' }
  | { readonly status: 'connected'; readonly sessionId: string; readonly joinCode: string }
  | { readonly status: 'error'; readonly message: string }

const CURSOR_STALE_MS = 8_000

function StickyNoteComposer({
  onPlace,
  isOnline,
}: {
  readonly onPlace: (discoveryCardId: string | null, x: number, y: number, rationale: string) => void | Promise<void>
  readonly isOnline: boolean
}) {
  const [open, setOpen] = useState(false)
  const [text, setText] = useState('')
  const [placing, setPlacing] = useState(false)

  if (!open) {
    return (
      <button type="button" className="button-secondary board-sidebar-full-width" onClick={() => setOpen(true)}>
        + Add a sticky note
      </button>
    )
  }

  const place = async (): Promise<void> => {
    if (text.trim() === '') return
    setPlacing(true)
    try {
      const { x, y } = randomScatterPosition()
      await onPlace(null, x, y, text.trim())
      setOpen(false)
      setText('')
    } finally {
      setPlacing(false)
    }
  }

  return (
    <div className="card-shortlist-form">
      <label htmlFor="board-route-sticky-note-text">
        Note: for anything the catalog doesn't cover <span aria-hidden="true">*</span>
      </label>
      <input
        id="board-route-sticky-note-text"
        value={text}
        onChange={(event) => setText(event.target.value)}
        maxLength={500}
      />
      <div className="card-shortlist-form-actions">
        <button type="button" onClick={() => void place()} disabled={!isOnline || placing || text.trim() === ''}>
          {placing ? 'Adding…' : 'Add to board'}
        </button>
        <button type="button" className="button-secondary" onClick={() => setOpen(false)}>
          Cancel
        </button>
      </div>
    </div>
  )
}

/** Draggable straight onto the canvas (lands exactly where it's dropped). The click "Place"
 * fallback stays for keyboard/touch users, landing at a scattered spot instead. */
function CatalogPlaceRow({
  cardId,
  displayName,
  onPlace,
  isOnline,
}: {
  readonly cardId: string
  readonly displayName: string
  readonly onPlace: (discoveryCardId: string | null, x: number, y: number, rationale: string) => void | Promise<void>
  readonly isOnline: boolean
}) {
  const [placing, setPlacing] = useState(false)

  const place = async (): Promise<void> => {
    setPlacing(true)
    try {
      const { x, y } = randomScatterPosition()
      await onPlace(cardId, x, y, '')
    } finally {
      setPlacing(false)
    }
  }

  return (
    <li
      className="board-catalog-row"
      draggable
      onDragStart={(event) => {
        event.dataTransfer.setData(CATALOG_DRAG_MIME, cardId)
        event.dataTransfer.effectAllowed = 'copy'
      }}
    >
      <span>{displayName}</span>
      <button type="button" className="button-secondary" onClick={() => void place()} disabled={!isOnline || placing}>
        {placing ? 'Placing…' : 'Place'}
      </button>
    </li>
  )
}

function tallyMap(tally: readonly { readonly discoveryCardId: string; readonly count: number }[]): Map<string, number> {
  return new Map(tally.map((item) => [item.discoveryCardId, item.count]))
}

/**
 * Full-screen, dedicated view onto the live shared mural. A separate concern from Discovery
 * Cards' catalog browsing and voting. It never starts its own live session; it attaches to
 * whichever session Discovery Cards' "Start a live vote" already minted for the chosen step, so
 * the whole room stays in one join code instead of splitting into two disconnected rooms.
 */
export function BoardRouteView({ client, workspaceId, engagement, isOnline }: BoardRouteViewProps) {
  const stepOptions = useJourneyStepOptions(engagement)
  const [journeyStepId, setJourneyStepId] = useState(stepOptions[0]?.id ?? '')
  const [connection, setConnection] = useState<ConnectionState>({ status: 'idle' })
  const [board, setBoard] = useState<readonly LiveBoardCard[]>([])
  const [revealed, setRevealed] = useState(true)
  const [presenceCount, setPresenceCount] = useState(0)
  const [categoryId, setCategoryId] = useState('')
  const [search, setSearch] = useState('')
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const [cursorsByParticipant, setCursorsByParticipant] = useState<ReadonlyMap<string, LiveCursor & { readonly lastSeen: number }>>(new Map())
  const [voteTally, setVoteTally] = useState<readonly CardVoteTally[]>([])
  const [pinTally, setPinTally] = useState<readonly CardPinTally[]>([])
  const [revealing, setRevealing] = useState(false)
  const [clearing, setClearing] = useState(false)
  const [snapshotting, setSnapshotting] = useState(false)
  const [snapshotMessage, setSnapshotMessage] = useState<string | null>(null)
  const [clustering, setClustering] = useState(false)
  const [clusterResult, setClusterResult] = useState<BoardClusterResult | null>(null)
  const [clusterError, setClusterError] = useState<string | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    const firstOption = stepOptions[0]
    if (firstOption === undefined) return
    if (stepOptions.some((option) => option.id === journeyStepId)) return
    setJourneyStepId(firstOption.id)
  }, [stepOptions, journeyStepId])

  useEffect(() => () => void connectionRef.current?.stop(), [])

  useEffect(() => {
    const onFullscreenChange = (): void => setIsFullscreen(document.fullscreenElement !== null)
    document.addEventListener('fullscreenchange', onFullscreenChange)
    return () => document.removeEventListener('fullscreenchange', onFullscreenChange)
  }, [])

  const toggleFullscreen = (): void => {
    if (document.fullscreenElement !== null) {
      void document.exitFullscreen()
    } else {
      void document.documentElement.requestFullscreen()
    }
  }

  // Sweeps cursors that stopped reporting without a clean disconnect (closed tab, lost network).
  // There's no server-side "cursor left" event, so a client-side timeout is the simplest
  // correct way to stop showing a stale dot.
  useEffect(() => {
    const interval = setInterval(() => {
      const now = performance.now()
      setCursorsByParticipant((current) => {
        const next = new Map([...current].filter(([, cursor]) => now - cursor.lastSeen < CURSOR_STALE_MS))
        return next.size === current.size ? current : next
      })
    }, 2_000)
    return () => clearInterval(interval)
  }, [])

  const attach = async (): Promise<void> => {
    if (journeyStepId === '') return
    void connectionRef.current?.stop()
    connectionRef.current = null
    setBoard([])
    setCursorsByParticipant(new Map())
    setConnection({ status: 'looking' })
    try {
      const session = await client.getActiveLiveSession(workspaceId, engagement.id, journeyStepId)
      if (session === null) {
        setConnection({ status: 'no-session' })
        return
      }
      const token = await client.getAccessToken()
      const hub = new HubConnectionBuilder()
        .withUrl(new URL('/hubs/collaboration', client.apiBaseUrl).toString(), {
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build()
      hub.on('BoardUpdated', (nextBoard: readonly LiveBoardCard[], nextRevealed: boolean) => {
        setBoard(nextBoard)
        setRevealed(nextRevealed)
      })
      hub.on('PresenceUpdated', (count: number) => setPresenceCount(count))
      hub.on('VoteTallyUpdated', (nextTally: readonly CardVoteTally[]) => setVoteTally(nextTally))
      hub.on('PinTallyUpdated', (nextTally: readonly CardPinTally[]) => setPinTally(nextTally))
      hub.on('CursorMoved', (participantId: string, displayName: string, x: number, y: number) => {
        setCursorsByParticipant((current) => {
          const next = new Map(current)
          next.set(participantId, { participantId, displayName, x, y, lastSeen: performance.now() })
          return next
        })
      })
      hub.onreconnected(() => void hub.invoke('JoinSession', session.id, workspaceId, engagement.id))
      await hub.start()
      await hub.invoke('JoinSession', session.id, workspaceId, engagement.id)
      connectionRef.current = hub
      setConnection({ status: 'connected', sessionId: session.id, joinCode: session.joinCode })
    } catch (error: unknown) {
      setConnection({
        status: 'error',
        message: error instanceof ApiRequestError ? error.message : 'Could not attach to the live session.',
      })
    }
  }

  const placeCard = async (discoveryCardId: string | null, x: number, y: number, rationale: string): Promise<void> => {
    await connectionRef.current?.invoke('PlaceBoardCard', discoveryCardId, x, y, rationale)
  }

  const placeCatalogCardAt = async (discoveryCardId: string, x: number, y: number): Promise<void> => {
    await connectionRef.current?.invoke('PlaceBoardCard', discoveryCardId, x, y, '')
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

  const reportCursor = (x: number, y: number): void => {
    void connectionRef.current?.invoke('MoveCursor', x, y)
  }

  const revealBoard = async (): Promise<void> => {
    if (connection.status !== 'connected') return
    setRevealing(true)
    try {
      await client.revealBoard(workspaceId, engagement.id, connection.sessionId)
    } finally {
      setRevealing(false)
    }
  }

  const setBoardPrivate = async (): Promise<void> => {
    if (connection.status !== 'connected') return
    setRevealing(true)
    try {
      await client.setBoardPrivate(workspaceId, engagement.id, connection.sessionId)
      setRevealed(false)
    } finally {
      setRevealing(false)
    }
  }

  const clearBoard = async (): Promise<void> => {
    if (connection.status !== 'connected') return
    if (!window.confirm('Remove every card from the board for everyone? Snapshot to evidence first if you want to keep a record. This cannot be undone.')) return
    setClearing(true)
    try {
      await client.clearBoard(workspaceId, engagement.id, connection.sessionId)
    } finally {
      setClearing(false)
    }
  }

  const snapshotBoard = async (): Promise<void> => {
    if (connection.status !== 'connected' || board.length === 0) return
    setSnapshotting(true)
    setSnapshotMessage(null)
    try {
      const fresh = await client.getEngagement(workspaceId, engagement.id)
      if (fresh.etag === null) throw new ApiRequestError('Missing engagement version.', 409, null, null)
      const items: readonly BoardSnapshotItem[] = board.map((card) => ({
        placementId: card.id,
        discoveryCardId: card.discoveryCardId,
        cardDisplayName: card.discoveryCardId === null
          ? null
          : discoveryCards.find((item) => item.id === card.discoveryCardId)?.displayName ?? null,
        rationale: card.rationale,
        placedByDisplayName: card.placedByDisplayName,
        zoneLabel: zoneLabelFor(card.x),
      }))
      await client.snapshotBoard(workspaceId, engagement.id, connection.sessionId, items, fresh.etag)
      setSnapshotMessage(`Captured ${items.length} card${items.length === 1 ? '' : 's'} as evidence.`)
    } catch (error: unknown) {
      setSnapshotMessage(error instanceof ApiRequestError ? error.message : 'Could not snapshot the board.')
    } finally {
      setSnapshotting(false)
    }
  }

  const suggestClusters = async (): Promise<void> => {
    if (board.length === 0) return
    setClustering(true)
    setClusterError(null)
    setClusterResult(null)
    try {
      const cards: readonly BoardClusterCardInput[] = board.map((card) => ({
        placementId: card.id,
        cardDisplayName: card.discoveryCardId === null
          ? null
          : discoveryCards.find((item) => item.id === card.discoveryCardId)?.displayName ?? null,
        rationale: card.rationale,
        x: card.x,
        y: card.y,
      }))
      const result = await client.suggestBoardClusters(workspaceId, engagement.id, cards)
      setClusterResult(result.data)
    } catch (error: unknown) {
      setClusterError(error instanceof ApiRequestError ? error.message : 'Could not get a clustering suggestion.')
    } finally {
      setClustering(false)
    }
  }

  const filteredCards = useMemo(
    () => filterDiscoveryCards(discoveryCards, categoryId, search),
    [categoryId, search],
  )

  const cursors = useMemo(() => {
    const now = performance.now()
    return [...cursorsByParticipant.values()].map((cursor) => {
      const age = now - cursor.lastSeen
      const opacity = age < 3_000 ? 1 : Math.max(0.25, 1 - (age - 3_000) / (CURSOR_STALE_MS - 3_000) * 0.75)
      return { ...cursor, opacity }
    })
  }, [cursorsByParticipant])
  const voteTallyMap = useMemo(() => tallyMap(voteTally), [voteTally])
  const pinTallyMap = useMemo(() => tallyMap(pinTally), [pinTally])
  const participantsWithCards = useMemo(
    () => new Set(board.map((card) => card.placedByParticipantId)).size,
    [board],
  )

  return (
    <section className="board-route-page">
      <div className="board-route-topbar">
        <div className="board-route-heading">
          <p className="eyebrow">Shared board</p>
          <h1>The mural</h1>
        </div>
        <label htmlFor="board-route-step">Journey step</label>
        <select
          id="board-route-step"
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
        <button
          type="button"
          className="button-secondary"
          onClick={() => void attach()}
          disabled={!isOnline || connection.status === 'looking' || stepOptions.length === 0}
        >
          {connection.status === 'looking' ? 'Looking…' : 'Attach to the live session'}
        </button>
        {connection.status === 'no-session' && (
          <p className="discovery-cards-count">
            No live session for this step. Start one from <a href="#/discovery-cards">Discovery cards</a>.
          </p>
        )}
        {connection.status === 'connected' && (
          <p className="discovery-cards-count">
            Connected · join code <strong>{connection.joinCode}</strong> ·{' '}
            {presenceCount} {presenceCount === 1 ? 'person' : 'people'} here
          </p>
        )}
        {connection.status === 'error' && <p className="form-error-summary">{connection.message}</p>}
        {connection.status === 'connected' && (
          <>
            {revealed ? (
              <button type="button" onClick={() => void setBoardPrivate()} disabled={!isOnline || revealing}>
                {revealing ? 'Starting…' : 'Start a private round'}
              </button>
            ) : (
              <button type="button" onClick={() => void revealBoard()} disabled={!isOnline || revealing}>
                {revealing ? 'Revealing…' : 'Reveal to everyone'}
              </button>
            )}
            <button
              type="button"
              className="button-secondary"
              onClick={() => void clearBoard()}
              disabled={!isOnline || clearing || board.length === 0}
            >
              {clearing ? 'Clearing…' : 'Clear board'}
            </button>
            <button
              type="button"
              className="button-secondary"
              onClick={() => void snapshotBoard()}
              disabled={!isOnline || snapshotting || board.length === 0}
            >
              {snapshotting ? 'Capturing…' : 'Snapshot to evidence'}
            </button>
            <button
              type="button"
              className="button-secondary"
              onClick={() => void suggestClusters()}
              disabled={!isOnline || clustering || board.length < 3}
              title={board.length < 3 ? 'Add at least 3 cards to get a useful clustering suggestion.' : undefined}
            >
              {clustering ? 'Thinking…' : 'Suggest clusters'}
            </button>
            <button
              type="button"
              className="button-secondary board-sidebar-toggle"
              onClick={() => setSidebarOpen((open) => !open)}
              aria-pressed={sidebarOpen}
            >
              {sidebarOpen ? 'Hide catalog' : 'Show catalog'}
            </button>
            <button type="button" className="button-secondary" onClick={toggleFullscreen}>
              {isFullscreen ? 'Exit fullscreen' : 'Enter fullscreen'}
            </button>
          </>
        )}
      </div>

      {connection.status === 'connected' && !revealed && (
        <p className="board-route-private-hint">
          Private mode: you're the only one seeing every card right now. Everyone else sees only
          their own placements until you reveal.{' '}
          {participantsWithCards} of {presenceCount} {presenceCount === 1 ? 'person has' : 'people have'} added a card.
        </p>
      )}
      {snapshotMessage !== null && <p className="discovery-cards-count">{snapshotMessage}</p>}
      {clusterError !== null && <p className="form-error-summary">{clusterError}</p>}
      {clusterResult !== null && (
        <div className="board-cluster-results">
          <div className="board-cluster-results-header">
            <p className="eyebrow">Suggested clusters</p>
            <button type="button" className="button-secondary" onClick={() => setClusterResult(null)}>
              Dismiss
            </button>
          </div>
          <p className="discovery-cards-count">{clusterResult.requiredReview}</p>
          <ul className="board-cluster-list">
            {clusterResult.clusters.map((cluster) => (
              <li key={cluster.label}>
                <strong>{cluster.label}</strong>
                <p>{cluster.rationale}</p>
                <span className="origin-label">{cluster.placementIds.length} card{cluster.placementIds.length === 1 ? '' : 's'}</span>
              </li>
            ))}
          </ul>
          {clusterResult.outlierPlacementIds.length > 0 && (
            <p className="discovery-cards-count">
              {clusterResult.outlierPlacementIds.length} card{clusterResult.outlierPlacementIds.length === 1 ? '' : 's'} didn't fit a cluster.
            </p>
          )}
        </div>
      )}

      {connection.status === 'connected' ? (
        <div className={`board-route-body${sidebarOpen ? '' : ' board-route-sidebar-collapsed'}`}>
          <aside className="board-route-sidebar">
            <StickyNoteComposer onPlace={placeCard} isOnline={isOnline} />
            <div className="board-sidebar-filters">
              <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
                <option value="">All categories</option>
                {discoveryCardCategories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.displayName}
                  </option>
                ))}
              </select>
              <input
                type="search"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search capability…"
              />
            </div>
            <p className="board-sidebar-hint">Drag a card onto the board, or tap Place.</p>
            <ul className="board-catalog-list">
              {filteredCards.map((card) => (
                <CatalogPlaceRow
                  key={card.id}
                  cardId={card.id}
                  displayName={card.displayName}
                  onPlace={placeCard}
                  isOnline={isOnline}
                />
              ))}
            </ul>
          </aside>
          <div className="board-route-canvas">
            <BoardView
              board={board}
              onMove={moveCard}
              onRemove={removeCard}
              onEdit={editCard}
              onPlaceCatalogCardAt={placeCatalogCardAt}
              onRestore={placeCard}
              cursors={cursors}
              onCursorMove={reportCursor}
              voteTally={voteTallyMap}
              pinTally={pinTallyMap}
              isOnline={isOnline}
            />
          </div>
        </div>
      ) : (
        <p className="board-route-empty">
          Attach to a live session above to open the shared canvas.
        </p>
      )}
    </section>
  )
}
