import type { ReactNode } from 'react'

interface StatCardProps {
  label: string
  value: string
  hint: string
  icon: ReactNode
  tone?: 'default' | 'accent' | 'warm'
}

export function StatCard({
  label,
  value,
  hint,
  icon,
  tone = 'default',
}: StatCardProps) {
  return (
    <article className={`surface stat-card stat-card--${tone}`}>
      <div className="stat-card__icon">{icon}</div>
      <div className="stat-card__content">
        <span className="stat-card__label">{label}</span>
        <strong className="stat-card__value">{value}</strong>
        <span className="stat-card__hint">{hint}</span>
      </div>
    </article>
  )
}
