import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { App } from './App'
import { engagementFixture, reviewFixture } from './test/fixtures'

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('role navigation and setup states', () => {
  it('presents each role and directs unconnected users to setup', async () => {
    const user = userEvent.setup()
    const fetchMock: typeof fetch = vi.fn(async (input) => {
      if (String(input) === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      throw new Error(`Unexpected request: ${String(input)}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    expect(
      await screen.findByRole('heading', {
        name: 'Move from workshop evidence to an accountable decision.',
      }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Evidence to Decision home' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Workshop facilitator' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'Decision reviewer' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Executive' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Delivery lead' })).toBeInTheDocument()
    expect(
      screen.getByText(
        'This site is ready to open an engagement.',
      ),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('link', { name: 'Capture evidence' }))

    const openEngagementHeading = await screen.findByRole('heading', {
      name: 'Find your engagement',
    })
    expect(openEngagementHeading).toBeInTheDocument()
    const openEngagementForm = openEngagementHeading.closest('section')
    expect(openEngagementForm).not.toBeNull()
    expect(within(openEngagementForm!).getByLabelText('Organization reference')).toBeRequired()
    expect(within(openEngagementForm!).getByLabelText('Engagement reference')).toBeRequired()

    expect(
      screen.getByRole('heading', { name: 'Start a new workshop' }),
    ).toBeInTheDocument()
  })

  it('shows a recoverable setup error instead of loading the application', async () => {
    const fetchMock: typeof fetch = vi.fn(async () =>
      Promise.resolve(new Response(null, { status: 500 })),
    )
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    const errorHeading = await screen.findByRole('heading', {
      name: 'Connection details unavailable',
    })
    expect(errorHeading).toHaveFocus()
    expect(document.title).toBe('Error: Evidence to Decision')
    expect(
      screen.getByRole('button', { name: 'Try connection again' }),
    ).toBeEnabled()
  })
})

describe('engagement experiences', () => {
  it('shows an explicit empty evidence state with accessible navigation', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/discover',
    )
    const fetchMock: typeof fetch = vi.fn(async (input) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(JSON.stringify(engagementFixture()), {
            status: 200,
            headers: { ETag: '"5"' },
          }),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Build the evidence trail' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { name: 'No evidence captured' }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('navigation', { name: 'Engagement' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Evidence' })).toHaveAttribute(
      'aria-current',
      'page',
    )
    expect(
      screen.getByRole('link', { name: 'Decision review' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Outcomes' })).toBeInTheDocument()
    expect(
      screen.getByRole('link', { name: 'Delivery documents' }),
    ).toBeInTheDocument()
    expect(screen.getByLabelText(/What was observed or said/)).toBeRequired()
  })

  it('does not offer retry when the session has expired', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/discover',
    )
    const fetchMock: typeof fetch = vi.fn(async (input) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              title: 'Unauthorized',
              status: 401,
            }),
            { status: 401 },
          ),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    expect(
      await screen.findByText('Your session has expired. Sign in again to continue.'),
    ).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
  })

  it('shows permission recovery without treating role navigation as authorization', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/review',
    )
    const fetchMock: typeof fetch = vi.fn(async (input) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              title: 'Access denied',
              status: 403,
              detail: 'Decision review access is required.',
            }),
            { status: 403 },
          ),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Access required' }),
    ).toHaveFocus()
    expect(
      screen.getByRole('link', { name: 'Decision review' }),
    ).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Try again' })).not.toBeInTheDocument()
  })

  it('links evidence validation errors to fields', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/discover',
    )
    const fetchMock: typeof fetch = vi.fn(async (input) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(JSON.stringify(engagementFixture()), {
            status: 200,
            headers: { ETag: '"5"' },
          }),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()

    render(<App />)

    await user.click(
      await screen.findByRole('button', { name: 'Save evidence' }),
    )

    const summary = await screen.findByRole('alert')
    await waitFor(() => expect(summary).toHaveFocus())
    expect(
      screen.getByRole('link', { name: 'Add what was observed or said' }),
    ).toHaveAttribute('href', '#evidence-statement')
    expect(screen.getByLabelText(/What was observed or said/)).toHaveAttribute(
      'aria-describedby',
      'evidence-statement-error',
    )
  })

  it('records a reviewer decision with concurrency protection', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/review',
    )
    const initialEngagement = engagementFixture({ withOpportunity: true })
    let decisionRequest: RequestInit | undefined
    const fetchMock: typeof fetch = vi.fn(async (input, init) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1') && init?.method !== 'POST') {
        return Promise.resolve(
          new Response(JSON.stringify(initialEngagement), {
            status: 200,
            headers: { ETag: '"5"' },
          }),
        )
      }
      if (url.endsWith('/opportunities/opportunity-1/review')) {
        return Promise.resolve(
          new Response(JSON.stringify(reviewFixture()), { status: 200 }),
        )
      }
      if (url.endsWith('/decisions') && init?.method === 'POST') {
        decisionRequest = init
        return Promise.resolve(
          new Response(JSON.stringify(initialEngagement), {
            status: 200,
            headers: { ETag: '"6"' },
          }),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()

    render(<App />)

    expect(
      await screen.findByRole('heading', {
        name: 'Record an accountable decision',
      }),
    ).toBeInTheDocument()
    await user.type(
      await screen.findByLabelText(/Rationale/),
      'Controls are sufficient for validation.',
    )
    await user.type(screen.getByLabelText(/Approval point/), 'Review board')
    await user.type(
      screen.getByLabelText(/Escalation path/),
      'Governance committee',
    )
    await user.click(screen.getByRole('button', { name: 'Save decision' }))

    expect(await screen.findByText('Decision saved.')).toHaveAttribute(
      'role',
      'status',
    )
    expect(decisionRequest).toBeDefined()
    const headers = new Headers(decisionRequest?.headers)
    expect(headers.get('If-Match')).toBe('"5"')
    expect(headers.get('Idempotency-Key')).not.toBeNull()
    expect(JSON.parse(String(decisionRequest?.body))).toMatchObject({
      opportunityId: 'opportunity-1',
      rationale: 'Controls are sufficient for validation.',
      approvalPoint: 'Review board',
      escalationPath: 'Governance committee',
    })
  })

  it('preserves decision edits while refreshing after a version conflict', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/review',
    )
    let engagementVersion = 5
    let engagementReads = 0
    const fetchMock: typeof fetch = vi.fn(async (input, init) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1') && init?.method !== 'POST') {
        engagementReads += 1
        if (engagementReads > 1) engagementVersion = 6
        return Promise.resolve(
          new Response(
            JSON.stringify({
              ...engagementFixture({ withOpportunity: true }),
              objectVersion: engagementVersion,
            }),
            {
              status: 200,
              headers: { ETag: `"${engagementVersion}"` },
            },
          ),
        )
      }
      if (url.endsWith('/opportunities/opportunity-1/review')) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              ...reviewFixture(),
              canonicalGraphVersion: engagementVersion,
            }),
            { status: 200 },
          ),
        )
      }
      if (url.endsWith('/decisions') && init?.method === 'POST') {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              title: 'Version conflict',
              status: 412,
              detail: 'The engagement changed.',
            }),
            { status: 412 },
          ),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()

    render(<App />)

    const rationale = await screen.findByLabelText(/Rationale/)
    await user.type(rationale, 'Keep this careful review rationale.')
    await user.type(screen.getByLabelText(/Approval point/), 'Review board')
    await user.type(
      screen.getByLabelText(/Escalation path/),
      'Governance committee',
    )
    await user.click(screen.getByRole('button', { name: 'Save decision' }))

    expect(
      await screen.findByText(
        /The engagement changed before this decision was saved/,
      ),
    ).toBeInTheDocument()
    await user.click(
      screen.getByRole('button', { name: 'Refresh record and keep edits' }),
    )

    expect(
      await screen.findByText(
        'Record refreshed. Your decision edits are still here.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByLabelText(/Rationale/)).toHaveValue(
      'Keep this careful review rationale.',
    )
    expect(screen.getByLabelText(/Approval point/)).toHaveValue('Review board')
  })

  it('shows accountable review-brief completion with evidence and approval', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/progress/operation-1?opportunity=opportunity-1',
    )
    const baseEngagement = engagementFixture({ withOpportunity: true })
    const engagement = {
      ...baseEngagement,
      evidence: [
        {
          id: 'evidence-1',
          type: 0,
          statement: 'Case preparation takes two hours.',
          sourceReference: 'Operations workshop',
          capturedAt: '2026-08-15T10:00:00Z',
          modality: 0,
          confidence: 0.85,
          validationStatus: 2,
          objectVersion: 1,
        },
      ],
      opportunities: baseEngagement.opportunities.map((opportunity) => ({
        ...opportunity,
        evidenceReferences: ['evidence-1'],
      })),
    }
    const fetchMock: typeof fetch = vi.fn(async (input) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(JSON.stringify(engagement), {
            status: 200,
            headers: { ETag: '"5"' },
          }),
        )
      }
      if (url.endsWith('/operations/operation-1')) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              id: 'operation-1',
              workspaceId: 'workspace-1',
              operationType: 'recommendation',
              status: 2,
              createdAt: '2026-08-15T10:00:00Z',
              updatedAt: '2026-08-15T10:01:00Z',
              correlationId: 'correlation-1',
              resultReference: 'recommendation-1',
              retryAfterSeconds: 1,
            }),
            { status: 200 },
          ),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    expect(
      await screen.findByText('AI-assisted · reviewer approval required'),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('heading', {
        name: 'Suggested next step: assess the brief and record the human decision',
      }),
    ).toBeInTheDocument()
    expect(
      screen.getByText('Case preparation takes two hours.'),
    ).toBeInTheDocument()
    expect(screen.getByText('Human approval is required.')).toBeInTheDocument()
    expect(
      screen.getByText(
        /The completed brief cannot be opened in this interface/,
      ),
    ).toBeInTheDocument()
  })
})

describe('framing and cards', () => {
  it('saves a workflow from the frame view and lists it', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/frame',
    )
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: URL | RequestInfo) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(JSON.stringify(engagementFixture()), {
            status: 200,
            headers: { ETag: '"5"' },
          }),
        )
      }
      if (url.endsWith('/engagements/engagement-1/workflows')) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              ...engagementFixture(),
              objectVersion: 6,
              workflows: [
                {
                  id: 'workflow-1',
                  trigger: 'A support ticket is opened',
                  actors: [],
                  inputs: [],
                  steps: ['Triage the ticket'],
                  decisions: [],
                  systems: [],
                  handoffs: [],
                  exceptions: [],
                  outputs: [],
                },
              ],
            }),
            { status: 200, headers: { ETag: '"6"' } },
          ),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock as unknown as typeof fetch)

    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Frame the workflow' }),
    ).toBeInTheDocument()
    await user.type(
      screen.getByLabelText(/Trigger/),
      'A support ticket is opened',
    )
    await user.type(screen.getByLabelText(/Steps, one per line/), 'Triage the ticket')
    await user.click(screen.getByRole('button', { name: 'Save workflow' }))

    expect(await screen.findByText('Workflow saved.')).toBeInTheDocument()
    expect(
      screen.getByText('A support ticket is opened', { selector: 'blockquote' }),
    ).toBeInTheDocument()
    expect(
      fetchMock.mock.calls.some(([input]) =>
        String(input).endsWith('/engagements/engagement-1/workflows'),
      ),
    ).toBe(true)
  })

  it('lists derived cards and refetches when the type filter changes', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/cards',
    )
    const user = userEvent.setup()
    const cardsRequests: string[] = []
    const fetchMock: typeof fetch = vi.fn(async (input) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(JSON.stringify(engagementFixture()), {
            status: 200,
            headers: { ETag: '"5"' },
          }),
        )
      }
      if (url.includes('/engagements/engagement-1/cards')) {
        cardsRequests.push(url)
        return Promise.resolve(
          new Response(
            JSON.stringify([
              {
                id: 'problem:problem-1',
                type: 'problem',
                title: 'Case preparation takes two hours.',
                description: 'Manual document assembly duplicates work.',
                tags: ['problem'],
                derivedFromId: 'problem-1',
                derivedFromVersion: 5,
                currentCanonicalGraphVersion: 5,
                staleness: 0,
              },
            ]),
            { status: 200 },
          ),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock)

    render(<App />)

    expect(
      await screen.findByText('Case preparation takes two hours.'),
    ).toBeInTheDocument()
    expect(screen.getByText('Current')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Type'), 'problem')

    await waitFor(() => {
      expect(cardsRequests.some((url) => url.includes('type=problem'))).toBe(true)
    })
  })
})

describe('journey mapping', () => {
  it('saves a persona then a journey map that references it', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/journey-map',
    )
    const user = userEvent.setup()
    const personaFixture = {
      id: 'persona-1',
      name: 'Riley the case worker',
      role: 'Case worker',
      goals: [],
      painPoints: [],
      characteristics: [],
    }
    const fetchMock = vi.fn(async (input: URL | RequestInfo) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(JSON.stringify(engagementFixture()), {
            status: 200,
            headers: { ETag: '"5"' },
          }),
        )
      }
      if (url.endsWith('/engagements/engagement-1/personas')) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              ...engagementFixture(),
              objectVersion: 6,
              personas: [personaFixture],
            }),
            { status: 200, headers: { ETag: '"6"' } },
          ),
        )
      }
      if (url.endsWith('/engagements/engagement-1/journey-maps')) {
        return Promise.resolve(
          new Response(
            JSON.stringify({
              ...engagementFixture(),
              objectVersion: 7,
              personas: [personaFixture],
              journeyMaps: [
                {
                  id: 'journey-map-1',
                  personaId: 'persona-1',
                  workflowId: null,
                  steps: [
                    {
                      id: 'step-1',
                      order: 1,
                      name: 'Open the case file',
                      painPoint: 'Documents are scattered',
                      opportunityArea: 'Automate retrieval',
                      successMetric: 'Time to open case file',
                    },
                  ],
                },
              ],
            }),
            { status: 200, headers: { ETag: '"7"' } },
          ),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock as unknown as typeof fetch)

    render(<App />)

    expect(
      await screen.findByRole('heading', { name: "Map the persona's journey" }),
    ).toBeInTheDocument()
    await user.type(screen.getByLabelText(/^Name/), 'Riley the case worker')
    await user.type(screen.getByLabelText(/^Role/), 'Case worker')
    await user.click(screen.getByRole('button', { name: 'Save persona' }))

    expect(await screen.findByText('Persona saved.')).toBeInTheDocument()
    expect(
      screen.getByText('Riley the case worker', { selector: 'blockquote' }),
    ).toBeInTheDocument()

    await user.type(
      screen.getByLabelText(/^Step name/),
      'Open the case file',
    )
    await user.click(screen.getByRole('button', { name: 'Save journey map' }))

    expect(await screen.findByText('Journey map saved.')).toBeInTheDocument()
    expect(
      fetchMock.mock.calls.some(([input]) =>
        String(input).endsWith('/engagements/engagement-1/journey-maps'),
      ),
    ).toBe(true)
  })
})

describe('discovery cards', () => {
  it('browses AI capability cards and filters by category and search', async () => {
    window.history.replaceState(
      {},
      '',
      '/?workspace=workspace-1&engagement=engagement-1#/discovery-cards',
    )
    const user = userEvent.setup()
    const fetchMock = vi.fn(async (input: URL | RequestInfo) => {
      const url = String(input)
      if (url === '/config.json') {
        return Promise.resolve(new Response(null, { status: 404 }))
      }
      if (url.endsWith('/engagements/engagement-1')) {
        return Promise.resolve(
          new Response(JSON.stringify(engagementFixture()), {
            status: 200,
            headers: { ETag: '"5"' },
          }),
        )
      }
      throw new Error(`Unexpected request: ${url}`)
    })
    vi.stubGlobal('fetch', fetchMock as unknown as typeof fetch)

    render(<App />)

    expect(
      await screen.findByRole('heading', { name: 'Spark the workshop conversation' }),
    ).toBeInTheDocument()
    const totalCountText = screen.getByText(/of \d+ cards/).textContent
    expect(totalCountText).toMatch(/^\d+ of \d+ cards$/)

    await user.selectOptions(screen.getByLabelText('Category'), 'communication')
    expect(
      await screen.findByRole('heading', { name: 'Engage in natural conversations' }),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: 'Automate home operations' }),
    ).not.toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Category'), '')
    await user.type(screen.getByLabelText('Search'), 'automate home operations')
    expect(
      await screen.findByRole('heading', { name: 'Automate home operations' }),
    ).toBeInTheDocument()
  })
})
