import { useEffect, useState } from 'react'

export type Route =
  | { readonly name: 'home' }
  | { readonly name: 'discover' }
  | { readonly name: 'ideation' }
  | { readonly name: 'discovery-cards' }
  | { readonly name: 'board' }
  | { readonly name: 'journey-map' }
  | { readonly name: 'frame' }
  | { readonly name: 'cards' }
  | { readonly name: 'review' }
  | { readonly name: 'outcomes' }
  | { readonly name: 'handoff' }
  | {
      readonly name: 'progress'
      readonly operationId: string
      readonly opportunityId?: string
    }
  | {
      readonly name: 'join'
      readonly joinCode: string
    }
  | { readonly name: 'not-found' }

export type WorkspaceSelection = {
  readonly workspaceId: string
  readonly engagementId: string
}

export function parseRoute(hash: string): Route {
  const path = hash.replace(/^#/, '') || '/'
  const [pathname = '/', query = ''] = path.split('?', 2)
  if (pathname === '/') return { name: 'home' }
  if (pathname === '/discover') return { name: 'discover' }
  if (pathname === '/ideation') return { name: 'ideation' }
  if (pathname === '/discovery-cards') return { name: 'discovery-cards' }
  if (pathname === '/board') return { name: 'board' }
  if (pathname === '/journey-map') return { name: 'journey-map' }
  if (pathname === '/frame') return { name: 'frame' }
  if (pathname === '/cards') return { name: 'cards' }
  if (pathname === '/review') return { name: 'review' }
  if (pathname === '/outcomes') return { name: 'outcomes' }
  if (pathname === '/handoff') return { name: 'handoff' }
  const progressMatch = /^\/progress\/([^/]+)$/.exec(pathname)
  const operationId = progressMatch?.[1]
  if (operationId !== undefined) {
    const opportunityId = new URLSearchParams(query).get('opportunity')?.trim()
    let decodedOperationId: string
    try {
      decodedOperationId = decodeURIComponent(operationId)
    } catch {
      return { name: 'not-found' }
    }
    return {
      name: 'progress',
      operationId: decodedOperationId,
      ...(opportunityId ? { opportunityId } : {}),
    }
  }
  const joinMatch = /^\/join\/([^/]+)$/.exec(pathname)
  const joinCode = joinMatch?.[1]
  if (joinCode !== undefined) {
    let decodedJoinCode: string
    try {
      decodedJoinCode = decodeURIComponent(joinCode)
    } catch {
      return { name: 'not-found' }
    }
    return { name: 'join', joinCode: decodedJoinCode }
  }
  return { name: 'not-found' }
}

export function readWorkspaceSelection(search: string): WorkspaceSelection | null {
  const parameters = new URLSearchParams(search)
  const workspaceId = parameters.get('workspace')?.trim()
  const engagementId = parameters.get('engagement')?.trim()
  return workspaceId && engagementId ? { workspaceId, engagementId } : null
}

export function useBrowserLocation(): {
  readonly route: Route
  readonly selection: WorkspaceSelection | null
} {
  const read = (): {
    readonly route: Route
    readonly selection: WorkspaceSelection | null
  } => ({
    route: parseRoute(window.location.hash),
    selection: readWorkspaceSelection(window.location.search),
  })
  const [location, setLocation] = useState(read)

  useEffect(() => {
    const update = (): void => setLocation(read())
    window.addEventListener('hashchange', update)
    window.addEventListener('popstate', update)
    return () => {
      window.removeEventListener('hashchange', update)
      window.removeEventListener('popstate', update)
    }
  }, [])

  return location
}

export function connectWorkspace(selection: WorkspaceSelection): void {
  const url = new URL(window.location.href)
  url.searchParams.set('workspace', selection.workspaceId)
  url.searchParams.set('engagement', selection.engagementId)
  window.history.pushState({}, '', url)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

export function navigateTo(route: string): void {
  window.location.hash = route
}
