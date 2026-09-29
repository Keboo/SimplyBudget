import type cvModule from '@techstark/opencv-js'

type OpenCvRuntime = typeof cvModule & {
  onRuntimeInitialized?: () => void
  onAbort?: (reason: string) => void
}

let openCvReady: Promise<OpenCvRuntime> | null = null

async function getOpenCv(): Promise<OpenCvRuntime> {
  const module = await import('@techstark/opencv-js')
  const loadedModule = module.default as OpenCvRuntime | Promise<OpenCvRuntime>
  const cv = loadedModule instanceof Promise ? await loadedModule : loadedModule
  if (cv.Mat) return cv
  if (!openCvReady) {
    openCvReady = new Promise((resolve, reject) => {
      const timeout = window.setTimeout(() => reject(new Error('Image processing did not initialize')), 20_000)
      cv.onRuntimeInitialized = () => {
        window.clearTimeout(timeout)
        resolve(cv)
      }
      cv.onAbort = reason => {
        window.clearTimeout(timeout)
        reject(new Error(`Image processing failed to initialize: ${reason}`))
      }
    })
  }
  return openCvReady
}

interface Point {
  x: number
  y: number
}

function orderCorners(points: Point[]): [Point, Point, Point, Point] {
  const bySum = [...points].sort((a, b) => (a.x + a.y) - (b.x + b.y))
  const topLeft = bySum[0]!
  const bottomRight = bySum[3]!
  const remaining = points.filter(point => point !== topLeft && point !== bottomRight)
  const topRight = remaining[0]!.x - remaining[0]!.y > remaining[1]!.x - remaining[1]!.y
    ? remaining[0]!
    : remaining[1]!
  const bottomLeft = remaining[0] === topRight ? remaining[1]! : remaining[0]!
  return [topLeft, topRight, bottomRight, bottomLeft]
}

function detectReceiptCorners(cv: OpenCvRuntime, source: InstanceType<typeof cvModule.Mat>): Point[] | null {
  const gray = new cv.Mat()
  const blurred = new cv.Mat()
  const edges = new cv.Mat()
  const contours = new cv.MatVector()
  const hierarchy = new cv.Mat()
  const kernel = cv.getStructuringElement(cv.MORPH_RECT, new cv.Size(3, 3))

  try {
    cv.cvtColor(source, gray, cv.COLOR_RGBA2GRAY)
    cv.GaussianBlur(gray, blurred, new cv.Size(5, 5), 0)
    cv.Canny(blurred, edges, 50, 150)
    cv.dilate(edges, edges, kernel)
    cv.findContours(edges, contours, hierarchy, cv.RETR_LIST, cv.CHAIN_APPROX_SIMPLE)

    let best: Point[] | null = null
    let bestArea = source.rows * source.cols * 0.12
    for (let index = 0; index < contours.size(); index++) {
      const contour = contours.get(index)
      const approximation = new cv.Mat()
      try {
        cv.approxPolyDP(contour, approximation, 0.02 * cv.arcLength(contour, true), true)
        const area = Math.abs(cv.contourArea(approximation))
        if (
          approximation.rows === 4
          && area > bestArea
          && cv.isContourConvex(approximation)
        ) {
          bestArea = area
          best = Array.from({ length: 4 }, (_, pointIndex) => ({
            x: approximation.data32S[pointIndex * 2]!,
            y: approximation.data32S[pointIndex * 2 + 1]!,
          }))
        }
      } finally {
        contour.delete()
        approximation.delete()
      }
    }
    return best
  } finally {
    gray.delete()
    blurred.delete()
    edges.delete()
    contours.delete()
    hierarchy.delete()
    kernel.delete()
  }
}

