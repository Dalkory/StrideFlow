import type {
  AdSlotResponse,
  AuthResponse,
  AuthSession,
  DashboardResponse,
  LeaderboardPeriod,
  LeaderboardResponse,
  LiveSessionResponse,
  LoginRequest,
  ProblemDetailsPayload,
  RegisterRequest,
  StartSessionRequest,
  TrackSessionPointsRequest,
  UpdateProfileRequest,
  UserProfileResponse,
  WalkingSessionDetailResponse,
  WalkingSessionResponse,
} from './types'

const AUTH_STORAGE_KEY = 'strideflow.auth'

export class ApiError extends Error {
  status: number
  errorCode?: string
  fieldErrors?: Record<string, string[]>

  constructor(
    message: string,
    status: number,
    errorCode?: string,
    fieldErrors?: Record<string, string[]>,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errorCode = errorCode
    this.fieldErrors = fieldErrors
  }
}

class ApiClient {
  private refreshPromise: Promise<AuthSession | null> | null = null

  getSession(): AuthSession | null {
    if (typeof window === 'undefined') {
      return null
    }

    const raw = window.localStorage.getItem(AUTH_STORAGE_KEY)
    if (!raw) {
      return null
    }

    try {
      const parsed = JSON.parse(raw) as AuthSession
      if (!parsed.tokens?.access_token || !parsed.tokens?.refresh_token) {
        this.clearSession()
        return null
      }

      return parsed
    } catch {
      this.clearSession()
      return null
    }
  }

  getAccessToken(): string {
    return this.getSession()?.tokens.access_token ?? ''
  }

  saveSession(session: AuthSession): void {
    if (typeof window === 'undefined') {
      return
    }

    window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
  }

  clearSession(): void {
    if (typeof window === 'undefined') {
      return
    }

    window.localStorage.removeItem(AUTH_STORAGE_KEY)
  }

  async register(payload: RegisterRequest): Promise<AuthSession> {
    const response = await this.request<AuthResponse>(
      '/api/auth/register',
      {
        method: 'POST',
        body: JSON.stringify(payload),
      },
      { auth: false },
    )

    this.saveSession(response)
    return response
  }

  async login(payload: LoginRequest): Promise<AuthSession> {
    const response = await this.request<AuthResponse>(
      '/api/auth/login',
      {
        method: 'POST',
        body: JSON.stringify(payload),
      },
      { auth: false },
    )

    this.saveSession(response)
    return response
  }

  async logout(): Promise<void> {
    const session = this.getSession()
    if (!session) {
      return
    }

    try {
      await this.request<void>(
        '/api/auth/logout',
        {
          method: 'POST',
          body: JSON.stringify({ refresh_token: session.tokens.refresh_token }),
        },
        { auth: true, retryOnUnauthorized: false },
      )
    } finally {
      this.clearSession()
    }
  }

  me(): Promise<UserProfileResponse> {
    return this.request<UserProfileResponse>('/api/auth/me')
  }

  getDashboard(): Promise<DashboardResponse> {
    return this.request<DashboardResponse>('/api/dashboard')
  }

  getProfile(): Promise<UserProfileResponse> {
    return this.request<UserProfileResponse>('/api/profile')
  }

