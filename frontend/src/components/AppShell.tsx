import { useEffect, useRef, type ReactNode } from 'react'
import {
  PRIMARY_NAVIGATION,
  PRODUCT_NAME,
  ROLE_BY_ROUTE,
} from '../app/names'
import type { Route, WorkspaceSelection } from '../app/routing'

type AppShellProps = {
  readonly route: Route
  readonly selection: WorkspaceSelection | null
  readonly connectionMessage: string
  readonly isOnline: boolean
  readonly lastCheckedAt: string | null
  readonly children: ReactNode
}

export function AppShell({
  route,
  selection,
  connectionMessage,
  isOnline,
  lastCheckedAt,
  children,
}: AppShellProps) {
  const mainRef = useRef<HTMLElement>(null)
  const isFullBleed = route.name === 'board'

  useEffect(() => {
    mainRef.current?.focus()
  }, [route.name])

  return (
    <div className={`app-shell${isFullBleed ? ' app-shell-full-bleed' : ''}`}>
      <a className="skip-link" href="#main-content">
        Skip to main content
      </a>
      <header className="masthead">
        <a className="wordmark" href="#/" aria-label={`${PRODUCT_NAME} home`}>
          <span className="wordmark-mark" aria-hidden="true">
            E/D
          </span>
          <span>{PRODUCT_NAME}</span>
        </a>
        <div className="connection-line" role="status" aria-live="polite">
          <span
            className={`connection-dot ${isOnline ? 'is-online' : 'is-offline'}`}
            aria-hidden="true"
          />
          {isOnline
            ? connectionMessage
            : lastCheckedAt === null
              ? 'No internet. Connect to open the engagement.'
              : `No internet. Showing the record checked ${lastCheckedAt}; changes are paused.`}
        </div>
      </header>

      {selection !== null && (
        <nav className="primary-nav" aria-label="Engagement">
          <div className="workspace-context" aria-label="Current work context">
            <span>
              Organization <strong>{selection.workspaceId}</strong>
            </span>
            <span>
              Engagement <strong>{selection.engagementId}</strong>
            </span>
            {route.name in ROLE_BY_ROUTE && (
              <span>
                Working as{' '}
                <strong>
                  {ROLE_BY_ROUTE[route.name as keyof typeof ROLE_BY_ROUTE]}
                </strong>
              </span>
            )}
          </div>
          <ul>
            {PRIMARY_NAVIGATION.map((item) => (
              <li key={item.route}>
                <a
                  href={item.href}
                  aria-current={
                    route.name === item.route ||
                    (route.name === 'progress' && item.route === 'review')
                      ? 'page'
                      : undefined
                  }
                >
                  {item.label}
                </a>
              </li>
            ))}
          </ul>
        </nav>
      )}

      <main id="main-content" ref={mainRef} tabIndex={-1}>
        {children}
      </main>
      {!isFullBleed && (
        <footer>
          <p>Evidence stays attributable. Decisions stay accountable.</p>
        </footer>
      )}
    </div>
  )
}
