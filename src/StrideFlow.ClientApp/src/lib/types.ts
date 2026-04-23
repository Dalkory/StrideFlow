export type LeaderboardPeriod = 'day' | 'week' | 'month'

export interface AuthTokensResponse {
  access_token: string
  refresh_token: string
  expires_at: string
}

export interface UserProfileResponse {
  id: string
  email: string
  username: string
  display_name: string
  bio: string
  city: string
  time_zone_id: string
  accent_color: string
  height_cm: number
  weight_kg: number
  step_length_meters: number
  daily_step_goal: number
  is_profile_public: boolean
  created_at: string
  last_seen_at: string
}

export interface AuthResponse {
  tokens: AuthTokensResponse
  user: UserProfileResponse
}

export interface RegisterRequest {
  email: string
  username: string
  display_name: string
  password: string
  height_cm: number
  weight_kg: number
  daily_step_goal: number
  city: string
  time_zone_id: string
}

export interface LoginRequest {
  email: string
  password: string
  device_name?: string
}

export interface UpdateProfileRequest {
  display_name: string
  bio: string
  city: string
  time_zone_id: string
  accent_color: string
  height_cm: number
  weight_kg: number
  step_length_meters: number | null
  daily_step_goal: number
  is_profile_public: boolean
}

export interface RewardTierResponse {
  rank: number
  telegram_stars: number
}

export interface RewardStandingResponse {
  period: LeaderboardPeriod
  city: string
  starts_at: string
  ends_at: string
  rank: number
  steps: number
  distance_meters: number
  telegram_stars: number | null
  is_eligible: boolean
  status: string
}

export interface RewardSummaryResponse {
  payout_provider: string
  is_test_mode: boolean
  settlement_policy: string
  weekly: RewardStandingResponse
  monthly: RewardStandingResponse
  weekly_tiers: RewardTierResponse[]
  monthly_tiers: RewardTierResponse[]
}

export interface AchievementResponse {
  key: string
  title: string
  description: string
  tone: string
  is_unlocked: boolean
  progress_percent: number
  current_value: number
  target_value: number
}

export interface ActivityCoachResponse {
  daily_goal: number
  remaining_steps_today: number
  suggested_steps_per_hour: number
  weekly_average_steps: number
  consistency_score: number
  message: string
  achievements: AchievementResponse[]
}

export interface ActivityInsightsResponse {
  generated_at: string
  coach: ActivityCoachResponse
  rewards: RewardSummaryResponse
}

export interface LeaderboardEntryResponse {
  user_id: string
  display_name: string
  accent_color: string
  steps: number
  distance_meters: number
  rank: number
  is_current_user: boolean
}

export interface LeaderboardResponse {
  period: LeaderboardPeriod
  city: string
  reward_currency: string
  reward_preview: RewardTierResponse[]
  entries: LeaderboardEntryResponse[]
}

export interface DailyTrendResponse {
  date: string
  steps: number
  distance_meters: number
  calories_burned: number
}

export interface DashboardSummaryResponse {
  today_steps: number
  today_distance_meters: number
  today_calories_burned: number
  active_minutes_today: number
  current_streak_days: number
  goal_progress_percent: number
  weekly_steps: number
  weekly_distance_meters: number
}

export interface WalkingSessionResponse {
  id: string
  name: string
  status: string
  started_at: string
  last_point_recorded_at: string | null
  completed_at: string | null
  total_distance_meters: number
  total_steps: number
  calories_burned: number
  duration_seconds: number
}

export interface WalkingSessionPointResponse {
  latitude: number
  longitude: number
  accuracy_meters: number
  distance_from_previous_meters: number
  step_delta: number
  speed_meters_per_second: number
  recorded_at: string
}

export interface WalkingSessionDetailResponse {
  session: WalkingSessionResponse
  points: WalkingSessionPointResponse[]
  average_pace_seconds_per_kilometer: number
  average_speed_meters_per_second: number
}

export interface LiveSessionResponse {
  session_id: string
  user_id: string
  display_name: string
  accent_color: string
  session_name: string
  status: string
  latitude: number
  longitude: number
  total_distance_meters: number
  total_steps: number
  calories_burned: number
  updated_at: string
  tail_points: WalkingSessionPointResponse[]
}

export interface AdSlotResponse {
  key: string
  placement: string
  title: string
  description: string
  enabled: boolean
  is_placeholder: boolean
}

export interface DashboardResponse {
  user: UserProfileResponse
  summary: DashboardSummaryResponse
  current_session: WalkingSessionDetailResponse | null
  recent_sessions: WalkingSessionResponse[]
  weekly_trend: DailyTrendResponse[]
  leaderboard: LeaderboardResponse
  ad_slots: AdSlotResponse[]
  active_walkers: LiveSessionResponse[]
}

export interface TrackPointRequest {
  latitude: number
  longitude: number
  accuracy_meters: number
  altitude_meters: number | null
  recorded_at: string
}

export interface TrackSessionPointsRequest {
  points: TrackPointRequest[]
}

export interface StartSessionRequest {
  name: string
}

export interface ProblemDetailsPayload {
  title?: string
  detail?: string
  status?: number
  error_code?: string
  errors?: Record<string, string[]>
}

export type AuthSession = AuthResponse
