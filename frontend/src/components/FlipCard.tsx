import { useState, type ReactNode } from 'react'

/**
 * Content-swap flip, not two absolutely-positioned stacked faces. Card descriptions vary too
 * much in length for a fixed-height double-sided layout to work without clipping. The brief
 * rotate animation on toggle is what sells "flip," not literal back-of-card geometry.
 * `prefers-reduced-motion` is handled in CSS (App.css), not here.
 */
export function FlipCard({
  front,
  back,
  flipLabel,
  unflipLabel = 'Back to overview',
}: {
  readonly front: ReactNode
  readonly back: ReactNode
  readonly flipLabel: string
  readonly unflipLabel?: string
}) {
  const [flipped, setFlipped] = useState(false)
  const [spinning, setSpinning] = useState(false)

  const toggle = (): void => {
    setFlipped((current) => !current)
    setSpinning(true)
    window.setTimeout(() => setSpinning(false), 400)
  }

  return (
    <div className={`flip-card${spinning ? ' is-spinning' : ''}`}>
      <div className="flip-card-face">{flipped ? back : front}</div>
      <button type="button" className="flip-card-toggle button-secondary" onClick={toggle}>
        {flipped ? unflipLabel : flipLabel}
      </button>
    </div>
  )
}
