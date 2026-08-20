/**
 * A fake HubConnection capturing `.on(event, handler)` registrations so a test can simulate a
 * server push via `.push(event, ...args)`, and recording every `.invoke(method, ...args)` call
 * so a test can assert on what the component sent. `vi.mock('@microsoft/signalr', ...)` and the
 * `vi.hoisted` plumbing that wires a connection created here into `HubConnectionBuilder.build()`
 * must still live in each test file: Vitest only hoists `vi.mock` within the file that calls it.
 */
export type FakeHubConnection = {
  readonly invocations: readonly { readonly method: string; readonly args: readonly unknown[] }[]
  on(event: string, handler: (...args: readonly unknown[]) => void): void
  invoke(method: string, ...args: readonly unknown[]): Promise<unknown>
  start(): Promise<void>
  stop(): Promise<void>
  onreconnected(callback: () => void): void
  push(event: string, ...args: readonly unknown[]): void
}

export function createFakeHubConnection(
  invokeResults: Readonly<Record<string, unknown>> = {},
): FakeHubConnection {
  const handlers = new Map<string, (...args: readonly unknown[]) => void>()
  const invocations: { method: string; args: readonly unknown[] }[] = []
  return {
    invocations,
    on(event, handler) {
      handlers.set(event, handler)
    },
    async invoke(method, ...args) {
      invocations.push({ method, args })
      return invokeResults[method]
    },
    async start() {},
    async stop() {},
    onreconnected() {},
    push(event, ...args) {
      handlers.get(event)?.(...args)
    },
  }
}
