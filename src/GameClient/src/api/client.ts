import type { GalaxyViewDto, GameStateDto, GameSystemDto } from './types'

type TokenProvider = () => Promise<string | null>

export class GameApiClient {
  constructor(private baseUrl: string, private getToken: TokenProvider) {}

  private async request<T>(path: string, init?: RequestInit): Promise<T> {
    const token = await this.getToken()
    const response = await fetch(`${this.baseUrl}${path}`, {
      ...init,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      }
    })

    if (!response.ok) throw new Error(`API ${response.status}: ${await response.text()}`)
    return response.json() as Promise<T>
  }

  state() { return this.request<GameStateDto>('/api/game/state') }
  galaxy() { return this.request<GalaxyViewDto>('/api/game/galaxy') }
  system(id: string) { return this.request<GameSystemDto>(`/api/game/system/${id}`) }

  command(payload: { commandId: string; type: string; payload: Record<string, unknown> }) {
    return this.request('/api/game/commands', { method: 'POST', body: JSON.stringify(payload) })
  }
}
