import { useMemo, useRef, useState } from 'react'
import type { LiveBoardCard } from '../api/contracts'
import { discoveryCards } from '../data/discoveryCards'

export const BOARD_ZONES: readonly { readonly label: string; readonly hint: string }[] = [
  { label: 'Exploring', hint: 'raw ideas' },
  { label: 'Strong fit', hint: 'clear yes' },
  { label: 'Maybe', hint: 'needs discussion' },
  { label: 'Not a fit', hint: 'ruled out' },
]

// The board's virtual size in CSS px, independent of the visible viewport, which is why
// zoom/pan exist at all. A card's stored x/y (0..1) is always relative to this world, not the
// viewport, so existing placements keep meaning the same relative spot regardless of zoom.
const WORLD_WIDTH = 3600
const WORLD_HEIGHT = 2200
const MIN_ZOOM = 0.4
const MAX_ZOOM = 2.5
const ZOOM_STEP = 0.2

const NOTE_COLORS = ['note-yellow', 'note-pink', 'note-blue', 'note-green', 'note-orange'] as const

/** Cheap, stable per-card variety (rotation angle, sticky-note colour) derived from the
 * placement's own id. Deterministic across renders, no extra state needed, and different
 * enough between neighbouring cards to read as "scattered by hand" rather than machine-laid. */
function hashString(value: string): number {
  let hash = 0
  for (let index = 0; index < value.length; index += 1) {
    hash = (hash * 31 + value.charCodeAt(index)) | 0
  }
  return Math.abs(hash)
}

function rotationFor(placementId: string): number {
  return (hashString(placementId) % 9) - 4
}

function colorFor(placementId: string): (typeof NOTE_COLORS)[number] {
  return NOTE_COLORS[hashString(placementId + 'c') % NOTE_COLORS.length] ?? NOTE_COLORS[0]
}

/** A freshly-placed card has no drag gesture to derive a position from, so it lands at a
 * randomized spot toward the open (left) side of the canvas: close enough to reach, scattered
 * enough that two cards placed back-to-back don't stack exactly on top of each other. Used by the
 * click-to-place fallback; a drag from the catalog sidebar lands exactly where it's dropped
 * instead (see CATALOG_DRAG_MIME below). */
export function randomScatterPosition(): { readonly x: number; readonly y: number } {
  return { x: 0.08 + Math.random() * 0.2, y: 0.1 + Math.random() * 0.75 }
}

/** Drag payload for dragging a catalog card straight from the sidebar onto the canvas. A
 * distinct MIME type from the plain-text payload existing placements use for onDragStart, so the
 * canvas's onDrop can tell "place a new card here" apart from "move this existing one here"
 * without parsing an envelope out of a single shared field. */
export const CATALOG_DRAG_MIME = 'application/x-oe-discovery-card'

/** Which decorative zone an x position falls under. Position IS the categorization (no LaneId
 * field), this just makes that fact visible instead of silent. Four equal columns, matching the
 * CSS grid `.board-mural-zones` already renders. */
export function zoneLabelFor(x: number): string {
  const index = Math.min(BOARD_ZONES.length - 1, Math.max(0, Math.floor(x * BOARD_ZONES.length)))
  return BOARD_ZONES[index]?.label ?? 'Exploring'
}

/** Pure coordinate inversion: a pointer position in viewport px → the normalized 0..1 board
 * position it corresponds to, accounting for the canvas's current scroll offset and zoom level.
 * Exported (rather than kept as a component closure) so this branchy math has a standalone,
 * mountless unit test; see BoardView.test.ts. */
export function invertPointerPosition(
  clientX: number,
  clientY: number,
  canvasRectLeft: number,
  canvasRectTop: number,
  scrollLeft: number,
  scrollTop: number,
  zoom: number,
): { readonly x: number; readonly y: number } {
  const worldX = (clientX - canvasRectLeft + scrollLeft) / zoom
  const worldY = (clientY - canvasRectTop + scrollTop) / zoom
  return {
    x: Math.min(1, Math.max(0, worldX / WORLD_WIDTH)),
    y: Math.min(1, Math.max(0, worldY / WORLD_HEIGHT)),
  }
}

