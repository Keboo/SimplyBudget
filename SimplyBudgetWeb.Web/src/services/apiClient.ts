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
  private requestTimeoutMs = 30_000
  private tokenTimeoutMs = 15_000

  private async withTimeout<T>(
    promiseFactory: () => Promise<T>,
    timeoutMs: number,
    timeoutMessage: string,
  ): Promise<T> {
    let timeoutId: ReturnType<typeof setTimeout> | null = null
    const timeoutPromise = new Promise<never>((_, reject) => {
      timeoutId = setTimeout(() => reject(new Error(timeoutMessage)), timeoutMs)
    })

    try {
      return await Promise.race([promiseFactory(), timeoutPromise]) as T
    } finally {
      if (timeoutId !== null) {
        clearTimeout(timeoutId)
      }
    }
  }

  private async fetchWithTimeout(url: string, init: RequestInit): Promise<Response> {
    const abortController = new AbortController()
    const timeoutId = setTimeout(() => abortController.abort(), this.requestTimeoutMs)

    try {
      return await fetch(this.baseUrl + url, {
        ...init,
        signal: abortController.signal,
      })
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error('Request timed out. Please try again.')
      }
      throw error
    } finally {
      clearTimeout(timeoutId)
    }
  }

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
      const tokenProvider = getTokenFn
      // Any failure here (including an unrecoverable session) propagates: all
      // API endpoints require auth, so an anonymous request would only 401.
      const token = await this.withTimeout(
        () => tokenProvider(),
        this.tokenTimeoutMs,
        'Authentication timed out. Please sign in again.',
      )
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
    const response = await this.fetchWithTimeout(url, { headers })
    await this.throwIfUnauthorized(response)
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`)
    const text = await response.text()
    return text ? JSON.parse(text) : undefined as T
  }

  async post<T = void>(url: string, data?: unknown): Promise<T> {
    const headers = await this.getHeaders()
    const response = await this.fetchWithTimeout(url, {
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
    const response = await this.fetchWithTimeout(url, { headers })
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
    const response = await this.fetchWithTimeout(url, {
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
    const response = await this.fetchWithTimeout(url, { method: 'DELETE', headers: requestHeaders })
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