  updateProfile(payload: UpdateProfileRequest): Promise<UserProfileResponse> {
    return this.request<UserProfileResponse>('/api/profile', {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  }

  getHistory(): Promise<WalkingSessionResponse[]> {
    return this.request<WalkingSessionResponse[]>('/api/sessions')
  }

  getCurrentSession(): Promise<WalkingSessionDetailResponse | null> {
    return this.request<WalkingSessionDetailResponse | null>('/api/sessions/current')
  }

  getSessionById(sessionId: string): Promise<WalkingSessionDetailResponse> {
    return this.request<WalkingSessionDetailResponse>(`/api/sessions/${sessionId}`)
  }

  startSession(payload: StartSessionRequest): Promise<WalkingSessionDetailResponse> {
    return this.request<WalkingSessionDetailResponse>('/api/sessions', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  }

  addPoints(
    sessionId: string,
    payload: TrackSessionPointsRequest,
  ): Promise<WalkingSessionDetailResponse> {
    return this.request<WalkingSessionDetailResponse>(
      `/api/sessions/${sessionId}/points`,
      {
        method: 'POST',
        body: JSON.stringify(payload),
      },
    )
  }

  pauseSession(sessionId: string): Promise<WalkingSessionDetailResponse> {
    return this.request<WalkingSessionDetailResponse>(`/api/sessions/${sessionId}/pause`, {
      method: 'POST',
    })
  }

  resumeSession(sessionId: string): Promise<WalkingSessionDetailResponse> {
    return this.request<WalkingSessionDetailResponse>(`/api/sessions/${sessionId}/resume`, {
      method: 'POST',
    })
  }

  stopSession(sessionId: string): Promise<WalkingSessionDetailResponse> {
    return this.request<WalkingSessionDetailResponse>(`/api/sessions/${sessionId}/stop`, {
      method: 'POST',
    })
  }

  getLeaderboard(
    period: LeaderboardPeriod,
    limit = 10,
    city?: string,
  ): Promise<LeaderboardResponse> {
    const params = new URLSearchParams({
      period,
      limit: String(limit),
    })

    if (city && city.trim().length > 0) {
      params.set('city', city.trim())
    }

    return this.request<LeaderboardResponse>(`/api/leaderboard?${params.toString()}`)
  }

  getLiveMap(): Promise<LiveSessionResponse[]> {
    return this.request<LiveSessionResponse[]>('/api/live/map')
  }

  getAdSlots(): Promise<AdSlotResponse[]> {
    return this.request<AdSlotResponse[]>('/api/ads/slots')
  }

  private async request<T>(
    path: string,
    init: RequestInit = {},
    options: { auth?: boolean; retryOnUnauthorized?: boolean } = {},
  ): Promise<T> {
    const requiresAuth = options.auth ?? true
    const retryOnUnauthorized = options.retryOnUnauthorized ?? true
    const headers = new Headers(init.headers ?? {})

    if (init.body && !(init.body instanceof FormData) && !headers.has('Content-Type')) {
      headers.set('Content-Type', 'application/json')
    }

    if (requiresAuth) {
      const token = this.getAccessToken()
      if (!token) {
        throw new ApiError('Требуется авторизация.', 401, 'unauthorized')
      }

      headers.set('Authorization', `Bearer ${token}`)
    }

    const response = await fetch(path, {
      ...init,
      headers,
    })

    if (response.status === 401 && requiresAuth && retryOnUnauthorized) {
      const refreshedSession = await this.refreshSession()
      if (refreshedSession) {
        return this.request<T>(path, init, { auth: requiresAuth, retryOnUnauthorized: false })
      }
    }

    if (!response.ok) {
      throw await this.buildError(response)
    }

    if (response.status === 204) {
      return undefined as T
    }

    const contentType = response.headers.get('content-type') ?? ''
    if (!contentType.includes('json')) {
      return undefined as T
    }

    return (await response.json()) as T
  }

  private async refreshSession(): Promise<AuthSession | null> {
    if (this.refreshPromise) {
      return this.refreshPromise
    }

    const session = this.getSession()
    if (!session?.tokens.refresh_token) {
      this.clearSession()
      return null
    }

    this.refreshPromise = this.request<AuthResponse>(
      '/api/auth/refresh',
      {
        method: 'POST',
        body: JSON.stringify({ refresh_token: session.tokens.refresh_token }),
      },
      { auth: false, retryOnUnauthorized: false },
    )
      .then((response) => {
        this.saveSession(response)
        return response
      })
      .catch(() => {
        this.clearSession()
        return null
      })
      .finally(() => {
        this.refreshPromise = null
      })

    return this.refreshPromise
  }

  private async buildError(response: Response): Promise<ApiError> {
    const payload = await this.tryReadProblem(response)
    return new ApiError(
      payload?.detail ?? 'Не удалось выполнить запрос.',
      payload?.status ?? response.status,
      payload?.error_code,
      payload?.errors,
    )
  }

  private async tryReadProblem(response: Response): Promise<ProblemDetailsPayload | null> {
    try {
      return (await response.json()) as ProblemDetailsPayload
    } catch {
      return null
    }
  }
}

export const apiClient = new ApiClient()
