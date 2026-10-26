import type { ReceiptImageGeometry, ReceiptImagePoint } from '@/types'
import { validGeometry } from '@/utils/receiptImage'

type Pixels = Pick<ImageData, 'width' | 'height' | 'data'>

const workEdge = 480
const searchExpansion = 1.45

function cross(a: ReceiptImagePoint, b: ReceiptImagePoint, p: ReceiptImagePoint) {
  return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x)
}

// Corners are clockwise in y-down space, so interior points are on the positive side of every edge.
function inside(quad: ReceiptImagePoint[], p: ReceiptImagePoint) {
  for (let i = 0; i < 4; i++) if (cross(quad[i]!, quad[(i + 1) % 4]!, p) < 0) return false
  return true
}

function area(quad: ReceiptImagePoint[]) {
  let twice = 0
  for (let i = 0; i < 4; i++) twice += quad[i]!.x * quad[(i + 1) % 4]!.y - quad[(i + 1) % 4]!.x * quad[i]!.y
  return Math.abs(twice) / 2
}

function otsu(histogram: Float64Array, total: number) {
  let sum = 0
  for (let i = 0; i < 256; i++) sum += i * histogram[i]!
  let background = 0, backgroundSum = 0, best = -1, threshold = 128
  for (let i = 0; i < 256; i++) {
    background += histogram[i]!
    if (!background) continue
    const foreground = total - background
    if (!foreground) break
    backgroundSum += i * histogram[i]!
    const difference = backgroundSum / background - (sum - backgroundSum) / foreground
    const variance = background * foreground * difference * difference
    if (variance > best) {
      best = variance
      threshold = i
    }
  }
  return threshold
}

/**
 * Receipts are taller than they are wide. When the corner labelling would produce a clearly
 * landscape crop, the receipt's top was misjudged by 90°, so pick the portrait labelling whose
 * top edge is higher in the photo.
 */
export function preferPortrait(geometry: ReceiptImageGeometry, width: number, height: number): ReceiptImageGeometry {
  const points = geometry.corners.map(p => ({ x: p.x * width, y: p.y * height }))
  const length = (i: number) => Math.hypot(points[(i + 1) % 4]!.x - points[i]!.x, points[(i + 1) % 4]!.y - points[i]!.y)
  if (length(0) + length(2) <= 1.3 * (length(1) + length(3))) return geometry
  const topY = (start: number) => points[start]!.y + points[(start + 1) % 4]!.y
  const start = topY(1) <= topY(3) ? 1 : 3
  return {
    version: 1,
    corners: [0, 1, 2, 3].map(i => geometry.corners[(start + i) % 4]!) as ReceiptImageGeometry['corners'],
  }
}

/**
 * Snaps an approximate receipt outline (for example from a vision model) to the actual paper
 * edges, the same way classic document scanners do: the receipt is the bright region that the
 * approximate outline sits on. Returns the original geometry whenever the paper cannot be
 * separated from its background with confidence. Corner order (which corner is the receipt's
 * top-left) is preserved.
 */
export function refineReceiptCorners(source: Pixels, coarse: ReceiptImageGeometry): ReceiptImageGeometry {
  if (!validGeometry(coarse) || source.width < 2 || source.height < 2) return coarse
  const scale = Math.min(1, workEdge / Math.max(source.width, source.height))
  const width = Math.max(2, Math.round(source.width * scale))
  const height = Math.max(2, Math.round(source.height * scale))

  const sums = new Float64Array(width * height)
  const counts = new Uint32Array(width * height)
  for (let y = 0; y < source.height; y++) {
    const row = Math.min(height - 1, Math.floor(y * height / source.height)) * width
    for (let x = 0; x < source.width; x++) {
      const offset = (y * source.width + x) * 4
      const target = row + Math.min(width - 1, Math.floor(x * width / source.width))
      sums[target]! += 0.299 * source.data[offset]! + 0.587 * source.data[offset + 1]! + 0.114 * source.data[offset + 2]!
      counts[target]!++
    }
  }
  // A 3x3 box blur softens thin printed strokes so the paper reads as one bright surface.
  const gray = new Float32Array(width * height)
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      let total = 0, n = 0
      for (let dy = -1; dy <= 1; dy++) {
        for (let dx = -1; dx <= 1; dx++) {
          const sx = x + dx, sy = y + dy
          if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue
          const index = sy * width + sx
          total += sums[index]! / counts[index]!
          n++
        }
      }
      gray[y * width + x] = total / n
    }
  }

  let quad: ReceiptImagePoint[] = coarse.corners.map(p => ({
    x: (p.x * (source.width - 1) + 0.5) * width / source.width - 0.5,
    y: (p.y * (source.height - 1) + 0.5) * height / source.height - 0.5,
  }))
  // An outline that only hugs the printed text can clip the paper, so search again around each
  // improved outline until it settles.
  let fit: PaperFit | null = null
  for (let pass = 0; pass < 4; pass++) {
    const next = fitPaper(gray, width, height, quad)
    if (!next) break
    const moved = Math.max(...next.corners.map((p, i) => Math.hypot(p.x - quad[i]!.x, p.y - quad[i]!.y)))
    fit = next
    quad = next.corners
    if (moved < 2) break
  }
  // On a light table the "paper" keeps spreading; only trust an outline that settles on
  // paper surrounded by darker background.
  if (!fit?.surrounded) return coarse
  const fitted = fit.corners
  const refined: ReceiptImageGeometry = {
    version: 1,
    corners: fitted.map(p => ({
      x: Math.min(1, Math.max(0, ((p.x + 0.5) * source.width / width - 0.5) / (source.width - 1))),
      y: Math.min(1, Math.max(0, ((p.y + 0.5) * source.height / height - 0.5) / (source.height - 1))),
    })) as ReceiptImageGeometry['corners'],
  }
  return validGeometry(refined) ? refined : coarse
}

