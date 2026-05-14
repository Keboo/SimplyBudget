let getTokenFn: (() => Promise<string>) | null = null

export function setTokenProvider(fn: () => Promise<string>) {
  getTokenFn = fn
}

class ApiClient {
  private baseUrl = __API_BASE_URL__ || ''

  private async getHeaders(): Promise<HeadersInit> {
    if (getTokenFn) {
      try {
        const token = await getTokenFn()
        return { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' }
      } catch { /* fall through */ }
    }
    return { 'Content-Type': 'application/json' }
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
      body: data ? JSON.stringify(data) : undefined,
    })
    if (!response.ok) {
      const error = await response.text()
      throw new Error(error || `HTTP error! status: ${response.status}`)
    }
    if (response.status === 204) return undefined as T
    const text = await response.text()
    return text ? JSON.parse(text) : undefined as T
  }

  async put<T = void>(url: string, data?: unknown): Promise<T> {
    const headers = await this.getHeaders()
    const response = await fetch(this.baseUrl + url, {
      method: 'PUT', headers,
      body: data ? JSON.stringify(data) : undefined,
    })
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`)
    if (response.status === 204) return undefined as T
    return response.json()
  }

  async delete<T = void>(url: string): Promise<T> {
    const headers = await this.getHeaders()
    const response = await fetch(this.baseUrl + url, { method: 'DELETE', headers })
    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`)
    if (response.status === 204) return undefined as T
    return response.json()
  }
}

export const apiClient = new ApiClient()
