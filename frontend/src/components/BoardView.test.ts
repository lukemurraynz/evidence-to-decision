import { describe, expect, it } from 'vitest'
import { invertPointerPosition } from './BoardView'

// Matches the WORLD_WIDTH/WORLD_HEIGHT constants inside BoardView.tsx. Not exported (no need
// to leak layout constants), so a known-good normalized point is forward-projected into a pixel
// here using the same numbers, then inverted and compared back.
const WORLD_WIDTH = 3600
const WORLD_HEIGHT = 2200

describe('invertPointerPosition', () => {
  it('round-trips a known point at zoom 1 with no scroll offset', () => {
    const rectLeft = 40
    const rectTop = 12
    const normalized = { x: 0.25, y: 0.6 }
    const clientX = rectLeft + normalized.x * WORLD_WIDTH
    const clientY = rectTop + normalized.y * WORLD_HEIGHT

    const result = invertPointerPosition(clientX, clientY, rectLeft, rectTop, 0, 0, 1)

    expect(result.x).toBeCloseTo(normalized.x, 10)
    expect(result.y).toBeCloseTo(normalized.y, 10)
  })

  it('round-trips a known point at a non-1 zoom with a scroll offset', () => {
    const rectLeft = 10
    const rectTop = 10
    const zoom = 1.5
    const scrollLeft = 200
    const scrollTop = 100
    const normalized = { x: 0.4, y: 0.3 }
    const worldX = normalized.x * WORLD_WIDTH
    const worldY = normalized.y * WORLD_HEIGHT
    const clientX = rectLeft - scrollLeft + worldX * zoom
    const clientY = rectTop - scrollTop + worldY * zoom

    const result = invertPointerPosition(clientX, clientY, rectLeft, rectTop, scrollLeft, scrollTop, zoom)

    expect(result.x).toBeCloseTo(normalized.x, 10)
    expect(result.y).toBeCloseTo(normalized.y, 10)
  })

  it('clamps a point above/left of the canvas to 0', () => {
    const result = invertPointerPosition(-500, -500, 0, 0, 0, 0, 1)

    expect(result.x).toBe(0)
    expect(result.y).toBe(0)
  })

  it('clamps a point below/right of the world to 1', () => {
    const result = invertPointerPosition(WORLD_WIDTH * 10, WORLD_HEIGHT * 10, 0, 0, 0, 0, 1)

    expect(result.x).toBe(1)
    expect(result.y).toBe(1)
  })
})
