import { apiClient } from '@/services/apiClient'
import type { ReceiptImageGeometry } from '@/types'
import { preferPortrait, refineReceiptCorners } from '@/utils/receiptEdges'
import { prepareReceiptCanvas, validGeometry } from '@/utils/receiptImage'

const maxDetectionEdge = 1600

// Sends an EXIF-oriented, downscaled JPEG to the Foundry-backed detector so the returned
// corners use the same oriented coordinate space as ReceiptImageGeometry.
export async function detectReceiptCorners(file: Blob): Promise<ReceiptImageGeometry | null> {
  const source = await prepareReceiptCanvas(file)
  const output = document.createElement('canvas')
  try {
    const scale = Math.min(1, maxDetectionEdge / Math.max(source.width, source.height))
    output.width = Math.max(1, Math.round(source.width * scale))
    output.height = Math.max(1, Math.round(source.height * scale))
    const context = output.getContext('2d')
    if (!context) throw new Error('Image processing is not available in this browser.')
    context.drawImage(source, 0, 0, output.width, output.height)
    const jpeg = await new Promise<Blob>((resolve, reject) => {
      output.toBlob(blob => blob ? resolve(blob) : reject(new Error('Could not prepare the receipt for detection.')), 'image/jpeg', 0.85)
    })
    const form = new FormData()
    form.append('image', new File([jpeg], 'receipt.jpg', { type: 'image/jpeg' }))
    const result = await apiClient.upload<{ geometry: ReceiptImageGeometry | null }>('/api/receipts/detect-corners', form)
    if (!result?.geometry || !validGeometry(result.geometry)) return null
    const pixels = context.getImageData(0, 0, output.width, output.height)
    return preferPortrait(refineReceiptCorners(pixels, result.geometry), output.width, output.height)
  } finally {
    source.width = source.height = output.width = output.height = 0
  }
}