type BoardViewProps = {
  readonly board: readonly LiveBoardCard[]
  readonly onMove: (placementId: string, x: number, y: number) => void | Promise<void>
  readonly onRemove: (placementId: string) => void | Promise<void>
  readonly onEdit: (placementId: string, rationale: string) => void | Promise<void>
  /** Omit to disable dropping catalog cards straight onto the canvas (e.g. read-only contexts). */
  readonly onPlaceCatalogCardAt?: (discoveryCardId: string, x: number, y: number) => void | Promise<void>
  /** Omit to disable the "undo remove" banner. Re-places a card with its original id/x/y/rationale. */
  readonly onRestore?: (discoveryCardId: string | null, x: number, y: number, rationale: string) => void | Promise<void>
  /** Other viewers' live pointer positions. Omit to disable rendering cursors entirely. */
  readonly cursors?: readonly LiveCursor[]
  /** discoveryCardId → count, from Discovery Cards' own vote/pin rounds. Omit to disable the
   * overlay badge (the mural has no voting of its own, this just surfaces the existing tally). */
  readonly voteTally?: ReadonlyMap<string, number>
  readonly pinTally?: ReadonlyMap<string, number>
  /** Called (throttled internally) as the local pointer moves over the canvas. Omit to disable
   * reporting this viewer's own cursor to others. Only fires for mouse/pen input; touch input is
   * reserved for pinch-zoom and tap-to-place, not cursor broadcast. */
  readonly onCursorMove?: (x: number, y: number) => void
  readonly isOnline: boolean
}

export type LiveCursor = {
  readonly participantId: string
  readonly displayName: string
  readonly x: number
  readonly y: number
  /** Fades a cursor that's stopped moving before it's swept away entirely. The caller (which
   * owns the "last seen" timestamp) computes this, BoardView just renders it. Omit for full
   * opacity. */
  readonly opacity?: number
}

type PointerPoint = { readonly x: number; readonly y: number }

/** Exactly two points from an active-pointers map, or null if the map doesn't have exactly two.
 * narrows away the "possibly undefined" TypeScript sees in a plain array destructure. */
function twoPoints(points: Map<number, PointerPoint>): readonly [PointerPoint, PointerPoint] | null {
  if (points.size !== 2) return null
  const [a, b] = [...points.values()]
  return a === undefined || b === undefined ? null : [a, b]
}

/**
 * Shared, live mural board: an open canvas where every placement carries its own x/y position;
 * there is no lane/category field, a card's spot on the board IS its categorization, exactly
 * like a physical sticky-note wall. The four zone labels along the top are a decorative backdrop
 * annotated with a live per-zone count (see zoneLabelFor), never read against placement data as
 * a stored field. Native HTML5 drag-and-drop is the primary desktop interaction (drop position
 * becomes the card's new x/y); tap-to-select-a-card then tap-the-canvas-to-drop-it-there is the
 * fallback for touch, since native drag doesn't fire on touchscreens without a polyfill and
 * phones are this app's primary participant device. Two-finger pinch/pan on touch, and
 * ctrl/cmd+wheel or the on-screen +/− controls on desktop, zoom the canvas. Panning otherwise
 * uses the outer element's native scroll (see App.css's `.board-mural`) rather than a hand-rolled
 * transform, so trackpad/scrollbar/momentum all come for free.
 */
const CURSOR_REPORT_INTERVAL_MS = 100

