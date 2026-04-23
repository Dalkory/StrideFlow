import type { CSSProperties, FormEvent } from 'react'
import {
  startTransition,
  useCallback,
  useDeferredValue,
  useEffect,
  useEffectEvent,
  useRef,
  useState,
} from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import {
  Activity,
  Clock3,
  Flame,
  Footprints,
  LogOut,
  Pause,
  Play,
  RadioTower,
  Route,
  ShieldCheck,
  Sparkles,
  Square,
  Trophy,
  Users,
  Waves,
  Zap,
} from 'lucide-react'

import { AdSlot } from './components/AdSlot'
import { MapPanel } from './components/MapPanel'
import { StatCard } from './components/StatCard'
import { ApiError, apiClient } from './lib/api'
import {
  formatCalories,
  formatCompact,
  formatDateLabel,
  formatDateTime,
  formatDistance,
  formatDuration,
  formatNumber,
  formatPace,
  formatPercent,
  formatRelativeTime,
  formatSpeed,
  normalizeCityFilter,
} from './lib/format'
import type {
  AdSlotResponse,
  AuthSession,
  LeaderboardEntryResponse,
  LeaderboardPeriod,
  LeaderboardResponse,
  LiveSessionResponse,
  LoginRequest,
  RegisterRequest,
  TrackPointRequest,
  UpdateProfileRequest,
  UserProfileResponse,
  WalkingSessionDetailResponse,
  WalkingSessionResponse,
} from './lib/types'

type PanelKey = 'overview' | 'tracking' | 'community' | 'profile'
type AuthMode = 'login' | 'register'
type TrackingMode = 'gps' | 'demo' | null
type NoticeTone = 'success' | 'error' | 'info'

interface Notice {
  tone: NoticeTone
  text: string
}

const DEFAULT_DEMO_CENTER = {
  latitude: 55.751244,
  longitude: 37.618423,
}

function createRegisterDraft(): RegisterRequest {
  return {
    email: '',
    username: '',
    display_name: '',
    password: '',
    height_cm: 176,
    weight_kg: 72,
    daily_step_goal: 10000,
    city: 'Moscow',
    time_zone_id: Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC',
  }
}

function createLoginDraft(): LoginRequest {
  return {
    email: '',
    password: '',
    device_name: 'Web dashboard',
  }
}

function profileToDraft(profile: UserProfileResponse): UpdateProfileRequest {
  return {
    display_name: profile.display_name,
    bio: profile.bio,
    city: profile.city,
    time_zone_id: profile.time_zone_id,
    accent_color: profile.accent_color,
    height_cm: profile.height_cm,
    weight_kg: profile.weight_kg,
    step_length_meters: profile.step_length_meters,
    daily_step_goal: profile.daily_step_goal,
    is_profile_public: profile.is_profile_public,
  }
}

function getFieldError(errors: Record<string, string[]>, key: string): string | null {
  return errors[key]?.[0] ?? null
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return fallback
}

function buildNotice(tone: NoticeTone, text: string): Notice {
  return { tone, text }
}

function upsertHistory(
  history: WalkingSessionResponse[],
  session: WalkingSessionResponse,
): WalkingSessionResponse[] {
  return [session, ...history.filter((item) => item.id !== session.id)].slice(0, 40)
}

function isActiveSession(session: WalkingSessionDetailResponse | null): boolean {
  return session?.session.status === 'active'
}

