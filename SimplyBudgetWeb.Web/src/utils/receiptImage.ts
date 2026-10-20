import type { ReceiptImageGeometry, ReceiptImagePoint } from '@/types'

export function imageBoundary(): ReceiptImageGeometry {
  return { version: 1, corners: [{ x: 0, y: 0 }, { x: 1, y: 0 }, { x: 1, y: 1 }, { x: 0, y: 1 }] }
}

export function validGeometry(value: ReceiptImageGeometry): boolean {
  if (value?.version !== 1 || value.corners?.length !== 4
    || value.corners.some(p => !p || !Number.isFinite(p.x) || !Number.isFinite(p.y)
      || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1)) return false
  let twiceArea = 0
  for (let i = 0; i < 4; i++) {
    const a = value.corners[i]!
    const b = value.corners[(i + 1) % 4]!
    const c = value.corners[(i + 2) % 4]!
    const dx = b.x - a.x
    const dy = b.y - a.y
    if (dx * dx + dy * dy < 0.000025 || dx * (c.y - b.y) - dy * (c.x - b.x) <= 0.00001)
      return false
    twiceArea += a.x * b.y - b.x * a.y
  }
  return twiceArea >= 0.002
}

export function boundedSize(width: number, height: number) {
  const scale = Math.min(1, 2200 / Math.max(width, height), Math.sqrt(4_000_000 / (width * height)))
  return { width: Math.max(2, Math.floor(width * scale)), height: Math.max(2, Math.floor(height * scale)) }
}

// A unit square -> quadrilateral homography, not a bounding-box or bilinear crop.
export function perspectiveMap(points: ReceiptImagePoint[]) {
  const [a, b, c, d] = points as ReceiptImageGeometry['corners']
  const dx1 = b.x - c.x, dx2 = d.x - c.x, dx3 = a.x - b.x + c.x - d.x
  const dy1 = b.y - c.y, dy2 = d.y - c.y, dy3 = a.y - b.y + c.y - d.y
  const determinant = dx1 * dy2 - dx2 * dy1
  const g = (dx3 * dy2 - dx2 * dy3) / determinant
  const h = (dx1 * dy3 - dx3 * dy1) / determinant
  return (u: number, v: number): ReceiptImagePoint => {
    const denominator = g * u + h * v + 1
    return {
      x: ((b.x - a.x + g * b.x) * u + (d.x - a.x + h * d.x) * v + a.x) / denominator,
      y: ((b.y - a.y + g * b.y) * u + (d.y - a.y + h * d.y) * v + a.y) / denominator,
    }
  }
}

export function flattenPixels(source: Pick<ImageData, 'width' | 'height' | 'data'>, geometry: ReceiptImageGeometry) {
  if (!validGeometry(geometry)) throw new Error('Corners must form a clockwise convex receipt. Move them apart without crossing.')
  const corners = geometry.corners.map(p => ({ x: p.x * (source.width - 1), y: p.y * (source.height - 1) }))
  const [a, b, c, d] = corners as ReceiptImageGeometry['corners']
  const distance = (p: ReceiptImagePoint, q: ReceiptImagePoint) => Math.hypot(p.x - q.x, p.y - q.y)
  const { width, height } = boundedSize(
    Math.round(Math.max(distance(a, b), distance(d, c))) + 1,
    Math.round(Math.max(distance(a, d), distance(b, c))) + 1,
  )
  const data = new Uint8ClampedArray(width * height * 4)
  const map = perspectiveMap(corners)
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const point = map(x / (width - 1), y / (height - 1))
      const sx = Math.max(0, Math.min(source.width - 1, point.x))
      const sy = Math.max(0, Math.min(source.height - 1, point.y))
      const left = Math.floor(sx), top = Math.floor(sy)
      const right = Math.min(left + 1, source.width - 1), bottom = Math.min(top + 1, source.height - 1)
      const fx = sx - left, fy = sy - top
      const topLeft = (top * source.width + left) * 4
      const topRight = (top * source.width + right) * 4
      const bottomLeft = (bottom * source.width + left) * 4
      const bottomRight = (bottom * source.width + right) * 4
      const offset = (y * width + x) * 4
      for (let channel = 0; channel < 4; channel++) {
        data[offset + channel] =
          source.data[topLeft + channel]! * (1 - fx) * (1 - fy) + source.data[topRight + channel]! * fx * (1 - fy)
          + source.data[bottomLeft + channel]! * (1 - fx) * fy + source.data[bottomRight + channel]! * fx * fy
      }
    }
  }
  return { width, height, data }
}

export async function prepareReceiptCanvas(blob: Blob): Promise<HTMLCanvasElement> {
  const url = URL.createObjectURL(blob)
  const image = new Image()
  try {
    // HTML images and canvas drawImage both honor EXIF orientation. Geometry always
    // refers to this oriented view, never the unrotated JPEG sensor coordinates.
    image.src = url
    await image.decode()
    if (!image.naturalWidth || !image.naturalHeight) throw new Error('Could not decode this receipt image.')
    const size = boundedSize(image.naturalWidth, image.naturalHeight)
    const canvas = document.createElement('canvas')
    canvas.width = size.width
    canvas.height = size.height
    const context = canvas.getContext('2d')
    if (!context) throw new Error('Image processing is not available in this browser.')
    context.drawImage(image, 0, 0, canvas.width, canvas.height)
    return canvas
  } finally {
    image.src = ''
    URL.revokeObjectURL(url)
  }
}

export async function flattenReceiptImage(blob: Blob, geometry: ReceiptImageGeometry): Promise<Blob> {
  const source = await prepareReceiptCanvas(blob)
  const output = document.createElement('canvas')
  try {
    const context = source.getContext('2d', { willReadFrequently: true })
    if (!context) throw new Error('Image processing is not available in this browser.')
    const pixels = flattenPixels(context.getImageData(0, 0, source.width, source.height), geometry)
    output.width = pixels.width
    output.height = pixels.height
    const destination = output.getContext('2d')
    if (!destination) throw new Error('Image processing is not available in this browser.')
    destination.putImageData(new ImageData(pixels.data, pixels.width, pixels.height), 0, 0)
    destination.globalCompositeOperation = 'destination-over'
    destination.fillStyle = '#fff'
    destination.fillRect(0, 0, output.width, output.height)
    return await new Promise<Blob>((resolve, reject) => {
      output.toBlob(result => result ? resolve(result) : reject(new Error('Could not build the flat receipt.')), 'image/jpeg', 0.92)
    })
  } finally {
    source.width = source.height = output.width = output.height = 0
  }
}
