export function formatNumber(value: number): string {
  return new Intl.NumberFormat('ru-RU').format(Math.round(value))
}

export function formatCompact(value: number): string {
  return new Intl.NumberFormat('ru-RU', {
    notation: 'compact',
    maximumFractionDigits: 1,
  }).format(value)
}

export function formatDistance(meters: number): string {
  if (meters >= 1000) {
    return `${(meters / 1000).toFixed(meters >= 10000 ? 0 : 1)} км`
  }

  return `${Math.round(meters)} м`
}

export function formatCalories(value: number): string {
  return `${Math.round(value)} ккал`
}

export function formatDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  const remainingSeconds = seconds % 60

  if (hours > 0) {
    return `${hours}ч ${minutes.toString().padStart(2, '0')}м`
  }

  if (minutes > 0) {
    return `${minutes}м ${remainingSeconds.toString().padStart(2, '0')}с`
  }

  return `${remainingSeconds}с`
}

export function formatPercent(value: number): string {
  return `${Math.round(value)}%`
}

export function formatDateLabel(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit',
    month: 'short',
  }).format(new Date(value))
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return '—'
  }

  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export function formatRelativeTime(value: string | null | undefined): string {
  if (!value) {
    return 'ещё не синхронизировано'
  }

  const diffSeconds = Math.round((new Date(value).getTime() - Date.now()) / 1000)
  const formatter = new Intl.RelativeTimeFormat('ru-RU', { numeric: 'auto' })

  const minutes = Math.round(diffSeconds / 60)
  if (Math.abs(minutes) < 60) {
    return formatter.format(minutes, 'minute')
  }

  const hours = Math.round(minutes / 60)
  if (Math.abs(hours) < 24) {
    return formatter.format(hours, 'hour')
  }

  const days = Math.round(hours / 24)
  return formatter.format(days, 'day')
}

export function formatPace(secondsPerKilometer: number): string {
  if (!Number.isFinite(secondsPerKilometer) || secondsPerKilometer <= 0) {
    return '—'
  }

  const minutes = Math.floor(secondsPerKilometer / 60)
  const seconds = Math.round(secondsPerKilometer % 60)
  return `${minutes}:${seconds.toString().padStart(2, '0')} / км`
}

export function formatSpeed(speedMetersPerSecond: number): string {
  if (!Number.isFinite(speedMetersPerSecond) || speedMetersPerSecond <= 0) {
    return '—'
  }

  return `${(speedMetersPerSecond * 3.6).toFixed(1)} км/ч`
}

export function normalizeCityFilter(city: string): string {
  return city.toLowerCase() === 'all cities' ? 'all' : city
}
