import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  ApiParseError,
  ApiRequestError,
  OpportunityApiClient,
  parseRetryAfterMs,
} from './client'
import { isEngagement, OperationStatus } from './contracts'
import type { RuntimeConfig } from '../config/runtimeConfig'
import { engagementFixture } from '../test/fixtures'

const config: RuntimeConfig = {
  apiBaseUrl: 'https://workshop.example/',
  requestTimeoutMs: 15_000,
  pollMaxAttempts: 3,
  pollMaxElapsedMs: 60_000,
  source: 'config',
  authClientId: 'client-1',
  authTenantId: 'tenant-1',
  authScope: 'api://workshop/access_as_user',
}

const getAccessToken = async (): Promise<string> => 'test-token'

afterEach(() => {
  vi.useRealTimers()
})

describe('API contract parsing', () => {
  it('accepts a complete engagement and rejects an incomplete payload', () => {
    expect(isEngagement(engagementFixture())).toBe(true)
    expect(isEngagement({ id: 'engagement-1' })).toBe(false)
  })

  it('returns validated data and preserves the ETag', async () => {
    const fetchMock: typeof fetch = vi.fn(async () =>
      Promise.resolve(
        new Response(JSON.stringify(engagementFixture()), {
          status: 200,
          headers: { 'Content-Type': 'application/json', ETag: '"5"' },
        }),
      ),
    )
    const client = new OpportunityApiClient(config, getAccessToken, fetchMock)

    const result = await client.getEngagement('workspace-1', 'engagement-1')

    expect(result.data.id).toBe('engagement-1')
    expect(result.etag).toBe('"5"')
  })

  it('rejects an unreadable successful payload', async () => {
    const fetchMock: typeof fetch = vi.fn(async () =>
      Promise.resolve(new Response('{not-json', { status: 200 })),
    )
    const client = new OpportunityApiClient(config, getAccessToken, fetchMock)

    await expect(
      client.getEngagement('workspace-1', 'engagement-1'),
    ).rejects.toBeInstanceOf(ApiParseError)
  })

  it('parses Problem Details without exposing status codes as copy', async () => {
    const fetchMock: typeof fetch = vi.fn(async () =>
      Promise.resolve(
        new Response(
          JSON.stringify({
            type: 'https://workshop.example/problems/stale',
            title: 'Version conflict',
            status: 412,
            detail: 'Reload the engagement before saving.',
            correlationId: 'reference-1',
          }),
          {
            status: 412,
            headers: {
              'Content-Type': 'application/problem+json',
              'Retry-After': '2',
            },
          },
        ),
      ),
    )
    const client = new OpportunityApiClient(config, getAccessToken, fetchMock)

    const error = await client
      .getEngagement('workspace-1', 'engagement-1')
      .catch((reason: unknown) => reason)

    expect(error).toBeInstanceOf(ApiRequestError)
    if (!(error instanceof ApiRequestError)) {
      throw new Error('Expected an ApiRequestError.')
    }
    expect(error.message).toBe(
      'This engagement changed after it was opened. Reload it and try again.',
    )
    expect(error.retryAfterMs).toBe(2_000)
    expect(error.referenceId).toBe('reference-1')
  })

  it('parses Retry-After seconds and HTTP dates', () => {
    const now = Date.parse('2026-08-16T09:00:00Z')

    expect(parseRetryAfterMs('3', now)).toBe(3_000)
    expect(parseRetryAfterMs('Sun, 16 Aug 2026 09:00:05 GMT', now)).toBe(5_000)
    expect(parseRetryAfterMs('invalid', now)).toBeNull()
  })
})

describe('bounded operation polling', () => {
  it('honors Retry-After and stops when work succeeds', async () => {
    let request = 0
    const fetchMock: typeof fetch = vi.fn(async () => {
      request += 1
      const status =
        request === 1 ? OperationStatus.Running : OperationStatus.Succeeded
      return Promise.resolve(
        new Response(
          JSON.stringify({
            id: 'operation-1',
            workspaceId: 'workspace-1',
            operationType: 'recommendation',
            status,
            createdAt: '2026-08-16T09:00:00Z',
            updatedAt: '2026-08-16T09:00:01Z',
            correlationId: 'reference-1',
            retryAfterSeconds: 10,
          }),
          {
            status: 200,
            headers: request === 1 ? { 'Retry-After': '2' } : {},
          },
        ),
      )
    })
    const delays: number[] = []
    const client = new OpportunityApiClient(config, getAccessToken, fetchMock)

    const operation = await client.pollOperation('workspace-1', 'operation-1', {
      sleep: (milliseconds) => {
        delays.push(milliseconds)
        return Promise.resolve()
      },
    })

    expect(operation.status).toBe(OperationStatus.Succeeded)
    expect(delays).toEqual([2_000])
    expect(request).toBe(2)
  })

  it('passes cancellation to the active request', async () => {
    const fetchMock: typeof fetch = vi.fn(
      (_input: RequestInfo | URL, init?: RequestInit) => {
      return new Promise<Response>((_resolve, reject) => {
        if (init?.signal?.aborted === true) {
          reject(new DOMException('Canceled', 'AbortError'))
          return
        }
        init?.signal?.addEventListener(
          'abort',
          () => reject(new DOMException('Canceled', 'AbortError')),
          { once: true },
        )
      })
      },
    )
    const client = new OpportunityApiClient(config, getAccessToken, fetchMock)
    const controller = new AbortController()

    const pending = client.getEngagement(
      'workspace-1',
      'engagement-1',
      controller.signal,
    )
    controller.abort()

    await expect(pending).rejects.toMatchObject({ name: 'AbortError' })
  })
})
