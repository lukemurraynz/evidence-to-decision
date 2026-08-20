import { useEffect, useMemo, useState } from 'react'
import {
  ApiRequestError,
  OpportunityApiClient,
  type AccessTokenProvider,
} from './api/client'
import type { Engagement } from './api/contracts'
import {
  createMsalClient,
  ensureSignedIn,
  getAccessToken as acquireMsalAccessToken,
} from './auth/msalClient'
import {
  emptyEvidenceDraft,
  type EvidenceDraft,
} from './app/evidenceDraft'
import { PAGE_NAMES, PRODUCT_NAME } from './app/names'
import {
  useBrowserLocation,
  type Route,
  type WorkspaceSelection,
} from './app/routing'
import { AppShell } from './components/AppShell'
import { PageError, PageLoading } from './components/AsyncStates'
import {
  loadRuntimeConfig,
  type RuntimeConfig,
} from './config/runtimeConfig'
import { CardsView } from './views/CardsView'
import { DiscoveryCardsView } from './views/DiscoveryCardsView'
import { BoardRouteView } from './views/BoardRouteView'
import { EvidenceWorkbench } from './views/EvidenceWorkbench'
import { ExecutiveView } from './views/ExecutiveView'
import { FrameView } from './views/FrameView'
import { HandoffView } from './views/HandoffView'
import { HomeView, WorkspaceSetup } from './views/HomeView'
import { IdeationView } from './views/IdeationView'
import { JoinView } from './views/JoinView'
import { JourneyMapView } from './views/JourneyMapView'
import { ProgressView } from './views/ProgressView'
import { ReviewView } from './views/ReviewView'
import './App.css'

type ConfigState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly config: RuntimeConfig }
  | { readonly status: 'error'; readonly message: string }

type AuthState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly getAccessToken: AccessTokenProvider }
  | { readonly status: 'error'; readonly message: string }

const noAuthConfigured: AccessTokenProvider = async () => ''

type EngagementState =
  | { readonly status: 'idle' }
  | { readonly status: 'loading' }
  | {
      readonly status: 'ready'
      readonly engagement: Engagement
      readonly etag: string | null
      readonly checkedAt: string
    }
  | {
      readonly status: 'error'
      readonly message: string
      readonly retryable: boolean
      readonly kind: 'session' | 'permission' | 'missing' | 'offline' | 'service'
    }

function routeTitle(route: Route): string {
  const pageName = PAGE_NAMES[route.name]
  return route.name === 'home'
    ? PRODUCT_NAME
    : `${pageName} | ${PRODUCT_NAME}`
}

