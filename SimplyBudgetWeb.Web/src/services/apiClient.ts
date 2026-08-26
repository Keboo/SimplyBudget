let getTokenFn: (() => Promise<string>) | null = null

export function setTokenProvider(fn: () => Promise<string>) {
  getTokenFn = fn
}

interface DeleteOptions {
  ifMatch?: string
}

class ApiClient {
  private baseUrl = __API_BASE_URL__ || ''

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
      try {
        const token = await getTokenFn()
        return { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' }
      } catch { /* fall through */ }
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
