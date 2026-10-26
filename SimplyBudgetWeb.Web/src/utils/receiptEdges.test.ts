import { describe, expect, it } from 'vitest'
import type { ReceiptImageGeometry, ReceiptImagePoint } from '@/types'
import { preferPortrait, refineReceiptCorners } from './receiptEdges'

const width = 300, height = 400

function geometry(points: [number, number][]): ReceiptImageGeometry {
  return { version: 1, corners: points.map(([x, y]) => ({ x, y })) as ReceiptImageGeometry['corners'] }
}

// Paints a bright receipt (with dark "printed" rows) on a background; corners are normalized.
function photo(paper: ReceiptImageGeometry, background: number) {
  const data = new Uint8ClampedArray(width * height * 4)
  const quad = paper.corners.map(p => ({ x: p.x * (width - 1), y: p.y * (height - 1) }))
  const inside = (x: number, y: number) => quad.every((a, i) => {
    const b = quad[(i + 1) % 4]!
    return (b.x - a.x) * (y - a.y) - (b.y - a.y) * (x - a.x) >= 0
  })
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const value = inside(x, y) ? (y % 12 < 4 && x % 9 < 4 ? 60 : 245) : background + ((x * 7 + y * 13) % 20)
      data.set([value, value, value, 255], (y * width + x) * 4)
    }
  }
  return { width, height, data }
}

function maxError(actual: ReceiptImageGeometry, expected: ReceiptImageGeometry) {
  return Math.max(...actual.corners.map((p: ReceiptImagePoint, i) =>
    Math.hypot(p.x - expected.corners[i]!.x, p.y - expected.corners[i]!.y)))
}

describe('receipt edge refinement', () => {
  it('snaps a rough, text-hugging outline to the paper corners and keeps the receipt top-left', () => {
    // Upside-down paper: the receipt's top-left is the bottom-right corner of the photo.
    const paper = geometry([[0.8, 0.88], [0.25, 0.92], [0.18, 0.1], [0.72, 0.06]])
    const rough = geometry([[0.66, 0.7], [0.35, 0.72], [0.32, 0.3], [0.62, 0.28]])

    const refined = refineReceiptCorners(photo(paper, 90), rough)

    expect(maxError(rough, paper)).toBeGreaterThan(0.15)
    expect(maxError(refined, paper)).toBeLessThan(0.015)
  })

  it('keeps the original outline when the paper does not stand out from the background', () => {
    const paper = geometry([[0.3, 0.1], [0.7, 0.1], [0.7, 0.9], [0.3, 0.9]])
    const rough = geometry([[0.35, 0.2], [0.65, 0.2], [0.65, 0.8], [0.35, 0.8]])

    expect(refineReceiptCorners(photo(paper, 230), rough)).toBe(rough)
  })
})

describe('portrait preference', () => {
  it('relabels a landscape crop so the receipt top is the higher short edge', () => {
    // Labelled as if the long left edge were the top.
    const sideways = geometry([[0.3, 0.9], [0.3, 0.1], [0.7, 0.1], [0.7, 0.9]])

    expect(preferPortrait(sideways, width, height).corners).toEqual(geometry([[0.3, 0.1], [0.7, 0.1], [0.7, 0.9], [0.3, 0.9]]).corners)
  })

  it('leaves portrait and upside-down labelling alone', () => {
    const upsideDown = geometry([[0.7, 0.9], [0.3, 0.9], [0.3, 0.1], [0.7, 0.1]])

    expect(preferPortrait(upsideDown, width, height)).toBe(upsideDown)
  })
})
