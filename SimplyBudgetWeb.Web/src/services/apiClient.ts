let getTokenFn: (() => Promise<string>) | null = null
let onUnauthorizedFn: (() => void | Promise<void>) | null = null

export function setTokenProvider(fn: () => Promise<string>) {
  getTokenFn = fn
}

/**
 * Registers the callback invoked when the API rejects a request because the
 * caller is no longer authenticated (expired/invalid token).
 */
export function setUnauthorizedHandler(fn: () => void | Promise<void>) {
  onUnauthorizedFn = fn
}

export class SessionExpiredError extends Error {
  constructor(message = 'Your session has expired. Please sign in again.') {
    super(message)
    this.name = 'SessionExpiredError'
  }
}

interface DeleteOptions {
  ifMatch?: string
}

class ApiClient {
  private baseUrl = __API_BASE_URL__ || ''

  private async throwIfUnauthorized(response: Response): Promise<void> {
    if (response.status !== 401) return
    await onUnauthorizedFn?.()
    throw new SessionExpiredError()
  }

  private parseFileName(contentDisposition: string | null): string | null {
    if (!contentDisposition) return null

    const encodedMatch = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i)
    if (encodedMatch?.[1]) {
      return decodeURIComponent(encodedMatch[1].trim())
    }

    const basicMatch = contentDisposition.match(/filename="?([^"]+)"?/i)
    return basicMatch?.[1]?.trim() ?? null
  }

  private async getHeaders(): Promise<HeadersInit> {
    if (getTokenFn) {
      // Any failure here (including an unrecoverable session) propagates: all
      // API endpoints require auth, so an anonymous request would only 401.
      const token = await getTokenFn()
      return { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }
    }
    return { 'Content-Type': 'application/json' }
  }

  private buildJsonBody(data?: unknown): string | undefined {
    if (!data) return undefined
    // Preserve literal question marks in values while avoiding raw '?' in request payload text.
    return JSON.stringify(data).replace(/\?/g, '\\u003F')
  }

  async get<T>(url: string): Promise<T> {
    const headers = await this.getHeaders()
    const response = await fetch(this.baseUrl + url, { headers })
    await this.throwIfUnauthorized(response)
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`)
    const text = await response.text()
    return text ? JSON.parse(text) : undefined as T
  }

  async post<T = void>(url: string, data?: unknown): Promise<T> {
    const headers = await this.getHeaders()
    const response = await fetch(this.baseUrl + url, {
      method: 'POST', headers,
      body: this.buildJsonBody(data),
    })
    await this.throwIfUnauthorized(response)
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || `HTTP error! status: ${response.status}`)
    }
    if (response.status === 204) return undefined as T
    const text = await response.text()
    return text ? JSON.parse(text) : undefined as T
  }

  async download(url: string): Promise<{ blob: Blob; fileName: string | null }> {
    const headers = await this.getHeaders()
    const response = await fetch(this.baseUrl + url, { headers })
    await this.throwIfUnauthorized(response)
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || `HTTP error! status: ${response.status}`)
    }

    return {
      blob: await response.blob(),
      fileName: this.parseFileName(response.headers.get('content-disposition')),
    }
  }

  async put<T = void>(url: string, data?: unknown): Promise<T> {
    const headers = await this.getHeaders()
    const response = await fetch(this.baseUrl + url, {
      method: 'PUT', headers,
      body: this.buildJsonBody(data),
    })
    await this.throwIfUnauthorized(response)
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`)
    if (response.status === 204) return undefined as T
    return response.json()
  }

  async delete<T = void>(url: string, options?: DeleteOptions): Promise<T> {
    const headers = await this.getHeaders()
    const requestHeaders = options?.ifMatch
      ? {
          ...headers,
          'If-Match': options.ifMatch,
        }
      : headers
    const response = await fetch(this.baseUrl + url, { method: 'DELETE', headers: requestHeaders })
    await this.throwIfUnauthorized(response)
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || `HTTP error! status: ${response.status}`)
    }
    if (response.status === 204) return undefined as T
    const text = await response.text()
    return text ? JSON.parse(text) : undefined as T
  }
}

export const apiClient = new ApiClient()
