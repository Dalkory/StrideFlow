import type { AdSlotResponse } from '../lib/types'

interface AdSlotProps {
  slot: AdSlotResponse
}

export function AdSlot({ slot }: AdSlotProps) {
  return (
    <article className="surface ad-slot">
      <div className="section-heading">
        <span className="eyebrow">Ad ready</span>
        <span className={`chip ${slot.enabled ? 'chip--success' : 'chip--muted'}`}>
          {slot.enabled ? 'готов к продаже' : 'отключён'}
        </span>
      </div>

      <h3>{slot.title}</h3>
      <p>{slot.description}</p>

      <div className="ad-slot__footer">
        <span className="ad-slot__placement">{slot.placement}</span>
        <span className="ad-slot__state">
          {slot.is_placeholder ? 'placeholder creative' : 'production creative'}
        </span>
      </div>
    </article>
  )
}