interface PaperFit {
  corners: ReceiptImagePoint[]
  surrounded: boolean
}

function fitPaper(gray: Float32Array, width: number, height: number, quad: ReceiptImagePoint[]): PaperFit | null {
  const center = { x: quad.reduce((s, p) => s + p.x, 0) / 4, y: quad.reduce((s, p) => s + p.y, 0) / 4 }
  const search = quad.map(p => ({
    x: center.x + (p.x - center.x) * searchExpansion,
    y: center.y + (p.y - center.y) * searchExpansion,
  }))

  const region = new Uint8Array(width * height)
  const histogram = new Float64Array(256)
  let regionSize = 0
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (!inside(search, { x, y })) continue
      const index = y * width + x
      region[index] = 1
      histogram[Math.min(255, Math.max(0, Math.round(gray[index]!)))]!++
      regionSize++
    }
  }
  if (regionSize < 64) return null
  const threshold = otsu(histogram, regionSize)
  let dark = 0, darkSum = 0, brightSum = 0
  for (let i = 0; i < 256; i++) {
    if (i <= threshold) {
      dark += histogram[i]!
      darkSum += i * histogram[i]!
    } else {
      brightSum += i * histogram[i]!
    }
  }
  if (!dark || dark === regionSize || brightSum / (regionSize - dark) - darkSum / dark < 30) return null

  // Label bright connected regions reachable from inside the approximate outline and keep the
  // one covering most of it.
  const labels = new Int32Array(width * height)
  const queue = new Int32Array(width * height)
  let bestLabel = 0, bestOverlap = 0, nextLabel = 0
  const sizes: number[] = [0]
  for (let start = 0; start < labels.length; start++) {
    if (labels[start] || !region[start] || gray[start]! <= threshold) continue
    if (!inside(quad, { x: start % width, y: Math.floor(start / width) })) continue
    const label = ++nextLabel
    let head = 0, tail = 0, overlap = 0
    labels[start] = label
    queue[tail++] = start
    while (head < tail) {
      const index = queue[head++]!
      const x = index % width, y = Math.floor(index / width)
      if (inside(quad, { x, y })) overlap++
      const neighbors = [x > 0 ? index - 1 : -1, x < width - 1 ? index + 1 : -1, y > 0 ? index - width : -1, y < height - 1 ? index + width : -1]
      for (const next of neighbors) {
        if (next < 0 || labels[next] || !region[next] || gray[next]! <= threshold) continue
        labels[next] = label
        queue[tail++] = next
      }
    }
    sizes.push(tail)
    if (overlap > bestOverlap) {
      bestOverlap = overlap
      bestLabel = label
    }
  }
  const paperSize = sizes[bestLabel] ?? 0
  if (!bestLabel || bestOverlap < area(quad) * 0.35 || paperSize > regionSize * 0.92) return null

  let mx = 0, my = 0
  for (let index = 0; index < labels.length; index++) {
    if (labels[index] !== bestLabel) continue
    mx += index % width
    my += Math.floor(index / width)
  }
  mx /= paperSize
  my /= paperSize

  // The paper corner matching each approximate corner is the paper point furthest along that
  // corner's direction; for a (perspective) rectangle this lands exactly on the corner.
  const directions = quad.map(p => {
    const dx = p.x - center.x, dy = p.y - center.y
    const length = Math.hypot(dx, dy) || 1
    return { x: dx / length, y: dy / length }
  })
  const best = directions.map(() => ({ score: -Infinity, x: 0, y: 0 }))
  for (let index = 0; index < labels.length; index++) {
    if (labels[index] !== bestLabel) continue
    const x = index % width, y = Math.floor(index / width)
    for (let i = 0; i < 4; i++) {
      const score = (x - mx) * directions[i]!.x + (y - my) * directions[i]!.y
      if (score > best[i]!.score) best[i] = { score, x, y }
    }
  }
  const fitted = best.map(p => ({ x: p.x, y: p.y }))
  // A paper region that leaks into the background or a misread outline will not fill its quad.
  const fittedArea = area(fitted)
  if (!fittedArea || paperSize / fittedArea < 0.85 || paperSize / fittedArea > 1.15) return null
  let ring = 0, ringBright = 0
  for (let index = 0; index < labels.length; index++) {
    if (!region[index] || inside(fitted, { x: index % width, y: Math.floor(index / width) })) continue
    ring++
    if (gray[index]! > threshold) ringBright++
  }
  return { corners: fitted, surrounded: ring >= paperSize * 0.1 && ringBright < ring * 0.3 }
}