function App() {
  const [authSession, setAuthSession] = useState<AuthSession | null>(() => apiClient.getSession())
  const [authMode, setAuthMode] = useState<AuthMode>('register')
  const [pendingAction, setPendingAction] = useState<string | null>(null)
  const [workspaceLoading, setWorkspaceLoading] = useState(false)
  const [notice, setNotice] = useState<Notice | null>(null)
  const [registerDraft, setRegisterDraft] = useState<RegisterRequest>(createRegisterDraft)
  const [loginDraft, setLoginDraft] = useState<LoginRequest>(createLoginDraft)
  const [profileDraft, setProfileDraft] = useState<UpdateProfileRequest | null>(null)
  const [authErrors, setAuthErrors] = useState<Record<string, string[]>>({})
  const [profileErrors, setProfileErrors] = useState<Record<string, string[]>>({})
  const [sessionErrors, setSessionErrors] = useState<Record<string, string[]>>({})
  const [activePanel, setActivePanel] = useState<PanelKey>('overview')
  const [sessionName, setSessionName] = useState('Вечерняя прогулка')
  const [dashboard, setDashboard] = useState<{
    user: UserProfileResponse
    summary: {
      today_steps: number
      today_distance_meters: number
      today_calories_burned: number
      active_minutes_today: number
      current_streak_days: number
      goal_progress_percent: number
      weekly_steps: number
      weekly_distance_meters: number
    }
    current_session: WalkingSessionDetailResponse | null
    recent_sessions: WalkingSessionResponse[]
    weekly_trend: {
      date: string
      steps: number
      distance_meters: number
      calories_burned: number
    }[]
    leaderboard: LeaderboardResponse
    ad_slots: AdSlotResponse[]
    active_walkers: LiveSessionResponse[]
  } | null>(null)
  const [profile, setProfile] = useState<UserProfileResponse | null>(null)
  const [history, setHistory] = useState<WalkingSessionResponse[]>([])
  const [currentSession, setCurrentSession] = useState<WalkingSessionDetailResponse | null>(null)
  const [leaderboard, setLeaderboard] = useState<LeaderboardResponse | null>(null)
  const [leaderboardPeriod, setLeaderboardPeriod] = useState<LeaderboardPeriod>('week')
  const [leaderboardCity, setLeaderboardCity] = useState('all')
  const deferredLeaderboardCity = useDeferredValue(leaderboardCity)
  const [liveSessions, setLiveSessions] = useState<LiveSessionResponse[]>([])
  const [adSlots, setAdSlots] = useState<AdSlotResponse[]>([])
  const [trackingSource, setTrackingSource] = useState<TrackingMode>(null)
  const [trackingPermission, setTrackingPermission] = useState<
    'idle' | 'granted' | 'denied' | 'unsupported'
  >('idle')
  const [bufferedPoints, setBufferedPoints] = useState(0)
  const [isSyncingPoints, setIsSyncingPoints] = useState(false)
  const [lastSyncedAt, setLastSyncedAt] = useState<string | null>(null)

  const currentSessionRef = useRef<WalkingSessionDetailResponse | null>(null)
  const geoWatchIdRef = useRef<number | null>(null)
  const demoTimerRef = useRef<number | null>(null)
  const flushTimerRef = useRef<number | null>(null)
  const pendingPointsRef = useRef<TrackPointRequest[]>([])
  const preferredTrackingModeRef = useRef<'gps' | 'demo'>('gps')
  const demoOriginRef = useRef(DEFAULT_DEMO_CENTER)
  const demoTickRef = useRef(0)

  useEffect(() => {
    currentSessionRef.current = currentSession
  }, [currentSession])

  useEffect(() => {
    if (!notice) {
      return
    }

    const timeoutId = window.setTimeout(() => {
      setNotice(null)
    }, 4200)

    return () => {
      window.clearTimeout(timeoutId)
    }
  }, [notice])

  const applyWorkspace = useEffectEvent(
    (
      dashboardResponse: NonNullable<typeof dashboard>,
      profileResponse: UserProfileResponse,
      historyResponse: WalkingSessionResponse[],
      liveMapResponse: LiveSessionResponse[],
      adSlotsResponse: AdSlotResponse[],
    ) => {
      startTransition(() => {
        setAuthSession(apiClient.getSession())
        setDashboard(dashboardResponse)
        setProfile(profileResponse)
        setProfileDraft(profileToDraft(profileResponse))
        setHistory(historyResponse)
        setCurrentSession(dashboardResponse.current_session)
        setLeaderboard(dashboardResponse.leaderboard)
        setLeaderboardPeriod(dashboardResponse.leaderboard.period)
        setLeaderboardCity(normalizeCityFilter(dashboardResponse.leaderboard.city))
        setLiveSessions(
          liveMapResponse.length > 0 ? liveMapResponse : dashboardResponse.active_walkers,
        )
        setAdSlots(adSlotsResponse.length > 0 ? adSlotsResponse : dashboardResponse.ad_slots)
      })
    },
  )

  const loadWorkspace = useEffectEvent(async () => {
    if (!apiClient.getSession()) {
      return
    }

    setWorkspaceLoading(true)

    try {
      const [dashboardResponse, profileResponse, historyResponse, liveMapResponse, adSlotsResponse] =
        await Promise.all([
          apiClient.getDashboard(),
          apiClient.getProfile(),
          apiClient.getHistory(),
          apiClient.getLiveMap(),
          apiClient.getAdSlots(),
        ])

      applyWorkspace(
        dashboardResponse,
        profileResponse,
        historyResponse,
        liveMapResponse,
        adSlotsResponse,
      )
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        apiClient.clearSession()
        setAuthSession(null)
        setDashboard(null)
        setProfile(null)
        setProfileDraft(null)
        setCurrentSession(null)
        return
      }

      setNotice(buildNotice('error', getErrorMessage(error, 'Не удалось загрузить рабочее место.')))
    } finally {
      setWorkspaceLoading(false)
    }
  })

  const syncLeaderboard = useEffectEvent(async (period: LeaderboardPeriod, city: string) => {
    setPendingAction('leaderboard')

    try {
      const response = await apiClient.getLeaderboard(period, 10, city)
      startTransition(() => {
        setLeaderboard(response)
        setDashboard((current) =>
          current
            ? {
                ...current,
                leaderboard: response,
              }
            : current,
        )
      })
    } catch (error) {
      setNotice(buildNotice('error', getErrorMessage(error, 'Не удалось обновить рейтинг.')))
    } finally {
      setPendingAction(null)
    }
  })

  const flushBufferedPoints = useEffectEvent(async (force = false) => {
    const session = currentSessionRef.current
    if (!session || session.session.status !== 'active' || pendingPointsRef.current.length === 0) {
      return
    }

    const pointsToSend = force
      ? pendingPointsRef.current.splice(0, pendingPointsRef.current.length)
      : pendingPointsRef.current.splice(0, Math.min(20, pendingPointsRef.current.length))

    setBufferedPoints(pendingPointsRef.current.length)
    setIsSyncingPoints(true)

    try {
      const response = await apiClient.addPoints(session.session.id, {
        points: pointsToSend,
      })

      setCurrentSession(response)
      setDashboard((current) =>
        current
          ? {
              ...current,
              current_session: response,
            }
          : current,
      )
      setLastSyncedAt(new Date().toISOString())
    } catch (error) {
      pendingPointsRef.current = [...pointsToSend, ...pendingPointsRef.current]
      setBufferedPoints(pendingPointsRef.current.length)
      setNotice(
        buildNotice('error', getErrorMessage(error, 'Не удалось синхронизировать маршрут.')),
      )
    } finally {
      setIsSyncingPoints(false)
    }
  })

  const enqueueTrackPoint = useEffectEvent((point: TrackPointRequest) => {
    pendingPointsRef.current.push(point)
    setBufferedPoints(pendingPointsRef.current.length)

    if (pendingPointsRef.current.length >= 6) {
      void flushBufferedPoints(true)
    }
  })

  const stopGpsTracking = useCallback(() => {
    if (geoWatchIdRef.current !== null) {
      navigator.geolocation.clearWatch(geoWatchIdRef.current)
      geoWatchIdRef.current = null
    }
  }, [])

  const stopDemoTracking = useCallback(() => {
    if (demoTimerRef.current !== null) {
      window.clearInterval(demoTimerRef.current)
      demoTimerRef.current = null
    }
  }, [])

  const stopFlushLoop = useCallback(() => {
    if (flushTimerRef.current !== null) {
      window.clearInterval(flushTimerRef.current)
      flushTimerRef.current = null
    }
  }, [])

  const detachLocalCapture = useCallback(() => {
    stopGpsTracking()
    stopDemoTracking()
    stopFlushLoop()
    setTrackingSource(null)
  }, [stopDemoTracking, stopFlushLoop, stopGpsTracking])

  function ensureFlushLoop() {
    if (flushTimerRef.current !== null) {
      return
    }

    flushTimerRef.current = window.setInterval(() => {
      void flushBufferedPoints(false)
    }, 5000)
  }

  function resetBufferedPoints() {
    pendingPointsRef.current = []
    setBufferedPoints(0)
  }

  function resolveDemoOrigin() {
    const point = currentSessionRef.current?.points.at(-1)
    if (point) {
      return {
        latitude: point.latitude,
        longitude: point.longitude,
      }
    }

    const livePoint = liveSessions[0]
    if (livePoint) {
      return {
        latitude: livePoint.latitude,
        longitude: livePoint.longitude,
      }
    }

    return DEFAULT_DEMO_CENTER
  }

  function beginGpsCapture() {
    preferredTrackingModeRef.current = 'gps'
    stopDemoTracking()

    if (!navigator.geolocation) {
      setTrackingPermission('unsupported')
      setTrackingSource(null)
      setNotice(
        buildNotice(
          'info',
          'В этом браузере нет geolocation API. Можно продолжить с demo mode и показать маршрут без телефона.',
        ),
      )
      return
    }

    stopGpsTracking()
    ensureFlushLoop()
    setTrackingSource('gps')

    geoWatchIdRef.current = navigator.geolocation.watchPosition(
      (position) => {
        setTrackingPermission('granted')
        enqueueTrackPoint({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracy_meters: position.coords.accuracy,
          altitude_meters: position.coords.altitude,
          recorded_at: new Date().toISOString(),
        })
      },
      () => {
        setTrackingPermission('denied')
        setTrackingSource(null)
        stopGpsTracking()
        stopFlushLoop()
        setNotice(
          buildNotice(
            'info',
            'GPS доступ не получен. Сессия остаётся активной, можно разрешить геолокацию или переключиться в demo mode.',
          ),
        )
      },
      {
        enableHighAccuracy: true,
        maximumAge: 1000,
        timeout: 15000,
      },
    )
  }

  function beginDemoCapture() {
    preferredTrackingModeRef.current = 'demo'
    stopGpsTracking()
    stopDemoTracking()
    ensureFlushLoop()
    setTrackingPermission('granted')
    setTrackingSource('demo')
    demoOriginRef.current = resolveDemoOrigin()
    demoTickRef.current = 0

    demoTimerRef.current = window.setInterval(() => {
      demoTickRef.current += 1
      const angle = (demoTickRef.current / 18) * Math.PI * 2
      const radiusLat = 0.00135
      const radiusLng = 0.0022

      enqueueTrackPoint({
        latitude: demoOriginRef.current.latitude + Math.cos(angle) * radiusLat,
        longitude: demoOriginRef.current.longitude + Math.sin(angle) * radiusLng,
        accuracy_meters: 6,
        altitude_meters: null,
        recorded_at: new Date().toISOString(),
      })
    }, 3500)
  }

  useEffect(() => {
    if (!authSession) {
      return
    }

    void loadWorkspace()
  }, [authSession, detachLocalCapture, loadWorkspace])

  useEffect(() => {
    if (!authSession) {
      return
    }

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/activity', {
        accessTokenFactory: () => apiClient.getAccessToken(),
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('sessionUpdated', (payload: WalkingSessionDetailResponse) => {
      if (currentSessionRef.current?.session.id === payload.session.id) {
        setCurrentSession(payload)
        setDashboard((current) =>
          current
            ? {
                ...current,
                current_session: payload,
              }
            : current,
        )
      }
    })

    connection.on('sessionCompleted', (payload: WalkingSessionDetailResponse) => {
      if (currentSessionRef.current?.session.id === payload.session.id) {
        setCurrentSession(null)
        resetBufferedPoints()
        detachLocalCapture()
      }

      setHistory((current) => upsertHistory(current, payload.session))
      void loadWorkspace()
    })

    connection.on('liveMapUpdated', (payload: LiveSessionResponse[]) => {
      setLiveSessions(payload)
    })

    connection.on('leaderboardUpdated', (payload: LeaderboardEntryResponse[]) => {
      setLeaderboard((current) =>
        current
          ? {
              ...current,
              entries: payload,
            }
          : current,
      )
    })

    void connection.start()

    return () => {
      void connection.stop()
    }
  }, [authSession, detachLocalCapture, loadWorkspace])

  useEffect(() => {
    return () => {
      detachLocalCapture()
    }
  }, [detachLocalCapture])

  async function handleAuthSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setPendingAction('auth')
    setAuthErrors({})

    try {
      const session =
        authMode === 'register'
          ? await apiClient.register(registerDraft)
          : await apiClient.login(loginDraft)

      setAuthSession(session)
      setNotice(
        buildNotice(
          'success',
          authMode === 'register'
            ? 'Аккаунт создан. Рабочее место готово для первой прогулки.'
            : 'С возвращением. Данные и live-каналы синхронизируются.',
        ),
      )
    } catch (error) {
      if (error instanceof ApiError) {
        setAuthErrors(error.fieldErrors ?? {})
      }

      setNotice(
        buildNotice(
          'error',
          getErrorMessage(error, 'Не удалось выполнить вход или регистрацию.'),
        ),
      )
    } finally {
      setPendingAction(null)
    }
  }

  async function handleLogout() {
    detachLocalCapture()
    resetBufferedPoints()
    setPendingAction('logout')

    try {
      await apiClient.logout()
    } finally {
      setAuthSession(null)
      setDashboard(null)
      setProfile(null)
      setProfileDraft(null)
      setHistory([])
      setCurrentSession(null)
      setLeaderboard(null)
      setLiveSessions([])
      setAdSlots([])
      setNotice(buildNotice('info', 'Сессия завершена. До новой прогулки.'))
      setPendingAction(null)
    }
  }

  async function handleStart(mode: 'gps' | 'demo') {
    setPendingAction('start')
    setSessionErrors({})

    try {
      const response = await apiClient.startSession({
        name: sessionName.trim(),
      })

      setCurrentSession(response)
      setDashboard((current) =>
        current
          ? {
              ...current,
              current_session: response,
            }
          : current,
      )
      setActivePanel('tracking')
      resetBufferedPoints()
      setLastSyncedAt(null)

      if (mode === 'gps') {
        beginGpsCapture()
      } else {
        beginDemoCapture()
      }

      setNotice(
        buildNotice(
          'success',
          mode === 'gps'
            ? 'Прогулка запущена. Маршрут и шаги начнут попадать в live-поток сразу после первой точки.'
            : 'Прогулка запущена в demo mode. Это удобно для презентаций и локального тестирования.',
        ),
      )
    } catch (error) {
      if (error instanceof ApiError) {
        setSessionErrors(error.fieldErrors ?? {})
      }

      setNotice(buildNotice('error', getErrorMessage(error, 'Не удалось запустить прогулку.')))
    } finally {
      setPendingAction(null)
    }
  }

  async function handlePause() {
    if (!currentSession) {
      return
    }

    setPendingAction('pause')

    try {
      await flushBufferedPoints(true)
      const response = await apiClient.pauseSession(currentSession.session.id)
      detachLocalCapture()
      setCurrentSession(response)
      setDashboard((current) =>
        current
          ? {
              ...current,
              current_session: response,
            }
          : current,
      )
      setNotice(buildNotice('info', 'Сессия поставлена на паузу.'))
    } catch (error) {
      setNotice(buildNotice('error', getErrorMessage(error, 'Не удалось поставить на паузу.')))
    } finally {
      setPendingAction(null)
    }
  }

  async function handleResume(mode: 'gps' | 'demo') {
    if (!currentSession) {
      return
    }

    setPendingAction('resume')

    try {
      const response = await apiClient.resumeSession(currentSession.session.id)
      setCurrentSession(response)
      setDashboard((current) =>
        current
          ? {
              ...current,
              current_session: response,
            }
          : current,
      )

      if (mode === 'gps') {
        beginGpsCapture()
      } else {
        beginDemoCapture()
      }

      setNotice(buildNotice('success', 'Сессия возобновлена.'))
    } catch (error) {
      setNotice(buildNotice('error', getErrorMessage(error, 'Не удалось возобновить прогулку.')))
    } finally {
      setPendingAction(null)
    }
  }

  async function handleStop() {
    if (!currentSession) {
      return
    }

    setPendingAction('stop')

    try {
      if (currentSession.session.status === 'active') {
        await flushBufferedPoints(true)
      }

      const response = await apiClient.stopSession(currentSession.session.id)
      detachLocalCapture()
      resetBufferedPoints()
      setCurrentSession(null)
      setHistory((current) => upsertHistory(current, response.session))
      setNotice(buildNotice('success', 'Прогулка завершена и сохранена в истории.'))
      await loadWorkspace()
    } catch (error) {
      setNotice(buildNotice('error', getErrorMessage(error, 'Не удалось завершить прогулку.')))
    } finally {
      setPendingAction(null)
    }
  }

  async function handleProfileSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!profileDraft) {
      return
    }

    setPendingAction('profile')
    setProfileErrors({})

    try {
      const response = await apiClient.updateProfile(profileDraft)
      setProfile(response)
      setDashboard((current) =>
        current
          ? {
              ...current,
              user: response,
            }
          : current,
      )
      setProfileDraft(profileToDraft(response))
      setNotice(buildNotice('success', 'Профиль сохранён.'))
      await loadWorkspace()
    } catch (error) {
      if (error instanceof ApiError) {
        setProfileErrors(error.fieldErrors ?? {})
      }

      setNotice(buildNotice('error', getErrorMessage(error, 'Не удалось сохранить профиль.')))
    } finally {
      setPendingAction(null)
    }
  }

  async function handleLeaderboardSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    await syncLeaderboard(leaderboardPeriod, deferredLeaderboardCity.trim() || 'all')
  }

  const userAccent = dashboard?.user.accent_color ?? profile?.accent_color ?? '#1f7a5c'
  const currentSteps = currentSession?.session.total_steps ?? dashboard?.summary.today_steps ?? 0
  const currentDistance =
    currentSession?.session.total_distance_meters ?? dashboard?.summary.today_distance_meters ?? 0
  const currentCalories =
    currentSession?.session.calories_burned ?? dashboard?.summary.today_calories_burned ?? 0
  const goal = profile?.daily_step_goal ?? dashboard?.user.daily_step_goal ?? 10000
  const progress = goal > 0 ? Math.min(100, Math.round((currentSteps / goal) * 100)) : 0
  const recentSessions = history.length > 0 ? history : dashboard?.recent_sessions ?? []
  const weeklyTrend = dashboard?.weekly_trend ?? []
  const trendMax = Math.max(1, ...weeklyTrend.map((item) => item.steps))
  const visibleLeaderboard = leaderboard ?? dashboard?.leaderboard ?? null
  const visibleLiveSessions = liveSessions.length > 0 ? liveSessions : dashboard?.active_walkers ?? []
  const visibleAds = adSlots.length > 0 ? adSlots : dashboard?.ad_slots ?? []
  const currentStatusText = currentSession
    ? currentSession.session.status === 'active'
      ? trackingSource === 'gps'
        ? 'GPS capture live'
        : trackingSource === 'demo'
          ? 'Demo route live'
          : 'Сессия активна, но локальный трекер не подключён'
      : 'Пауза'
    : 'Нет активной прогулки'
  const wrapperStyle = {
    '--user-accent': userAccent,
  } as CSSProperties

  if (!authSession) {
    return (
      <div className="auth-shell">
        <div className="auth-shell__backdrop" />
        <section className="auth-shell__hero">
          <div className="surface hero-copy">
            <span className="eyebrow">StrideFlow</span>
            <h1>Платформа шагомера, которую не стыдно показать как эталон.</h1>
            <p>
              JWT auth, Redis-backed realtime, SignalR live feed, карта маршрута,
              городские рейтинги и готовые рекламные слоты уже собраны в одном монолите.
            </p>

            <div className="hero-copy__chips">
              <span className="chip chip--accent">ASP.NET 8 + React 19</span>
              <span className="chip chip--muted">PostgreSQL + Redis</span>
              <span className="chip chip--muted">Telegram Stars leaderboard</span>
            </div>

            <div className="feature-grid">
              <article className="surface feature-card">
                <RadioTower />
                <strong>Live tracking</strong>
                <p>SignalR мгновенно обновляет карту, активные сессии и рейтинг.</p>
              </article>
              <article className="surface feature-card">
                <ShieldCheck />
                <strong>Чистая auth-модель</strong>
                <p>Access token, refresh rotation, logout и `problem+json` ошибки.</p>
              </article>
              <article className="surface feature-card">
                <Trophy />
                <strong>Рост и монетизация</strong>
                <p>Рейтинг по городам, preview Telegram Stars и ad inventory под продажу.</p>
              </article>
            </div>
          </div>
        </section>

        <section className="auth-shell__panel">
          <div className="surface auth-card">
            <div className="auth-card__switch">
              <button
                type="button"
                className={authMode === 'register' ? 'segment segment--active' : 'segment'}
                onClick={() => {
                  setAuthMode('register')
                  setAuthErrors({})
                }}
              >
                Регистрация
              </button>
              <button
                type="button"
                className={authMode === 'login' ? 'segment segment--active' : 'segment'}
                onClick={() => {
                  setAuthMode('login')
                  setAuthErrors({})
                }}
              >
                Вход
              </button>
            </div>

            <form className="form-stack" onSubmit={handleAuthSubmit}>
              {authMode === 'register' ? (
                <>
                  <div className="form-grid">
                    <label className="field">
                      <span>Email</span>
                      <input
                        type="email"
                        value={registerDraft.email}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            email: event.target.value,
                          }))
                        }
                        placeholder="walker@strideflow.app"
                      />
                      {getFieldError(authErrors, 'email') ? (
                        <small>{getFieldError(authErrors, 'email')}</small>
                      ) : null}
                    </label>

                    <label className="field">
                      <span>Username</span>
                      <input
                        value={registerDraft.username}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            username: event.target.value,
                          }))
                        }
                        placeholder="dalkory"
                      />
                      {getFieldError(authErrors, 'username') ? (
                        <small>{getFieldError(authErrors, 'username')}</small>
                      ) : null}
                    </label>

                    <label className="field">
                      <span>Отображаемое имя</span>
                      <input
                        value={registerDraft.display_name}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            display_name: event.target.value,
                          }))
                        }
                        placeholder="Daniil"
                      />
                      {getFieldError(authErrors, 'display_name') ? (
                        <small>{getFieldError(authErrors, 'display_name')}</small>
                      ) : null}
                    </label>

                    <label className="field">
                      <span>Пароль</span>
                      <input
                        type="password"
                        value={registerDraft.password}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            password: event.target.value,
                          }))
                        }
                        placeholder="StrongPass123"
                      />
                      {getFieldError(authErrors, 'password') ? (
                        <small>{getFieldError(authErrors, 'password')}</small>
                      ) : null}
                    </label>

                    <label className="field">
                      <span>Рост, см</span>
                      <input
                        type="number"
                        min={120}
                        max={230}
                        value={registerDraft.height_cm}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            height_cm: Number(event.target.value),
                          }))
                        }
                      />
                    </label>

                    <label className="field">
                      <span>Вес, кг</span>
                      <input
                        type="number"
                        min={35}
                        max={300}
                        value={registerDraft.weight_kg}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            weight_kg: Number(event.target.value),
                          }))
                        }
                      />
                    </label>

                    <label className="field">
                      <span>Цель шагов</span>
                      <input
                        type="number"
                        min={1000}
                        max={50000}
                        value={registerDraft.daily_step_goal}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            daily_step_goal: Number(event.target.value),
                          }))
                        }
                      />
                    </label>

                    <label className="field">
                      <span>Город</span>
                      <input
                        value={registerDraft.city}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            city: event.target.value,
                          }))
                        }
                        placeholder="Moscow"
                      />
                    </label>

                    <label className="field field--full">
                      <span>Time zone</span>
                      <input
                        value={registerDraft.time_zone_id}
                        onChange={(event) =>
                          setRegisterDraft((current) => ({
                            ...current,
                            time_zone_id: event.target.value,
                          }))
                        }
                        placeholder="Europe/Moscow"
                      />
                    </label>
                  </div>

                  <button className="button button--primary" disabled={pendingAction === 'auth'}>
                    {pendingAction === 'auth' ? 'Создаём пространство...' : 'Создать аккаунт'}
                  </button>
                </>
              ) : (
                <>
                  <label className="field">
                    <span>Email</span>
                    <input
                      type="email"
                      value={loginDraft.email}
                      onChange={(event) =>
                        setLoginDraft((current) => ({
                          ...current,
                          email: event.target.value,
                        }))
                      }
                      placeholder="walker@strideflow.app"
                    />
                    {getFieldError(authErrors, 'email') ? (
                      <small>{getFieldError(authErrors, 'email')}</small>
                    ) : null}
                  </label>

                  <label className="field">
                    <span>Пароль</span>
                    <input
                      type="password"
                      value={loginDraft.password}
                      onChange={(event) =>
                        setLoginDraft((current) => ({
                          ...current,
                          password: event.target.value,
                        }))
                      }
                      placeholder="StrongPass123"
                    />
                    {getFieldError(authErrors, 'password') ? (
                      <small>{getFieldError(authErrors, 'password')}</small>
                    ) : null}
                  </label>

                  <label className="field">
                    <span>Имя устройства</span>
                    <input
                      value={loginDraft.device_name ?? ''}
                      onChange={(event) =>
                        setLoginDraft((current) => ({
                          ...current,
                          device_name: event.target.value,
                        }))
                      }
                      placeholder="Web dashboard"
                    />
                  </label>

                  <button className="button button--primary" disabled={pendingAction === 'auth'}>
                    {pendingAction === 'auth' ? 'Проверяем токены...' : 'Войти'}
                  </button>
                </>
              )}
            </form>

            <p className="auth-card__note">
              После входа ты получишь полноценную рабочую панель с картой, сессиями, профилем,
              рейтингом и live-обновлениями.
            </p>
          </div>
        </section>

        {notice ? (
          <div className={`notice notice--${notice.tone} notice--floating`}>{notice.text}</div>
        ) : null}
      </div>
    )
  }

  if (!dashboard || !profile || !profileDraft) {
    return (
      <div className="loading-shell" style={wrapperStyle}>
        <div className="surface loading-card">
          <Sparkles />
          <strong>Собираем рабочее место...</strong>
          <p>Подтягиваем профиль, историю, live map и рекламные слоты.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="app-shell" style={wrapperStyle}>
      <div className="app-shell__bg" />

      <header className="surface topbar">
        <div className="topbar__brand">
          <div className="brand-badge">
            <Footprints />
          </div>
          <div>
            <span className="eyebrow">StrideFlow</span>
            <strong>Realtime pedometer workspace</strong>
          </div>
        </div>

        <div className="topbar__meta">
          <span className="chip chip--muted">{profile.city}</span>
          <span className="chip chip--success">{currentStatusText}</span>
          <button
            type="button"
            className="button button--ghost"
            onClick={handleLogout}
            disabled={pendingAction === 'logout'}
          >
            <LogOut size={16} />
            Выйти
          </button>
        </div>
      </header>

      {notice ? <div className={`notice notice--${notice.tone}`}>{notice.text}</div> : null}
      {workspaceLoading ? <div className="notice notice--info">Синхронизация данных...</div> : null}

      <main className="app-main">
        <section className="hero-grid">
          <article className="surface hero-card">
            <div className="hero-card__copy">
              <span className="eyebrow">Control center</span>
              <h1>{profile.display_name}, сегодня уже видно хороший темп.</h1>
              <p>
                Цель дня {formatNumber(goal)} шагов. Рейтинг по городу, ad slots и realtime карта
                уже подключены и готовы к продовому сценарию.
              </p>

              <div className="hero-card__meta">
                <span className="chip chip--accent">JWT secured</span>
                <span className="chip chip--muted">SignalR live</span>
                <span className="chip chip--muted">Redis synchronized</span>
              </div>

              <div className="hero-card__kpis">
                <div>
                  <strong>{formatNumber(currentSteps)}</strong>
                  <span>шагов сегодня</span>
                </div>
                <div>
                  <strong>{formatDistance(currentDistance)}</strong>
                  <span>дистанция</span>
                </div>
                <div>
                  <strong>{formatCalories(currentCalories)}</strong>
                  <span>сожжено</span>
                </div>
              </div>
            </div>

            <div className="progress-ring">
              <div
                className="progress-ring__dial"
                style={
                  {
                    '--progress-angle': `${Math.max(progress, 3) * 3.6}deg`,
                  } as CSSProperties
                }
              >
                <div className="progress-ring__center">
                  <span>goal</span>
                  <strong>{formatPercent(progress)}</strong>
                  <small>{formatNumber(goal)} steps</small>
                </div>
              </div>
            </div>
          </article>

          <div className="stats-grid">
            <StatCard
              label="Стрик"
              value={`${dashboard.summary.current_streak_days} дн.`}
              hint="Сколько дней подряд были шаги"
              icon={<Zap size={20} />}
              tone="accent"
            />
            <StatCard
              label="Активность"
              value={`${dashboard.summary.active_minutes_today} мин`}
              hint="Активные минуты сегодня"
              icon={<Clock3 size={20} />}
            />
            <StatCard
              label="За неделю"
              value={formatCompact(dashboard.summary.weekly_steps)}
              hint={formatDistance(dashboard.summary.weekly_distance_meters)}
              icon={<Activity size={20} />}
              tone="warm"
            />
            <StatCard
              label="Онлайн walkers"
              value={String(visibleLiveSessions.length)}
              hint="Сейчас на карте"
              icon={<Users size={20} />}
            />
          </div>
        </section>

        <nav className="surface panel-switcher">
          {[
            ['overview', 'Обзор'],
            ['tracking', 'Трекинг'],
            ['community', 'Рейтинг'],
            ['profile', 'Профиль'],
          ].map(([key, label]) => (
            <button
              key={key}
              type="button"
              className={activePanel === key ? 'segment segment--active' : 'segment'}
              onClick={() => setActivePanel(key as PanelKey)}
            >
              {label}
            </button>
          ))}
        </nav>

        {activePanel === 'overview' ? (
          <section className="content-grid">
            <article className="surface section-card section-card--wide">
              <div className="section-heading">
                <span className="eyebrow">Weekly trend</span>
                <strong>Как выглядит неделя по шагам</strong>
              </div>

              <div className="trend-chart">
                {weeklyTrend.map((item) => (
                  <div key={item.date} className="trend-chart__item">
                    <div
                      className="trend-chart__bar"
                      style={
                        {
                          '--bar-height': `${Math.max((item.steps / trendMax) * 100, 6)}%`,
                        } as CSSProperties
                      }
                    />
                    <strong>{formatCompact(item.steps)}</strong>
                    <span>{formatDateLabel(item.date)}</span>
                  </div>
                ))}
              </div>
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">Live route</span>
                <strong>Карта активных маршрутов</strong>
              </div>
              <MapPanel
                currentSession={currentSession}
                liveSessions={visibleLiveSessions}
                userAccent={userAccent}
              />
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">Recent sessions</span>
                <strong>Последние прогулки</strong>
              </div>

              <div className="session-list">
                {recentSessions.slice(0, 5).map((session) => (
                  <div key={session.id} className="session-item">
                    <div>
                      <strong>{session.name}</strong>
                      <span>{formatDateTime(session.started_at)}</span>
                    </div>
                    <div className="session-item__stats">
                      <span>{formatNumber(session.total_steps)} шагов</span>
                      <span>{formatDistance(session.total_distance_meters)}</span>
                    </div>
                  </div>
                ))}
              </div>
            </article>

            {visibleAds.slice(0, 2).map((slot) => (
              <AdSlot key={slot.key} slot={slot} />
            ))}
          </section>
        ) : null}

        {activePanel === 'tracking' ? (
          <section className="content-grid">
            <article className="surface section-card section-card--wide">
              <div className="section-heading">
                <span className="eyebrow">Tracking control</span>
                <strong>Старт, пауза, резюм и синхронизация</strong>
              </div>

              {!currentSession ? (
                <div className="control-stack">
                  <label className="field">
                    <span>Название прогулки</span>
                    <input
                      value={sessionName}
                      onChange={(event) => setSessionName(event.target.value)}
                      placeholder="Вечерняя прогулка"
                    />
                    {getFieldError(sessionErrors, 'name') ? (
                      <small>{getFieldError(sessionErrors, 'name')}</small>
                    ) : null}
                  </label>

                  <div className="button-row">
                    <button
                      type="button"
                      className="button button--primary"
                      onClick={() => void handleStart('gps')}
                      disabled={pendingAction === 'start'}
                    >
                      <Play size={16} />
                      Старт с GPS
                    </button>
                    <button
                      type="button"
                      className="button button--secondary"
                      onClick={() => void handleStart('demo')}
                      disabled={pendingAction === 'start'}
                    >
                      <Sparkles size={16} />
                      Demo route
                    </button>
                  </div>
                </div>
              ) : (
                <div className="control-stack">
                  <div className="status-grid">
                    <div className="status-card">
                      <span>Сессия</span>
                      <strong>{currentSession.session.name}</strong>
                      <small>{currentStatusText}</small>
                    </div>
                    <div className="status-card">
                      <span>Буфер точек</span>
                      <strong>{bufferedPoints}</strong>
                      <small>{isSyncingPoints ? 'идёт sync' : 'готово к отправке'}</small>
                    </div>
                    <div className="status-card">
                      <span>Последний sync</span>
                      <strong>{formatRelativeTime(lastSyncedAt)}</strong>
                      <small>{trackingPermission === 'granted' ? 'permission ok' : trackingPermission}</small>
                    </div>
                  </div>

                  <div className="button-row">
                    {isActiveSession(currentSession) ? (
                      <>
                        <button
                          type="button"
                          className="button button--secondary"
                          onClick={handlePause}
                          disabled={pendingAction === 'pause'}
                        >
                          <Pause size={16} />
                          Пауза
                        </button>
                        <button
                          type="button"
                          className="button button--ghost"
                          onClick={() => beginGpsCapture()}
                        >
                          <RadioTower size={16} />
                          Подключить GPS
                        </button>
                        <button
                          type="button"
                          className="button button--ghost"
                          onClick={() => beginDemoCapture()}
                        >
                          <Sparkles size={16} />
                          Demo mode
                        </button>
                      </>
                    ) : (
                      <>
                        <button
                          type="button"
                          className="button button--primary"
                          onClick={() => void handleResume('gps')}
                          disabled={pendingAction === 'resume'}
                        >
                          <Play size={16} />
                          Resume GPS
                        </button>
                        <button
                          type="button"
                          className="button button--secondary"
                          onClick={() => void handleResume('demo')}
                          disabled={pendingAction === 'resume'}
                        >
                          <Sparkles size={16} />
                          Resume demo
                        </button>
                      </>
                    )}

                    <button
                      type="button"
                      className="button button--danger"
                      onClick={handleStop}
                      disabled={pendingAction === 'stop'}
                    >
                      <Square size={16} />
                      Завершить
                    </button>
                  </div>
                </div>
              )}
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">Current walk</span>
                <strong>Живая карта текущей сессии</strong>
              </div>
              <MapPanel
                currentSession={currentSession}
                liveSessions={visibleLiveSessions}
                userAccent={userAccent}
              />
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">Session metrics</span>
                <strong>Текущая телеметрия</strong>
              </div>

              <div className="mini-metrics">
                <div>
                  <Footprints size={18} />
                  <div>
                    <strong>{formatNumber(currentSession?.session.total_steps ?? 0)}</strong>
                    <span>шагов</span>
                  </div>
                </div>
                <div>
                  <Route size={18} />
                  <div>
                    <strong>{formatDistance(currentSession?.session.total_distance_meters ?? 0)}</strong>
                    <span>дистанция</span>
                  </div>
                </div>
                <div>
                  <Flame size={18} />
                  <div>
                    <strong>{formatCalories(currentSession?.session.calories_burned ?? 0)}</strong>
                    <span>калории</span>
                  </div>
                </div>
                <div>
                  <Clock3 size={18} />
                  <div>
                    <strong>{formatDuration(currentSession?.session.duration_seconds ?? 0)}</strong>
                    <span>длительность</span>
                  </div>
                </div>
                <div>
                  <Activity size={18} />
                  <div>
                    <strong>
                      {formatPace(currentSession?.average_pace_seconds_per_kilometer ?? 0)}
                    </strong>
                    <span>средний темп</span>
                  </div>
                </div>
                <div>
                  <Waves size={18} />
                  <div>
                    <strong>{formatSpeed(currentSession?.average_speed_meters_per_second ?? 0)}</strong>
                    <span>скорость</span>
                  </div>
                </div>
              </div>
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">History</span>
                <strong>Полная история пользователя</strong>
              </div>
              <div className="session-list">
                {recentSessions.map((session) => (
                  <div key={session.id} className="session-item">
                    <div>
                      <strong>{session.name}</strong>
                      <span>{formatDateTime(session.started_at)}</span>
                    </div>
                    <div className="session-item__stats">
                      <span>{formatNumber(session.total_steps)} шагов</span>
                      <span>{formatDistance(session.total_distance_meters)}</span>
                      <span>{formatDuration(session.duration_seconds)}</span>
                    </div>
                  </div>
                ))}
              </div>
            </article>
          </section>
        ) : null}

        {activePanel === 'community' ? (
          <section className="content-grid">
            <article className="surface section-card section-card--wide">
              <div className="section-heading">
                <span className="eyebrow">City leaderboard</span>
                <strong>Рейтинг за {visibleLeaderboard?.period ?? 'week'} и preview наград</strong>
              </div>

              <form className="leaderboard-toolbar" onSubmit={handleLeaderboardSubmit}>
                <div className="pill-row">
                  {(['day', 'week', 'month'] as LeaderboardPeriod[]).map((period) => (
                    <button
                      key={period}
                      type="button"
                      className={leaderboardPeriod === period ? 'segment segment--active' : 'segment'}
                      onClick={() => setLeaderboardPeriod(period)}
                    >
                      {period}
                    </button>
                  ))}
                </div>

                <label className="field field--inline">
                  <span>Город</span>
                  <input
                    value={leaderboardCity}
                    onChange={(event) => setLeaderboardCity(event.target.value)}
                    placeholder="all"
                  />
                </label>

                <button
                  className="button button--secondary"
                  disabled={pendingAction === 'leaderboard'}
                >
                  Обновить рейтинг
                </button>
              </form>

              <div className="reward-row">
                {visibleLeaderboard?.reward_preview.map((tier) => (
                  <div key={tier.rank} className="reward-chip">
                    <strong>#{tier.rank}</strong>
                    <span>{tier.telegram_stars} stars</span>
                  </div>
                ))}
              </div>

              <div className="leaderboard-list">
                {visibleLeaderboard?.entries.map((entry) => (
                  <div
                    key={entry.user_id}
                    className={entry.is_current_user ? 'leaderboard-item leaderboard-item--me' : 'leaderboard-item'}
                  >
                    <div className="leaderboard-item__rank">{entry.rank}</div>
                    <div className="leaderboard-item__name">
                      <span
                        className="leaderboard-item__avatar"
                        style={{ backgroundColor: entry.accent_color }}
                      />
                      <div>
                        <strong>{entry.display_name}</strong>
                        <span>{entry.is_current_user ? 'ты' : visibleLeaderboard?.city}</span>
                      </div>
                    </div>
                    <div className="leaderboard-item__stats">
                      <strong>{formatNumber(entry.steps)}</strong>
                      <span>{formatDistance(entry.distance_meters)}</span>
                    </div>
                  </div>
                ))}
              </div>
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">Live walkers</span>
                <strong>Кто сейчас в эфире</strong>
              </div>

              <div className="session-list">
                {visibleLiveSessions.map((walker) => (
                  <div key={walker.session_id} className="session-item">
                    <div>
                      <strong>{walker.display_name}</strong>
                      <span>{walker.session_name}</span>
                    </div>
                    <div className="session-item__stats">
                      <span>{formatNumber(walker.total_steps)} шагов</span>
                      <span>{formatRelativeTime(walker.updated_at)}</span>
                    </div>
                  </div>
                ))}
              </div>
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">Reward notes</span>
                <strong>Как это выглядит для тестового запуска</strong>
              </div>
              <p className="section-card__copy">
                На weekly board сейчас preview наград строится в Telegram Stars. По текущей конфигурации
                топ-1 получает 3 stars, затем 2 и 1. Для monthly board награда расширяется до топ-5.
              </p>
              <p className="section-card__copy">
                Фильтр по городу уже готов: введи `all`, чтобы увидеть глобальную ленту, либо конкретный
                город, если нужен локальный рейтинг.
              </p>
            </article>

            {visibleAds.map((slot) => (
              <AdSlot key={slot.key} slot={slot} />
            ))}
          </section>
        ) : null}

        {activePanel === 'profile' ? (
          <section className="content-grid">
            <article className="surface section-card section-card--wide">
              <div className="section-heading">
                <span className="eyebrow">Profile settings</span>
                <strong>Настройка профиля и точности расчёта шагов</strong>
              </div>

              <form className="form-stack" onSubmit={handleProfileSave}>
                <div className="form-grid">
                  <label className="field">
                    <span>Отображаемое имя</span>
                    <input
                      value={profileDraft.display_name}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                display_name: event.target.value,
                              }
                            : current,
                        )
                      }
                    />
                    {getFieldError(profileErrors, 'display_name') ? (
                      <small>{getFieldError(profileErrors, 'display_name')}</small>
                    ) : null}
                  </label>

                  <label className="field">
                    <span>Город</span>
                    <input
                      value={profileDraft.city}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                city: event.target.value,
                              }
                            : current,
                        )
                      }
                    />
                    {getFieldError(profileErrors, 'city') ? (
                      <small>{getFieldError(profileErrors, 'city')}</small>
                    ) : null}
                  </label>

                  <label className="field field--full">
                    <span>Bio</span>
                    <textarea
                      rows={4}
                      value={profileDraft.bio}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                bio: event.target.value,
                              }
                            : current,
                        )
                      }
                      placeholder="Коротко о себе, стиле прогулок и целях."
                    />
                  </label>

                  <label className="field">
                    <span>Time zone</span>
                    <input
                      value={profileDraft.time_zone_id}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                time_zone_id: event.target.value,
                              }
                            : current,
                        )
                      }
                    />
                  </label>

                  <label className="field">
                    <span>Акцентный цвет</span>
                    <input
                      value={profileDraft.accent_color}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                accent_color: event.target.value,
                              }
                            : current,
                        )
                      }
                    />
                  </label>

                  <label className="field">
                    <span>Рост, см</span>
                    <input
                      type="number"
                      min={120}
                      max={230}
                      value={profileDraft.height_cm}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                height_cm: Number(event.target.value),
                              }
                            : current,
                        )
                      }
                    />
                  </label>

                  <label className="field">
                    <span>Вес, кг</span>
                    <input
                      type="number"
                      min={35}
                      max={300}
                      value={profileDraft.weight_kg}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                weight_kg: Number(event.target.value),
                              }
                            : current,
                        )
                      }
                    />
                  </label>

                  <label className="field">
                    <span>Длина шага, м</span>
                    <input
                      type="number"
                      step="0.01"
                      min={0.3}
                      max={1.5}
                      value={profileDraft.step_length_meters ?? ''}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                step_length_meters:
                                  event.target.value.length > 0
                                    ? Number(event.target.value)
                                    : null,
                              }
                            : current,
                        )
                      }
                    />
                  </label>

                  <label className="field">
                    <span>Цель шагов</span>
                    <input
                      type="number"
                      min={1000}
                      max={50000}
                      value={profileDraft.daily_step_goal}
                      onChange={(event) =>
                        setProfileDraft((current) =>
                          current
                            ? {
                                ...current,
                                daily_step_goal: Number(event.target.value),
                              }
                            : current,
                        )
                      }
                    />
                  </label>
                </div>

                <label className="checkbox-row">
                  <input
                    type="checkbox"
                    checked={profileDraft.is_profile_public}
                    onChange={(event) =>
                      setProfileDraft((current) =>
                        current
                          ? {
                              ...current,
                              is_profile_public: event.target.checked,
                            }
                          : current,
                      )
                    }
                  />
                  <span>Показывать профиль и результаты в публичных таблицах лидеров</span>
                </label>

                <button className="button button--primary" disabled={pendingAction === 'profile'}>
                  Сохранить профиль
                </button>
              </form>
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">Account facts</span>
                <strong>О системе пользователя</strong>
              </div>

              <div className="fact-list">
                <div>
                  <span>Email</span>
                  <strong>{profile.email}</strong>
                </div>
                <div>
                  <span>Username</span>
                  <strong>{profile.username}</strong>
                </div>
                <div>
                  <span>Создан</span>
                  <strong>{formatDateTime(profile.created_at)}</strong>
                </div>
                <div>
                  <span>Последний визит</span>
                  <strong>{formatDateTime(profile.last_seen_at)}</strong>
                </div>
              </div>
            </article>

            <article className="surface section-card">
              <div className="section-heading">
                <span className="eyebrow">Architecture</span>
                <strong>Что уже готово под growth</strong>
              </div>
              <ul className="plain-list">
                <li>Refresh-token rotation и корректный logout для web-клиента.</li>
                <li>Рекламные placements уже выделены и отдаются отдельным endpoint.</li>
                <li>SignalR hub готов к масштабированию через Redis backplane.</li>
                <li>Leaderboard умеет периоды `day`, `week`, `month` и фильтрацию по городу.</li>
              </ul>
            </article>
          </section>
        ) : null}
      </main>
    </div>
  )
}

export default App
