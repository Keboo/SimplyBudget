import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { apiClient, SessionExpiredError, setTokenProvider, setUnauthorizedHandler } from './apiClient'

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

describe('apiClient', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    setTokenProvider(async () => 'token-123')
    setUnauthorizedHandler(() => undefined)
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  describe('upload', () => {
    it('posts multipart form data with the bearer token and no explicit content type', async () => {
      fetchMock.mockResolvedValue(jsonResponse({ id: 7 }, 201))
      const form = new FormData()
      form.append('image', new File(['x'], 'receipt.jpg', { type: 'image/jpeg' }))

      const result = await apiClient.upload<{ id: number }>('/api/receipts', form)

      expect(result).toEqual({ id: 7 })
      const [url, init] = fetchMock.mock.calls[0]!
      expect(url).toBe('http://api.test/api/receipts')
      expect(init?.method).toBe('POST')
      expect(init?.body).toBe(form)
      const headers = init?.headers as Record<string, string>
      expect(headers.Authorization).toBe('Bearer token-123')
      expect(headers['Content-Type']).toBeUndefined()
    })

    it('surfaces the ProblemDetails detail instead of raw JSON', async () => {
      fetchMock.mockResolvedValue(jsonResponse({ title: 'Service Unavailable', status: 503, detail: 'Receipt storage is not configured.' }, 503))

      await expect(apiClient.upload('/api/receipts', new FormData()))
        .rejects.toThrow('Receipt storage is not configured.')
    })

    it('uses plain text error bodies as-is', async () => {
      fetchMock.mockResolvedValue(new Response('Upload a JPEG, PNG, or WebP receipt image.', { status: 400 }))

      await expect(apiClient.upload('/api/receipts', new FormData()))
        .rejects.toThrow('Upload a JPEG, PNG, or WebP receipt image.')
    })

    it('falls back to the status code when the body is empty', async () => {
      fetchMock.mockResolvedValue(new Response('', { status: 500 }))

      await expect(apiClient.upload('/api/receipts', new FormData()))
        .rejects.toThrow('HTTP error! status: 500')
    })

    it('invokes the unauthorized handler and throws SessionExpiredError on 401', async () => {
      const onUnauthorized = vi.fn()
      setUnauthorizedHandler(onUnauthorized)
      fetchMock.mockResolvedValue(new Response('', { status: 401 }))

      await expect(apiClient.upload('/api/receipts', new FormData())).rejects.toBeInstanceOf(SessionExpiredError)
      expect(onUnauthorized).toHaveBeenCalledOnce()
    })
  })

  describe('get / put', () => {
    it('returns parsed JSON from get', async () => {
      fetchMock.mockResolvedValue(jsonResponse([{ id: 1 }]))
      expect(await apiClient.get('/api/receipts')).toEqual([{ id: 1 }])
    })

    it('reports server messages for failed get requests', async () => {
      fetchMock.mockResolvedValue(jsonResponse({ message: 'Invalid operation' }, 400))
      await expect(apiClient.get('/api/receipts')).rejects.toThrow('Invalid operation')
    })

    it('sends JSON for put and parses the response', async () => {
      fetchMock.mockResolvedValue(jsonResponse({ id: 1, merchantName: 'Cafe' }))

      const result = await apiClient.put('/api/receipts/1', { merchantName: 'Cafe' })

      expect(result).toEqual({ id: 1, merchantName: 'Cafe' })
      const init = fetchMock.mock.calls[0]![1]!
      expect(init.method).toBe('PUT')
      expect((init.headers as Record<string, string>)['Content-Type']).toBe('application/json')
      expect(init.body).toBe('{"merchantName":"Cafe"}')
    })
  })

  describe('download', () => {
    it('returns the blob and file name', async () => {
      fetchMock.mockResolvedValue(new Response('img', {
        status: 200,
        headers: { 'Content-Disposition': 'attachment; filename="receipt.jpg"' },
      }))

      const { blob, fileName } = await apiClient.download('/api/receipts/1/image')

      expect(await blob.text()).toBe('img')
      expect(fileName).toBe('receipt.jpg')
    })

    it('surfaces problem details when the image cannot be loaded', async () => {
      fetchMock.mockResolvedValue(jsonResponse({ detail: 'Receipt storage is unavailable.' }, 503))
      await expect(apiClient.download('/api/receipts/1/image')).rejects.toThrow('Receipt storage is unavailable.')
    })
  })
})
