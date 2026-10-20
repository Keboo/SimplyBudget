import { afterEach, describe, expect, it, vi } from 'vitest'
import type { ReceiptImageGeometry } from '@/types'
import { boundedSize, flattenPixels, imageBoundary, prepareReceiptCanvas, validGeometry } from './receiptImage'

afterEach(() => vi.restoreAllMocks())

describe('receipt perspective reconstruction', () => {
  it('maps a trapezoid to a rectangle, including its projective interior rather than its bounding box', () => {
    const data = new Uint8ClampedArray(101 * 101 * 4)
    for (let y = 0; y < 101; y++) {
      for (let x = 0; x < 101; x++) data.set([x, y, 0, 255], (y * 101 + x) * 4)
    }
    const geometry: ReceiptImageGeometry = {
      version: 1, corners: [{ x: 0.2, y: 0.1 }, { x: 0.8, y: 0.1 }, { x: 1, y: 0.9 }, { x: 0, y: 0.9 }],
    }
    const output = flattenPixels({ width: 101, height: 101, data }, geometry)
    const pixel = (x: number, y: number) => [...output.data.slice((y * output.width + x) * 4, (y * output.width + x) * 4 + 4)]
    expect([output.width, output.height]).toEqual([101, 83])
    expect(pixel(0, 0)).toEqual([20, 10, 0, 255])
    expect(pixel(100, 0)).toEqual([80, 10, 0, 255])
    expect(pixel(100, 82)).toEqual([100, 90, 0, 255])
    expect(pixel(0, 82)).toEqual([0, 90, 0, 255])
    expect(pixel(50, 41)).toEqual([50, 40, 0, 255]) // A bounding-box/bilinear crop would sample y=50.
  })

  it('retains every pixel for a full-boundary correction and never modifies source bytes', () => {
    const data = new Uint8ClampedArray(Array.from({ length: 3 * 3 * 4 }, (_, i) => i * 7))
    const original = data.slice()
    const output = flattenPixels({ width: 3, height: 3, data }, imageBoundary())
    expect(output.data).toEqual(original)
    expect(data).toEqual(original)
  })

  it('reconstructs a clockwise rotation from corner order without rotating or overwriting the original', () => {
    const data = new Uint8ClampedArray([
      1, 0, 0, 255, 2, 0, 0, 255, 3, 0, 0, 255,
      4, 0, 0, 255, 5, 0, 0, 255, 6, 0, 0, 255,
    ])
    const [a, b, c, d] = imageBoundary().corners
    const output = flattenPixels({ width: 3, height: 2, data }, { version: 1, corners: [d, a, b, c] })
    expect([output.width, output.height]).toEqual([2, 3])
    expect([...output.data].filter((_, i) => i % 4 === 0)).toEqual([4, 1, 5, 2, 6, 3])
    expect(data[0]).toBe(1)
  })

  it.each([
    [{ x: 0, y: 0 }, { x: 1, y: 1 }, { x: 1, y: 0 }, { x: 0, y: 1 }],
    [{ x: 0, y: 0 }, { x: 0, y: 1 }, { x: 1, y: 1 }, { x: 1, y: 0 }],
    [{ x: 0, y: 0 }, { x: 1, y: 0 }, { x: 0.2, y: 0.1 }, { x: 1, y: 1 }],
    [{ x: 0, y: 0 }, { x: 0.001, y: 0 }, { x: 0.001, y: 1 }, { x: 0, y: 1 }],
    [{ x: Number.NaN, y: 0 }, { x: 1, y: 0 }, { x: 1, y: 1 }, { x: 0, y: 1 }],
    [{ x: -0.1, y: 0 }, { x: 1, y: 0 }, { x: 1, y: 1 }, { x: 0, y: 1 }],
  ])('rejects crossed, counterclockwise, concave, tiny, nonfinite, or out-of-image corners: %j', (...corners) => {
    const geometry = { version: 1, corners } as ReceiptImageGeometry
    expect(validGeometry(geometry)).toBe(false)
    expect(() => flattenPixels({ width: 2, height: 2, data: new Uint8ClampedArray(16) }, geometry)).toThrow('Corners')
  })

  it('bounds phone-photo source and output sizes by both dimension and area', () => {
    for (const [width, height] of [[8000, 6000], [6000, 8000], [10000, 10000], [100, 30000]]) {
      const size = boundedSize(width!, height!)
      expect(Math.max(size.width, size.height)).toBeLessThanOrEqual(2200)
      expect(size.width * size.height).toBeLessThanOrEqual(4_000_000)
      expect(size.width / size.height).toBeCloseTo(width! / height!, 1)
    }
  })

  it('uses the browser-oriented image dimensions and releases the decode URL on success or failure', async () => {
    const drawImage = vi.fn()
    vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage } as unknown as CanvasRenderingContext2D)
    const decode = vi.fn().mockResolvedValue(undefined)
    vi.spyOn(globalThis, 'Image').mockImplementation(function () {
      return { naturalWidth: 3000, naturalHeight: 4000, decode, src: '' } as unknown as HTMLImageElement
    })
    URL.createObjectURL = vi.fn().mockReturnValue('blob:decode')
    URL.revokeObjectURL = vi.fn()
    const canvas = await prepareReceiptCanvas(new Blob(['oriented jpeg']))
    expect([canvas.width, canvas.height]).toEqual([1650, 2200])
    expect(drawImage).toHaveBeenCalledWith(expect.objectContaining({ naturalWidth: 3000, naturalHeight: 4000 }), 0, 0, 1650, 2200)
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:decode')
    decode.mockRejectedValue(new Error('Decode failed'))
    await expect(prepareReceiptCanvas(new Blob(['bad image']))).rejects.toThrow('Decode failed')
    expect(URL.revokeObjectURL).toHaveBeenCalledTimes(2)
  })
})