function findFallbackBounds(imageData: ImageData): { x: number; y: number; width: number; height: number } | null {
  const { data, width, height } = imageData
  const corners = [
    [0, 0],
    [width - 1, 0],
    [0, height - 1],
    [width - 1, height - 1],
  ].map(([x, y]) => {
    const offset = (y! * width + x!) * 4
    return [data[offset]!, data[offset + 1]!, data[offset + 2]!]
  })
  const background = [0, 1, 2].map(channel =>
    corners.reduce((sum, corner) => sum + corner[channel]!, 0) / corners.length,
  )
  let left = width
  let top = height
  let right = -1
  let bottom = -1
  for (let y = 0; y < height; y += 2) {
    for (let x = 0; x < width; x += 2) {
      const offset = (y * width + x) * 4
      const difference = Math.abs(data[offset]! - background[0]!)
        + Math.abs(data[offset + 1]! - background[1]!)
        + Math.abs(data[offset + 2]! - background[2]!)
      if (difference > 75) {
        left = Math.min(left, x)
        top = Math.min(top, y)
        right = Math.max(right, x)
        bottom = Math.max(bottom, y)
      }
    }
  }
  if (right < 0 || bottom < 0 || (right - left) * (bottom - top) < width * height * 0.1)
    return null
  const padding = Math.round(Math.max(right - left, bottom - top) * 0.015)
  left = Math.max(0, left - padding)
  top = Math.max(0, top - padding)
  right = Math.min(width - 1, right + padding)
  bottom = Math.min(height - 1, bottom + padding)
  return { x: left, y: top, width: right - left + 1, height: bottom - top + 1 }
}

function toJpeg(canvas: HTMLCanvasElement): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob(blob => {
      if (blob) resolve(blob)
      else reject(new Error('Could not create the cropped receipt image'))
    }, 'image/jpeg', 0.92)
  })
}

export async function flattenReceiptImage(file: File): Promise<File> {
  const image = await createImageBitmap(file)
  const scale = Math.min(1, 2200 / Math.max(image.width, image.height))
  const width = Math.max(1, Math.round(image.width * scale))
  const height = Math.max(1, Math.round(image.height * scale))
  const sourceCanvas = document.createElement('canvas')
  sourceCanvas.width = width
  sourceCanvas.height = height
  const sourceContext = sourceCanvas.getContext('2d', { willReadFrequently: true })
  if (!sourceContext) {
    image.close()
    throw new Error('Image processing is not available in this browser')
  }
  sourceContext.drawImage(image, 0, 0, width, height)
  image.close()

  const cv = await getOpenCv()
  const source = cv.imread(sourceCanvas)
  const corners = detectReceiptCorners(cv, source)
  const output = document.createElement('canvas')

  try {
    if (corners) {
      const [topLeft, topRight, bottomRight, bottomLeft] = orderCorners(corners)
      const outputWidth = Math.round(Math.max(
        Math.hypot(topRight.x - topLeft.x, topRight.y - topLeft.y),
        Math.hypot(bottomRight.x - bottomLeft.x, bottomRight.y - bottomLeft.y),
      ))
      const outputHeight = Math.round(Math.max(
        Math.hypot(bottomLeft.x - topLeft.x, bottomLeft.y - topLeft.y),
        Math.hypot(bottomRight.x - topRight.x, bottomRight.y - topRight.y),
      ))
      const sourcePoints = cv.matFromArray(4, 1, cv.CV_32FC2, [
        topLeft.x, topLeft.y, topRight.x, topRight.y,
        bottomRight.x, bottomRight.y, bottomLeft.x, bottomLeft.y,
      ])
      const destinationPoints = cv.matFromArray(4, 1, cv.CV_32FC2, [
        0, 0, outputWidth - 1, 0,
        outputWidth - 1, outputHeight - 1, 0, outputHeight - 1,
      ])
      const transform = cv.getPerspectiveTransform(sourcePoints, destinationPoints)
      const flattened = new cv.Mat()
      try {
        cv.warpPerspective(
          source,
          flattened,
          transform,
          new cv.Size(outputWidth, outputHeight),
          cv.INTER_CUBIC,
          cv.BORDER_REPLICATE,
        )
        output.width = flattened.cols
        output.height = flattened.rows
        cv.imshow(output, flattened)
      } finally {
        sourcePoints.delete()
        destinationPoints.delete()
        transform.delete()
        flattened.delete()
      }
    } else {
      const bounds = findFallbackBounds(sourceContext.getImageData(0, 0, width, height))
      if (bounds) {
        output.width = bounds.width
        output.height = bounds.height
        output.getContext('2d')!.drawImage(
          sourceCanvas,
          bounds.x,
          bounds.y,
          bounds.width,
          bounds.height,
          0,
          0,
          bounds.width,
          bounds.height,
        )
      } else {
        output.width = width
        output.height = height
        output.getContext('2d')!.drawImage(sourceCanvas, 0, 0)
      }
    }
  } finally {
    source.delete()
  }

  const croppedBlob = await toJpeg(output)
  const fileName = `${file.name.replace(/\.[^.]+$/, '') || 'receipt'}.jpg`
  return new File([croppedBlob], fileName, { type: 'image/jpeg', lastModified: Date.now() })
}