export function BoardView({
  board,
  onMove,
  onRemove,
  onEdit,
  onPlaceCatalogCardAt,
  onRestore,
  cursors,
  onCursorMove,
  voteTally,
  pinTally,
  isOnline,
}: BoardViewProps) {
  const canvasRef = useRef<HTMLDivElement>(null)
  const [selectedPlacementId, setSelectedPlacementId] = useState<string | null>(null)
  const [isDragOver, setIsDragOver] = useState(false)
  const [editingPlacementId, setEditingPlacementId] = useState<string | null>(null)
  const [editDraft, setEditDraft] = useState('')
  const [zoom, setZoom] = useState(1)
  const [viewport, setViewport] = useState({ scrollLeft: 0, scrollTop: 0, width: 0, height: 0 })
  const [lastAction, setLastAction] = useState<{ readonly type: 'removed' | 'moved'; readonly card: LiveBoardCard } | null>(null)
  const lastActionTimeout = useRef<ReturnType<typeof setTimeout> | null>(null)
  const lastCursorSentAt = useRef(0)

  const activePointers = useRef(new Map<number, PointerPoint>())
  const pinchStartDistance = useRef<number | null>(null)
  const pinchStartZoom = useRef(1)
  const pinchLastMidpoint = useRef<PointerPoint | null>(null)

  const applyZoom = (nextZoom: number): void => {
    setZoom(Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, nextZoom)))
  }

  const positionFromPointer = (clientX: number, clientY: number): { x: number; y: number } | null => {
    const canvas = canvasRef.current
    if (canvas === null) return null
    const rect = canvas.getBoundingClientRect()
    if (rect.width === 0 || rect.height === 0) return null
    return invertPointerPosition(clientX, clientY, rect.left, rect.top, canvas.scrollLeft, canvas.scrollTop, zoom)
  }

  const updateViewport = (): void => {
    const canvas = canvasRef.current
    if (canvas === null) return
    setViewport({
      scrollLeft: canvas.scrollLeft,
      scrollTop: canvas.scrollTop,
      width: canvas.clientWidth,
      height: canvas.clientHeight,
    })
  }

  const zoomToFit = (): void => {
    const canvas = canvasRef.current
    if (canvas === null || board.length === 0) {
      applyZoom(1)
      return
    }

    const minX = Math.min(...board.map((card) => card.x))
    const maxX = Math.max(...board.map((card) => card.x))
    const minY = Math.min(...board.map((card) => card.y))
    const maxY = Math.max(...board.map((card) => card.y))
    const spanWidth = Math.max(0.15, maxX - minX) * WORLD_WIDTH + 260
    const spanHeight = Math.max(0.15, maxY - minY) * WORLD_HEIGHT + 200
    const nextZoom = Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, Math.min(canvas.clientWidth / spanWidth, canvas.clientHeight / spanHeight)))
    setZoom(nextZoom)
    requestAnimationFrame(() => {
      const centerX = ((minX + maxX) / 2) * WORLD_WIDTH * nextZoom
      const centerY = ((minY + maxY) / 2) * WORLD_HEIGHT * nextZoom
      canvas.scrollLeft = Math.max(0, centerX - canvas.clientWidth / 2)
      canvas.scrollTop = Math.max(0, centerY - canvas.clientHeight / 2)
      updateViewport()
    })
  }

  const onPointerDownCapture = (event: React.PointerEvent<HTMLDivElement>): void => {
    if (event.pointerType !== 'touch') return
    activePointers.current.set(event.pointerId, { x: event.clientX, y: event.clientY })
    const pair = twoPoints(activePointers.current)
    if (pair === null) return
    const [a, b] = pair
    pinchStartDistance.current = Math.hypot(a.x - b.x, a.y - b.y)
    pinchStartZoom.current = zoom
    pinchLastMidpoint.current = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }
  }

  const onPointerMoveCapture = (event: React.PointerEvent<HTMLDivElement>): void => {
    if (event.pointerType !== 'touch') {
      if (onCursorMove === undefined) return
      const now = performance.now()
      if (now - lastCursorSentAt.current < CURSOR_REPORT_INTERVAL_MS) return
      const position = positionFromPointer(event.clientX, event.clientY)
      if (position === null) return
      lastCursorSentAt.current = now
      onCursorMove(position.x, position.y)
      return
    }

    if (!activePointers.current.has(event.pointerId)) return
    activePointers.current.set(event.pointerId, { x: event.clientX, y: event.clientY })
    if (pinchStartDistance.current === null) return
    const pair = twoPoints(activePointers.current)
    if (pair === null) return
    const [a, b] = pair

    const distance = Math.hypot(a.x - b.x, a.y - b.y)
    applyZoom(pinchStartZoom.current * (distance / pinchStartDistance.current))

    const midpoint = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }
    const canvas = canvasRef.current
    if (canvas !== null && pinchLastMidpoint.current !== null) {
      canvas.scrollLeft -= midpoint.x - pinchLastMidpoint.current.x
      canvas.scrollTop -= midpoint.y - pinchLastMidpoint.current.y
    }
    pinchLastMidpoint.current = midpoint
  }

  const onPointerEndCapture = (event: React.PointerEvent<HTMLDivElement>): void => {
    activePointers.current.delete(event.pointerId)
    if (activePointers.current.size < 2) {
      pinchStartDistance.current = null
      pinchLastMidpoint.current = null
    }
  }

  const handleWheel = (event: React.WheelEvent<HTMLDivElement>): void => {
    if (!event.ctrlKey) return
    event.preventDefault()
    applyZoom(zoom - event.deltaY * 0.01)
  }

  const selectCard = (placementId: string): void => {
    setSelectedPlacementId((current) => (current === placementId ? null : placementId))
  }

  const dropSelectedAt = (clientX: number, clientY: number): void => {
    if (selectedPlacementId === null) return
    const position = positionFromPointer(clientX, clientY)
    if (position === null) return
    moveWithUndo(selectedPlacementId, position.x, position.y)
    setSelectedPlacementId(null)
  }

  const startEdit = (placement: LiveBoardCard): void => {
    setEditingPlacementId(placement.id)
    setEditDraft(placement.rationale)
  }

  const saveEdit = (placementId: string): void => {
    void onEdit(placementId, editDraft)
    setEditingPlacementId(null)
  }

  const armUndo = (action: { readonly type: 'removed' | 'moved'; readonly card: LiveBoardCard }): void => {
    if (lastActionTimeout.current !== null) clearTimeout(lastActionTimeout.current)
    setLastAction(action)
    lastActionTimeout.current = setTimeout(() => setLastAction(null), 8_000)
  }

  const removeWithUndo = (placement: LiveBoardCard): void => {
    void onRemove(placement.id)
    if (onRestore === undefined) return
    armUndo({ type: 'removed', card: placement })
  }

  /** Snapshots the placement's pre-move position before calling onMove, so undo can restore the
   * same record to where it was. Unlike a removed card, a moved one is never deleted, so undo
   * here is a second onMove rather than a re-create. */
  const moveWithUndo = (placementId: string, x: number, y: number): void => {
    const previous = board.find((card) => card.id === placementId)
    void onMove(placementId, x, y)
    if (previous === undefined) return
    armUndo({ type: 'moved', card: previous })
  }

  const undoLastAction = (): void => {
    if (lastAction === null) return
    const { type, card } = lastAction
    if (type === 'removed') {
      if (onRestore !== undefined) void onRestore(card.discoveryCardId, card.x, card.y, card.rationale)
    } else {
      void onMove(card.id, card.x, card.y)
    }
    if (lastActionTimeout.current !== null) clearTimeout(lastActionTimeout.current)
    setLastAction(null)
  }

  const zoneCounts = useMemo(() => {
    const counts = new Array<number>(BOARD_ZONES.length).fill(0)
    for (const card of board) {
      const index = Math.min(BOARD_ZONES.length - 1, Math.max(0, Math.floor(card.x * BOARD_ZONES.length)))
      counts[index] = (counts[index] ?? 0) + 1
    }
    return counts
  }, [board])

  return (
    <div className="board-mural-shell">
      <div className="board-zoom-controls">
        <button type="button" onClick={() => applyZoom(zoom - ZOOM_STEP)} aria-label="Zoom out">
          −
        </button>
        <span className="board-zoom-level">{Math.round(zoom * 100)}%</span>
        <button type="button" onClick={() => applyZoom(zoom + ZOOM_STEP)} aria-label="Zoom in">
          +
        </button>
        <button type="button" onClick={zoomToFit}>
          Zoom to fit
        </button>
        <button type="button" onClick={() => applyZoom(1)}>
          Reset
        </button>
      </div>
      <div
        ref={canvasRef}
        className={`board-mural${isDragOver ? ' board-mural-drag-over' : ''}${selectedPlacementId !== null ? ' board-mural-placing' : ''}`}
        onScroll={updateViewport}
        onWheel={handleWheel}
        onPointerDown={onPointerDownCapture}
        onPointerMove={onPointerMoveCapture}
        onPointerUp={onPointerEndCapture}
        onPointerCancel={onPointerEndCapture}
        onDragOver={(event) => {
          event.preventDefault()
          setIsDragOver(true)
        }}
        onDragLeave={() => setIsDragOver(false)}
        onDrop={(event) => {
          event.preventDefault()
          setIsDragOver(false)
          const position = positionFromPointer(event.clientX, event.clientY)
          if (position === null) return
          const discoveryCardId = event.dataTransfer.getData(CATALOG_DRAG_MIME)
          if (discoveryCardId !== '') {
            if (onPlaceCatalogCardAt !== undefined) void onPlaceCatalogCardAt(discoveryCardId, position.x, position.y)
            return
          }
          const placementId = event.dataTransfer.getData('text/plain')
          if (placementId !== '') moveWithUndo(placementId, position.x, position.y)
        }}
        onClick={(event) => {
          if (event.target === event.currentTarget) dropSelectedAt(event.clientX, event.clientY)
        }}
      >
        <div
          className="board-mural-world"
          style={{ width: WORLD_WIDTH, height: WORLD_HEIGHT, transform: `scale(${zoom})` }}
        >
          <div className="board-mural-zones" aria-hidden="true">
            {BOARD_ZONES.map((zone, index) => (
              <div key={zone.label} className="board-mural-zone">
                <span className="board-mural-zone-label">
                  {zone.label}
                  {(zoneCounts[index] ?? 0) > 0 && <span className="board-mural-zone-count">{zoneCounts[index]}</span>}
                </span>
                <span className="board-mural-zone-hint">{zone.hint}</span>
              </div>
            ))}
          </div>
          {board.length === 0 && (
            <p className="discovery-cards-count board-mural-empty">
              No cards on the board yet. Place one from the catalog below.
            </p>
          )}
          {board.map((placement) => {
            const card =
              placement.discoveryCardId === null
                ? null
                : discoveryCards.find((item) => item.id === placement.discoveryCardId)
            if (card === undefined) return null
            const selected = selectedPlacementId === placement.id
            const editing = editingPlacementId === placement.id
            return (
              <div
                key={placement.id}
                className={`board-sticky ${colorFor(placement.id)}${card === null ? ' board-sticky-note' : ''}${selected ? ' board-sticky-selected' : ''}${editing ? ' board-sticky-expanded' : ''}`}
                style={{
                  left: `${placement.x * 100}%`,
                  top: `${placement.y * 100}%`,
                  transform: `translate(-50%, -50%) rotate(${editing ? 0 : rotationFor(placement.id)}deg)`,
                }}
                draggable={!editing}
                onDragStart={(event) => {
                  event.dataTransfer.setData('text/plain', placement.id)
                  event.dataTransfer.effectAllowed = 'move'
                }}
                onClick={(event) => event.stopPropagation()}
              >
                <div className="board-sticky-toolbar">
                  <button
                    type="button"
                    className="board-sticky-remove"
                    aria-label="Remove from board"
                    onClick={() => removeWithUndo(placement)}
                    disabled={!isOnline}
                  >
                    ×
                  </button>
                </div>
                {editing ? (
                  <div className="board-sticky-edit">
                    <textarea
                      value={editDraft}
                      onChange={(event) => setEditDraft(event.target.value)}
                      maxLength={500}
                      autoFocus
                    />
                    <div className="card-shortlist-form-actions">
                      <button type="button" onClick={() => saveEdit(placement.id)} disabled={!isOnline}>
                        Save
                      </button>
                      <button type="button" className="button-secondary" onClick={() => setEditingPlacementId(null)}>
                        Cancel
                      </button>
                    </div>
                  </div>
                ) : (
                  <button
                    type="button"
                    className="board-sticky-select"
                    onClick={() => selectCard(placement.id)}
                    aria-pressed={selected}
                  >
                    {card === null ? (
                      <p>{placement.rationale}</p>
                    ) : (
                      <>
                        <strong>{card.displayName}</strong>
                        {(() => {
                          const votes = voteTally?.get(card.id) ?? 0
                          const pins = pinTally?.get(card.id) ?? 0
                          return (
                            <>
                              {votes > 0 && (
                                <span className="discovery-card-vote-badge">
                                  {votes} vote{votes === 1 ? '' : 's'}
                                </span>
                              )}
                              {pins > 0 && (
                                <span className="discovery-card-vote-badge">
                                  {pins} pin{pins === 1 ? '' : 's'}
                                </span>
                              )}
                            </>
                          )
                        })()}
                        {placement.rationale !== '' && <p>{placement.rationale}</p>}
                        <div className="board-sticky-detail">
                          <p>{card.description}</p>
                          {card.examples.length > 0 && (
                            <dl>
                              {card.examples.slice(0, 3).map((example) => (
                                <dt key={example}>{example.split(' - ')[0]}</dt>
                              ))}
                            </dl>
                          )}
                          {card.microsoftServices.length > 0 && (
                            <ul className="card-tags">
                              {card.microsoftServices.map((service) => (
                                <li key={service}>{service}</li>
                              ))}
                            </ul>
                          )}
                        </div>
                      </>
                    )}
                    <span className="origin-label">{placement.placedByDisplayName}</span>
                  </button>
                )}
                {!editing && (
                  <button type="button" className="board-sticky-edit-toggle" onClick={() => startEdit(placement)}>
                    Edit
                  </button>
                )}
              </div>
            )
          })}
          {cursors?.map((cursor) => (
            <div
              key={cursor.participantId}
              className="board-cursor"
              style={{ left: `${cursor.x * 100}%`, top: `${cursor.y * 100}%`, opacity: cursor.opacity ?? 1 }}
            >
              <span className="board-cursor-dot" />
              <span className="board-cursor-label">{cursor.displayName}</span>
            </div>
          ))}
        </div>
        {selectedPlacementId !== null && <p className="board-mural-placing-hint">Tap anywhere on the board to drop it there.</p>}
      </div>
      <BoardMiniMap
        board={board}
        zoom={zoom}
        viewport={viewport}
        onJump={(worldX, worldY) => {
          const canvas = canvasRef.current
          if (canvas === null) return
          canvas.scrollLeft = Math.max(0, worldX * zoom - canvas.clientWidth / 2)
          canvas.scrollTop = Math.max(0, worldY * zoom - canvas.clientHeight / 2)
          updateViewport()
        }}
      />
      {lastAction !== null && (lastAction.type === 'moved' || onRestore !== undefined) && (
        <p className="board-undo-banner">
          {lastAction.type === 'removed' ? 'Card removed' : 'Card moved'} ·{' '}
          <button type="button" onClick={undoLastAction}>
            Undo
          </button>
        </p>
      )}
    </div>
  )
}