export function App() {
  const { route, selection } = useBrowserLocation()
  const [configAttempt, setConfigAttempt] = useState(0)
  const [configState, setConfigState] = useState<ConfigState>({
    status: 'loading',
  })
  const [authState, setAuthState] = useState<AuthState>({ status: 'loading' })
  const [engagementAttempt, setEngagementAttempt] = useState(0)
  const [engagementState, setEngagementState] = useState<EngagementState>({
    status: 'idle',
  })
  const [isOnline, setIsOnline] = useState(navigator.onLine)
  const [evidenceDraft, setEvidenceDraft] =
    useState<EvidenceDraft>(emptyEvidenceDraft)

  useEffect(() => {
    setEvidenceDraft(emptyEvidenceDraft)
  }, [selection?.workspaceId, selection?.engagementId])

  useEffect(() => {
    const hasError =
      configState.status === 'error' || engagementState.status === 'error'
    document.title = `${hasError ? 'Error: ' : ''}${routeTitle(route)}`
  }, [configState.status, engagementState.status, route])

  useEffect(() => {
    const wentOnline = (): void => setIsOnline(true)
    const wentOffline = (): void => setIsOnline(false)
    window.addEventListener('online', wentOnline)
    window.addEventListener('offline', wentOffline)
    return () => {
      window.removeEventListener('online', wentOnline)
      window.removeEventListener('offline', wentOffline)
    }
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    let disposed = false
    const timeout = window.setTimeout(
      () => controller.abort(new DOMException('Setup timed out.', 'TimeoutError')),
      8_000,
    )
    setConfigState({ status: 'loading' })
    void loadRuntimeConfig(fetch, window.location.origin, controller.signal)
      .then((config) => {
        window.clearTimeout(timeout)
        if (disposed) return
        setConfigState({ status: 'ready', config })
      })
      .catch((error: unknown) => {
        window.clearTimeout(timeout)
        if (disposed) return
        setConfigState({
          status: 'error',
          message:
            error instanceof Error
              ? error.message
              : 'Connection details could not be loaded.',
        })
      })
    return () => {
      disposed = true
      window.clearTimeout(timeout)
      controller.abort()
    }
  }, [configAttempt])

  useEffect(() => {
    if (configState.status !== 'ready' || route.name === 'join') return
    const { authClientId, authTenantId, authScope } = configState.config
    if (!authClientId || !authTenantId || !authScope) {
      setAuthState({ status: 'ready', getAccessToken: noAuthConfigured })
      return
    }
    let disposed = false
    setAuthState({ status: 'loading' })
    const { instance, scope } = createMsalClient(authClientId, authTenantId, authScope)
    void ensureSignedIn(instance, scope)
      .then(() => {
        if (disposed) return
        setAuthState({
          status: 'ready',
          getAccessToken: () => acquireMsalAccessToken(instance, scope),
        })
      })
      .catch((error: unknown) => {
        if (disposed) return
        setAuthState({
          status: 'error',
          message:
            error instanceof Error
              ? error.message
              : 'Sign-in failed. Reload the page to try again.',
        })
      })
    return () => {
      disposed = true
    }
  }, [configState, route.name])

  const client = useMemo(
    () =>
      configState.status === 'ready' && authState.status === 'ready'
        ? new OpportunityApiClient(configState.config, authState.getAccessToken)
        : null,
    [configState, authState],
  )

  useEffect(() => {
    if (client === null || selection === null || route.name === 'join') {
      setEngagementState({ status: 'idle' })
      return
    }
    const controller = new AbortController()
    setEngagementState({ status: 'loading' })
    void client
      .getEngagement(
        selection.workspaceId,
        selection.engagementId,
        controller.signal,
      )
      .then((result) => {
        setEngagementState({
          status: 'ready',
          engagement: result.data,
          etag: result.etag,
          checkedAt: new Date().toISOString(),
        })
      })
      .catch((error: unknown) => {
        if (controller.signal.aborted) return
        const status = error instanceof ApiRequestError ? error.status : null
        setEngagementState({
          status: 'error',
          message:
            error instanceof ApiRequestError
              ? error.message
              : 'The engagement could not be reached. Check the connection and try again.',
          retryable:
            !(error instanceof ApiRequestError) ||
            ![401, 403, 404, 422].includes(error.status),
          kind:
            status === 401
              ? 'session'
              : status === 403
                ? 'permission'
                : status === 404
                  ? 'missing'
                  : !navigator.onLine
                    ? 'offline'
                    : 'service',
        })
      })
    return () => controller.abort()
  }, [client, engagementAttempt, selection, route.name])

  const connectionMessage =
    configState.status === 'ready'
      ? selection === null
        ? 'Connection ready'
        : engagementState.status === 'ready'
          ? `Record checked ${formatCheckedAt(engagementState.checkedAt)}`
          : 'Checking engagement access'
      : 'Connection unavailable'

  return (
    <AppShell
      route={route}
      selection={selection}
      connectionMessage={connectionMessage}
      isOnline={isOnline}
      lastCheckedAt={
        engagementState.status === 'ready'
          ? formatCheckedAt(engagementState.checkedAt)
          : null
      }
    >
      {renderRoute({
        route,
        selection,
        configState,
        authState,
        client,
        engagementState,
        isOnline,
        evidenceDraft,
        setEvidenceDraft,
        retryConfig: () => setConfigAttempt((attempt) => attempt + 1),
        retryEngagement: () =>
          setEngagementAttempt((attempt) => attempt + 1),
        updateEngagement: (engagement, etag) =>
          setEngagementState({
            status: 'ready',
            engagement,
            etag,
            checkedAt: new Date().toISOString(),
          }),
      })}
    </AppShell>
  )
}

type RenderRouteOptions = {
  readonly route: Route
  readonly selection: WorkspaceSelection | null
  readonly configState: ConfigState
  readonly authState: AuthState
  readonly client: OpportunityApiClient | null
  readonly engagementState: EngagementState
  readonly isOnline: boolean
  readonly evidenceDraft: EvidenceDraft
  readonly setEvidenceDraft: React.Dispatch<React.SetStateAction<EvidenceDraft>>
  readonly retryConfig: () => void
  readonly retryEngagement: () => void
  readonly updateEngagement: (
    engagement: Engagement,
    etag: string | null,
  ) => void
}

