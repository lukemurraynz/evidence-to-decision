import { isRecord } from '../api/contracts'

export type RuntimeConfig = {
  readonly apiBaseUrl: string
  readonly requestTimeoutMs: number
  readonly pollMaxAttempts: number
  readonly pollMaxElapsedMs: number
  readonly source: 'config' | 'same-origin'
  readonly authClientId: string
  readonly authTenantId: string
  readonly authScope: string
}

type RuntimeConfigDocument = {
  readonly apiBaseUrl?: string
  readonly requestTimeoutMs?: number
  readonly pollMaxAttempts?: number
  readonly pollMaxElapsedMs?: number
  readonly authClientId?: string
  readonly authTenantId?: string
  readonly authScope?: string
}

export class RuntimeConfigError extends Error {
  public constructor(message: string, options?: ErrorOptions) {
    super(message, options)
    this.name = 'RuntimeConfigError'
  }
}

function isPositiveIntegerInRange(
  value: unknown,
  minimum: number,
  maximum: number,
): value is number {
  return (
    Number.isInteger(value) &&
    Number(value) >= minimum &&
    Number(value) <= maximum
  )
}

function isRuntimeConfigDocument(value: unknown): value is RuntimeConfigDocument {
  return (
    isRecord(value) &&
    (value.apiBaseUrl === undefined || typeof value.apiBaseUrl === 'string') &&
    (value.requestTimeoutMs === undefined ||
      isPositiveIntegerInRange(value.requestTimeoutMs, 1_000, 60_000)) &&
    (value.pollMaxAttempts === undefined ||
      isPositiveIntegerInRange(value.pollMaxAttempts, 1, 60)) &&
    (value.pollMaxElapsedMs === undefined ||
      isPositiveIntegerInRange(value.pollMaxElapsedMs, 1_000, 600_000)) &&
    (value.authClientId === undefined || typeof value.authClientId === 'string') &&
    (value.authTenantId === undefined || typeof value.authTenantId === 'string') &&
    (value.authScope === undefined || typeof value.authScope === 'string')
  )
}

function resolveApiBaseUrl(value: string | undefined, origin: string): string {
  const resolved = new URL(value ?? origin, origin)
  if (resolved.protocol !== 'http:' && resolved.protocol !== 'https:') {
    throw new RuntimeConfigError('The connection address is not valid.')
  }
  return resolved.toString()
}

export async function loadRuntimeConfig(
  fetchImplementation: typeof fetch = fetch,
  origin = window.location.origin,
  signal?: AbortSignal,
): Promise<RuntimeConfig> {
  let response: Response
  try {
    response = await fetchImplementation('/config.json', {
      cache: 'no-store',
      credentials: 'same-origin',
      ...(signal === undefined ? {} : { signal }),
    })
  } catch (error: unknown) {
    throw new RuntimeConfigError(
      'Connection details could not be loaded. Check the connection and reload.',
      { cause: error },
    )
  }

  if (response.status === 404) {
    return {
      apiBaseUrl: resolveApiBaseUrl(undefined, origin),
      requestTimeoutMs: 15_000,
      pollMaxAttempts: 20,
      pollMaxElapsedMs: 180_000,
      source: 'same-origin',
      authClientId: '',
      authTenantId: '',
      authScope: '',
    }
  }

  if (!response.ok) {
    throw new RuntimeConfigError(
      'Connection details could not be loaded. Ask an administrator to check the site configuration.',
    )
  }

  let body: unknown
  try {
    body = (await response.json()) as unknown
  } catch (error: unknown) {
    throw new RuntimeConfigError(
      'Connection details are not valid. Ask an administrator to check the site configuration.',
      { cause: error },
    )
  }

  if (!isRuntimeConfigDocument(body)) {
    throw new RuntimeConfigError(
      'Connection details contain invalid values. Ask an administrator to check the site configuration.',
    )
  }

  return {
    apiBaseUrl: resolveApiBaseUrl(body.apiBaseUrl, origin),
    requestTimeoutMs: body.requestTimeoutMs ?? 15_000,
    pollMaxAttempts: body.pollMaxAttempts ?? 20,
    pollMaxElapsedMs: body.pollMaxElapsedMs ?? 180_000,
    source: 'config',
    authClientId: body.authClientId ?? '',
    authTenantId: body.authTenantId ?? '',
    authScope: body.authScope ?? '',
  }
}
