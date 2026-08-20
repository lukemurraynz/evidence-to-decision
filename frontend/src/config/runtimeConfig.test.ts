import { describe, expect, it } from 'vitest'
import { loadRuntimeConfig, RuntimeConfigError } from './runtimeConfig'

describe('runtime configuration', () => {
  it('uses an explicit same-origin default when config.json is absent', async () => {
    const fetchMock: typeof fetch = async () =>
      Promise.resolve(new Response(null, { status: 404 }))

    const config = await loadRuntimeConfig(
      fetchMock,
      'https://workshop.example',
    )

    expect(config.apiBaseUrl).toBe('https://workshop.example/')
    expect(config.source).toBe('same-origin')
  })

  it('fails closed when configured values are invalid', async () => {
    const fetchMock: typeof fetch = async () =>
      Promise.resolve(
        new Response(JSON.stringify({ requestTimeoutMs: 10 }), { status: 200 }),
      )

    await expect(
      loadRuntimeConfig(fetchMock, 'https://workshop.example'),
    ).rejects.toBeInstanceOf(RuntimeConfigError)
  })
})