function renderRoute(options: RenderRouteOptions) {
  if (options.configState.status === 'loading') {
    return <PageLoading label="Checking the connection" />
  }
  if (options.configState.status === 'error') {
    return (
      <PageError
        title="Connection details unavailable"
        message={options.configState.message}
        action={
          <button type="button" onClick={options.retryConfig}>
            Try connection again
          </button>
        }
      />
    )
  }
  if (options.route.name === 'home') {
    return (
      <HomeView
        hasSelection={options.selection !== null}
        usingSameOrigin={options.configState.config.source === 'same-origin'}
        client={options.client}
      />
    )
  }
  if (options.route.name === 'join') {
    return (
      <JoinView apiBaseUrl={options.configState.config.apiBaseUrl} joinCode={options.route.joinCode} />
    )
  }
  if (options.route.name === 'not-found') {
    return (
      <PageError
        title="Page not found"
        message="This page does not exist. Return to the engagement home."
        action={
          <a className="button-link" href="#/">
            Return home
          </a>
        }
      />
    )
  }
  if (options.selection === null) {
    return (
      <section className="page">
        <WorkspaceSetup
          usingSameOrigin={options.configState.config.source === 'same-origin'}
          client={options.client}
        />
      </section>
    )
  }
  if (options.client === null) {
    if (options.authState.status === 'loading') {
      return <PageLoading label="Signing in" />
    }
    if (options.authState.status === 'error') {
      return (
        <PageError title="Sign-in failed" message={options.authState.message} />
      )
    }
    return (
      <PageError
        title="Connection unavailable"
        message="Try the connection again before opening this engagement."
      />
    )
  }
  if (options.engagementState.status === 'loading') {
    return <PageLoading label="Loading the engagement" />
  }
  if (options.engagementState.status === 'error') {
    const errorTitles = {
      session: 'Session ended',
      permission: 'Access required',
      missing: 'Engagement not found',
      offline: 'Engagement unavailable offline',
      service: 'Engagement unavailable',
    } as const
    return (
      <PageError
        title={errorTitles[options.engagementState.kind]}
        message={options.engagementState.message}
        action={
          options.engagementState.retryable ? (
            <button type="button" onClick={options.retryEngagement}>
              Try again
            </button>
          ) : undefined
        }
      />
    )
  }
  if (options.engagementState.status !== 'ready') {
    return (
      <PageError
        title="Choose an engagement"
        message="Enter the engagement references supplied by your workshop administrator."
      />
    )
  }

  const shared = {
    client: options.client,
    workspaceId: options.selection.workspaceId,
    engagement: options.engagementState.engagement,
  }
  if (options.route.name === 'progress') {
    return (
      <ProgressView
        {...shared}
        operationId={options.route.operationId}
        {...(options.route.opportunityId === undefined
          ? {}
          : { opportunityId: options.route.opportunityId })}
      />
    )
  }
  switch (options.route.name) {
    case 'discover':
      return (
        <EvidenceWorkbench
          {...shared}
          etag={options.engagementState.etag}
          isOnline={options.isOnline}
          draft={options.evidenceDraft}
          setDraft={options.setEvidenceDraft}
          onUpdated={options.updateEngagement}
        />
      )
    case 'ideation':
      return (
        <IdeationView
          {...shared}
          etag={options.engagementState.etag}
          isOnline={options.isOnline}
          onUpdated={options.updateEngagement}
        />
      )
    case 'discovery-cards':
      return (
        <DiscoveryCardsView
          {...shared}
          etag={options.engagementState.etag}
          isOnline={options.isOnline}
          onUpdated={options.updateEngagement}
        />
      )
    case 'board':
      return <BoardRouteView {...shared} isOnline={options.isOnline} />
    case 'journey-map':
      return (
        <JourneyMapView
          {...shared}
          etag={options.engagementState.etag}
          isOnline={options.isOnline}
          onUpdated={options.updateEngagement}
        />
      )
    case 'frame':
      return (
        <FrameView
          {...shared}
          etag={options.engagementState.etag}
          isOnline={options.isOnline}
          onUpdated={options.updateEngagement}
        />
      )
    case 'cards':
      return <CardsView {...shared} />
    case 'review':
      return (
        <ReviewView
          {...shared}
          etag={options.engagementState.etag}
          isOnline={options.isOnline}
          onUpdated={options.updateEngagement}
        />
      )
    case 'outcomes':
      return <ExecutiveView engagement={options.engagementState.engagement} />
    case 'handoff':
      return <HandoffView {...shared} isOnline={options.isOnline} />
  }

}

function formatCheckedAt(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
