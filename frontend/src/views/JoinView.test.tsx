import { cleanup, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { act } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { JoinView } from './JoinView'
import { createFakeHubConnection, type FakeHubConnection } from '../test/signalrHarness'

const { getFakeConnection, setFakeConnection } = vi.hoisted(() => {
  let current: FakeHubConnection | null = null
  return {
    getFakeConnection: (): FakeHubConnection => {
      if (current === null) throw new Error('No fake connection configured for this test')
      return current
    },
    setFakeConnection: (connection: FakeHubConnection): void => {
      current = connection
    },
  }
})

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    withUrl() {
      return this
    }
    withAutomaticReconnect() {
      return this
    }
    configureLogging() {
      return this
    }
    build() {
      return getFakeConnection()
    }
  },
  LogLevel: { Warning: 2 },
}))

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

function stubJoinResponse(response: Readonly<Record<string, unknown>>): void {
  vi.stubGlobal(
    'fetch',
    vi.fn(async () => new Response(JSON.stringify(response), { status: 200 })),
  )
}

describe('JoinView ideation round', () => {
  it('submits an idea over the hub and renders the live board as it updates', async () => {
    const user = userEvent.setup()
    const connection = createFakeHubConnection()
    setFakeConnection(connection)
    stubJoinResponse({
      token: 'participant-token',
      workspaceId: 'workspace-1',
      engagementId: 'engagement-1',
      joinSessionId: 'session-1',
      journeyStepId: null,
      journeyStepName: null,
      journeyStepPainPoint: null,
      shortlistedDiscoveryCardIds: [],
    })

    render(<JoinView apiBaseUrl="https://api.example" joinCode="ABC123" />)
    await user.type(screen.getByLabelText('Your name'), 'Riley')
    await user.click(screen.getByRole('button', { name: 'Join' }))

    expect(await screen.findByRole('heading', { name: "You're in, Riley" })).toBeInTheDocument()

    await user.type(screen.getByLabelText('Your idea'), 'Skip the re-keying step entirely.')
    await user.click(screen.getByRole('button', { name: 'Submit idea' }))

    expect(connection.invocations).toContainEqual({
      method: 'SubmitIdea',
      args: ['Skip the re-keying step entirely.'],
    })

    act(() => {
      connection.push('IdeationBoardUpdated', [
        {
          id: 'note-1',
          workspaceId: 'workspace-1',
          joinSessionId: 'session-1',
          participantId: 'participant-1',
          displayName: 'Riley',
          text: 'Skip the re-keying step entirely.',
          submittedAt: '2026-08-19T00:00:00Z',
        },
      ])
    })

    expect(await screen.findByText('Skip the re-keying step entirely.')).toBeInTheDocument()
  })
})

describe('JoinView pin and browse', () => {
  it('toggles a pin over the hub and reflects the returned pinned state', async () => {
    const user = userEvent.setup()
    const connection = createFakeHubConnection({
      TogglePin: {
        pinned: true,
        tally: [{ discoveryCardId: 'navigation-and-control-automate-home-operations', journeyStepId: 'step-1', count: 1 }],
      },
    })
    setFakeConnection(connection)
    stubJoinResponse({
      token: 'participant-token',
      workspaceId: 'workspace-1',
      engagementId: 'engagement-1',
      joinSessionId: 'session-1',
      journeyStepId: 'step-1',
      journeyStepName: 'Receive the claim',
      journeyStepPainPoint: 'Re-keying',
      shortlistedDiscoveryCardIds: [],
    })

    render(<JoinView apiBaseUrl="https://api.example" joinCode="ABC123" />)
    await user.type(screen.getByLabelText('Your name'), 'Riley')
    await user.click(screen.getByRole('button', { name: 'Join' }))

    const cardHeading = await screen.findByRole('heading', { name: 'Automate home operations' })
    const card = cardHeading.closest('li')
    if (card === null) throw new Error('Expected the card heading to be inside a list item')
    const pinButton = within(card).getByRole('button', { name: 'Pin for later' })

    await user.click(pinButton)

    expect(connection.invocations).toContainEqual({
      method: 'TogglePin',
      args: ['navigation-and-control-automate-home-operations', 'step-1'],
    })
    expect(await within(card).findByRole('button', { name: '✓ Pinned' })).toBeInTheDocument()
  })
})
