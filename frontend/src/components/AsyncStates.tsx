import { useEffect, useRef, type ReactNode } from 'react'

export function PageLoading({ label }: { readonly label: string }) {
  return (
    <section className="state-page" aria-busy="true" aria-live="polite">
      <span className="loading-rule" aria-hidden="true" />
      <p className="eyebrow">Getting the latest record</p>
      <h1>{label}</h1>
      <p>Checking the latest approved records and decisions.</p>
    </section>
  )
}

export function PageError({
  title,
  message,
  action,
}: {
  readonly title: string
  readonly message: string
  readonly action?: ReactNode
}) {
  const headingRef = useRef<HTMLHeadingElement | null>(null)
  useEffect(() => {
    headingRef.current?.focus()
  }, [])

  return (
    <section className="state-page state-error">
      <p className="eyebrow">Action needed</p>
      <h1 ref={headingRef} tabIndex={-1}>
        {title}
      </h1>
      <p>{message}</p>
      {action}
    </section>
  )
}

export function EmptyState({
  title,
  message,
}: {
  readonly title: string
  readonly message: string
}) {
  return (
    <div className="empty-state">
      <h2>{title}</h2>
      <p>{message}</p>
    </div>
  )
}