type BoardMiniMapProps = {
  readonly board: readonly LiveBoardCard[]
  readonly zoom: number
  readonly viewport: { readonly scrollLeft: number; readonly scrollTop: number; readonly width: number; readonly height: number }
  readonly onJump: (worldX: number, worldY: number) => void
}

const MINIMAP_WIDTH = 140
const MINIMAP_HEIGHT = (MINIMAP_WIDTH * WORLD_HEIGHT) / WORLD_WIDTH

/** Small fixed overview of the whole board. Necessary once zoom/pan exists, since a zoomed-in
 * view can no longer show where the visible viewport sits relative to everything placed.
 * Purely derived from the board + current scroll/zoom state, no state of its own. */
function BoardMiniMap({ board, zoom, viewport, onJump }: BoardMiniMapProps) {
  if (board.length === 0) return null

  const scale = MINIMAP_WIDTH / WORLD_WIDTH
  const viewportRect = {
    left: (viewport.scrollLeft / zoom) * scale,
    top: (viewport.scrollTop / zoom) * scale,
    width: (viewport.width / zoom) * scale,
    height: (viewport.height / zoom) * scale,
  }

  return (
    <div
      className="board-minimap"
      style={{ width: MINIMAP_WIDTH, height: MINIMAP_HEIGHT }}
      onClick={(event) => {
        const rect = event.currentTarget.getBoundingClientRect()
        onJump(((event.clientX - rect.left) / scale), ((event.clientY - rect.top) / scale))
      }}
    >
      {board.map((card) => (
        <span
          key={card.id}
          className="board-minimap-dot"
          style={{ left: card.x * MINIMAP_WIDTH, top: card.y * MINIMAP_HEIGHT }}
        />
      ))}
      <div
        className="board-minimap-viewport"
        style={{
          left: Math.max(0, viewportRect.left),
          top: Math.max(0, viewportRect.top),
          width: Math.min(MINIMAP_WIDTH, viewportRect.width),
          height: Math.min(MINIMAP_HEIGHT, viewportRect.height),
        }}
      />
    </div>
  )
}
