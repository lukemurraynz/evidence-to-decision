import {
  isArtifactEnvelope,
  isBoardClusterResult,
  isCardPinTallyArray,
  isCardVoteTallyArray,
  isDerivedCardArray,
  isDiscoveryCardSuggestionResult,
  isDurableOperation,
  isEngagement,
  isEngagementArray,
  isEvidenceQualityAssessment,
  isFrameDraftResult,
  isLiveBoardCardArray,
  isLiveIdeationNoteArray,
  isLiveSession,
  isOpportunityReview,
  OperationStatus,
  type ArtifactEnvelope,
  type ArtifactTypeValue,
  type BoardClusterCardInput,
  type BoardClusterResult,
  type BoardSnapshotItem,
  type CaptureEvidenceInput,
  type CardPinTally,
  type CardVoteTally,
  type CreateCardShortlistEntryInput,
  type CreateEngagementInput,
  type CreateJourneyMapInput,
  type CreateOpportunityInput,
  type CreatePersonaInput,
  type CreateProblemInput,
  type CreateWorkflowInput,
  type DerivedCard,
  type DiscoveryCardCandidateInput,
  type DiscoveryCardSuggestionResult,
  type DurableOperation,
  type Engagement,
  type EvidenceQualityAssessment,
  type FrameDraftResult,
  type LiveBoardCard,
  type LiveIdeationNote,
  type LiveSession,
  type OpportunityReview,
  type RecordDecisionInput,
  type UpdateEngagementDetailsInput,
  isRecord,
} from './contracts'
import type { RuntimeConfig } from '../config/runtimeConfig'

type ApiResponse<T> = {
  readonly data: T
  readonly etag: string | null
  readonly retryAfterMs: number | null
}

type ProblemDetails = {
  readonly type?: string
  readonly title?: string
  readonly detail?: string
  readonly status?: number
  readonly instance?: string
  readonly correlationId?: string
  readonly traceId?: string
}

type RequestOptions<T> = {
  readonly method?: 'GET' | 'POST'
  readonly body?: unknown
  readonly etag?: string
  readonly idempotencyKey?: string
  readonly signal?: AbortSignal
  readonly timeoutMs?: number
  readonly validate: (value: unknown) => value is T
}

export class ApiRequestError extends Error {
  public readonly status: number
  public readonly retryAfterMs: number | null
  public readonly referenceId: string | null

  public constructor(
    message: string,
    status: number,
    retryAfterMs: number | null,
    referenceId: string | null,
  ) {
    super(message)
    this.name = 'ApiRequestError'
    this.status = status
    this.retryAfterMs = retryAfterMs
    this.referenceId = referenceId
  }
}

export class ApiParseError extends Error {
  public constructor(message: string, options?: ErrorOptions) {
    super(message, options)
    this.name = 'ApiParseError'
  }
}

export class OperationPollingError extends Error {
  public constructor(message: string) {
    super(message)
    this.name = 'OperationPollingError'
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  if (!isRecord(value)) {
    return false
  }
  return (
    (value.type === undefined || typeof value.type === 'string') &&
    (value.title === undefined || typeof value.title === 'string') &&
    (value.detail === undefined || typeof value.detail === 'string') &&
    (value.status === undefined || typeof value.status === 'number') &&
    (value.instance === undefined || typeof value.instance === 'string') &&
    (value.correlationId === undefined ||
      typeof value.correlationId === 'string') &&
    (value.traceId === undefined || typeof value.traceId === 'string')
  )
}

function userSafeMessage(status: number, problem?: ProblemDetails): string {
  if (status === 409 || status === 412) {
    return 'This engagement changed after it was opened. Reload it and try again.'
  }
  if (status === 400 || status === 422) {
    return (
      problem?.detail ??
      problem?.title ??
      'Some information needs attention. Review your entries and try again.'
    )
  }
  if (status === 401) {
    return 'Your session has expired. Sign in again to continue.'
  }
  if (status === 403) {
    return 'You do not have access to this workspace. Contact an administrator.'
  }
  if (status === 404) {
    return 'This item does not exist or you do not have access.'
  }
  if (status === 429) {
    return 'Too many requests were made. Wait before trying again.'
  }
  if (status >= 500) {
    return 'The requested information is temporarily unavailable. Try again.'
  }
  return 'The request could not be completed. Review your entries and try again.'
}

export function parseRetryAfterMs(
  value: string | null,
  now = Date.now(),
): number | null {
  if (value === null || value.trim() === '') {
    return null
  }
  const seconds = Number(value)
  if (Number.isFinite(seconds) && seconds >= 0) {
    return Math.ceil(seconds * 1_000)
  }
  const date = Date.parse(value)
  return Number.isNaN(date) ? null : Math.max(0, date - now)
}

function createRequestId(): string {
  return crypto.randomUUID()
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text()
  if (text.trim() === '') {
    throw new ApiParseError('The service returned an empty response.')
  }
  try {
    return JSON.parse(text) as unknown
  } catch (error: unknown) {
    throw new ApiParseError('The service returned an unreadable response.', {
      cause: error,
    })
  }
}

async function readError(response: Response): Promise<ApiRequestError> {
  let problem: ProblemDetails | undefined
  try {
    const body: unknown = await response.json()
    if (isProblemDetails(body)) {
      problem = body
    }
  } catch {
    problem = undefined
  }

  const referenceId =
    response.headers.get('x-correlation-id') ??
    problem?.correlationId ??
    problem?.traceId ??
    null
  return new ApiRequestError(
    userSafeMessage(response.status, problem),
    response.status,
    parseRetryAfterMs(response.headers.get('retry-after')),
    referenceId,
  )
}

function createLinkedAbort(
  timeoutMs: number,
  outerSignal?: AbortSignal,
): {
  readonly signal: AbortSignal
  readonly dispose: () => void
} {
  const controller = new AbortController()
  const timeoutId = window.setTimeout(() => controller.abort(), timeoutMs)
  const abortFromOuter = (): void => controller.abort(outerSignal?.reason)
  if (outerSignal?.aborted === true) {
    controller.abort(outerSignal.reason)
  } else {
    outerSignal?.addEventListener('abort', abortFromOuter, { once: true })
  }

  return {
    signal: controller.signal,
    dispose: () => {
      window.clearTimeout(timeoutId)
      outerSignal?.removeEventListener('abort', abortFromOuter)
    },
  }
}

function workspacePath(workspaceId: string): string {
  return `/api/v1/workspaces/${encodeURIComponent(workspaceId)}`
}

export type AccessTokenProvider = () => Promise<string>

export class OpportunityApiClient {
  readonly #config: RuntimeConfig
  readonly #fetch: typeof fetch
  readonly #getAccessToken: AccessTokenProvider

  public constructor(
    config: RuntimeConfig,
    getAccessToken: AccessTokenProvider,
    fetchImplementation: typeof fetch = fetch.bind(globalThis),
  ) {
    this.#config = config
    this.#getAccessToken = getAccessToken
    this.#fetch = fetchImplementation
  }

  /** The API's connection base. Needed to open the collaboration hub connection directly. */
  public get apiBaseUrl(): string {
    return this.#config.apiBaseUrl
  }

  /** Exposes the same access token used for REST calls, for the SignalR hub's own auth. */
  public getAccessToken(): Promise<string> {
    return this.#getAccessToken()
  }

  async #request<T>(
    path: string,
    options: RequestOptions<T>,
  ): Promise<ApiResponse<T>> {
    const linkedAbort = createLinkedAbort(
      options.timeoutMs ?? this.#config.requestTimeoutMs,
      options.signal,
    )
    const accessToken = await this.#getAccessToken()
    const headers = new Headers({
      Accept: 'application/json',
      Authorization: `Bearer ${accessToken}`,
      'x-correlation-id': createRequestId(),
    })
    if (options.body !== undefined) {
      headers.set('Content-Type', 'application/json')
    }
    if (options.etag !== undefined) {
      headers.set('If-Match', options.etag)
    }
    if (options.idempotencyKey !== undefined) {
      headers.set('Idempotency-Key', options.idempotencyKey)
    }

    try {
      const response = await this.#fetch(new URL(path, this.#config.apiBaseUrl), {
        method: options.method ?? 'GET',
        headers,
        cache: 'no-store',
        signal: linkedAbort.signal,
        ...(options.body === undefined
          ? {}
          : { body: JSON.stringify(options.body) }),
      })
      if (!response.ok) {
        throw await readError(response)
      }
      const payload = await readJson(response)
      if (!options.validate(payload)) {
        throw new ApiParseError(
          'The service response did not match the expected contract.',
        )
      }
      return {
        data: payload,
        etag: response.headers.get('etag'),
        retryAfterMs: parseRetryAfterMs(response.headers.get('retry-after')),
      }
    } finally {
      linkedAbort.dispose()
    }
  }

  public getEngagement(
    workspaceId: string,
    engagementId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}`,
      { validate: isEngagement, ...(signal === undefined ? {} : { signal }) },
    )
  }

  public listEngagements(
    workspaceId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<readonly Engagement[]>> {
    return this.#request(`${workspacePath(workspaceId)}/engagements`, {
      validate: isEngagementArray,
      ...(signal === undefined ? {} : { signal }),
    })
  }

  public createEngagement(
    workspaceId: string,
    input: CreateEngagementInput,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(`${workspacePath(workspaceId)}/engagements`, {
      method: 'POST',
      body: input,
      validate: isEngagement,
      ...(signal === undefined ? {} : { signal }),
    })
  }

  public updateEngagementDetails(
    workspaceId: string,
    engagementId: string,
    input: UpdateEngagementDetailsInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/details`,
      {
        method: 'POST',
        body: input,
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public captureEvidence(
    workspaceId: string,
    engagementId: string,
    input: CaptureEvidenceInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/evidence`,
      {
        method: 'POST',
        body: input,
        etag,
        idempotencyKey: createRequestId(),
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public assessEvidenceQuality(
    workspaceId: string,
    engagementId: string,
    evidenceId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<EvidenceQualityAssessment>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/evidence/${encodeURIComponent(evidenceId)}/quality-assessment`,
      {
        method: 'POST',
        validate: isEvidenceQualityAssessment,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public draftFrame(
    workspaceId: string,
    engagementId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<FrameDraftResult>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/frame-draft`,
      {
        method: 'POST',
        validate: isFrameDraftResult,
        // Runs two sequential Foundry model calls (draft, then citation critique) rather than
        // one. The default requestTimeoutMs is sized for a single call and cuts this one off
        // mid-flight on an engagement with a large evidence set.
        timeoutMs: Math.max(this.#config.requestTimeoutMs * 2, 30_000),
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public addWorkflow(
    workspaceId: string,
    engagementId: string,
    input: CreateWorkflowInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/workflows`,
      {
        method: 'POST',
        body: input,
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public addProblem(
    workspaceId: string,
    engagementId: string,
    input: CreateProblemInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/problems`,
      {
        method: 'POST',
        body: input,
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public addPersona(
    workspaceId: string,
    engagementId: string,
    input: CreatePersonaInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/personas`,
      {
        method: 'POST',
        body: input,
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public addJourneyMap(
    workspaceId: string,
    engagementId: string,
    input: CreateJourneyMapInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/journey-maps`,
      {
        method: 'POST',
        body: input,
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public addCardShortlistEntry(
    workspaceId: string,
    engagementId: string,
    input: CreateCardShortlistEntryInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/card-shortlist`,
      {
        method: 'POST',
        body: input,
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public markCardShortlistSelection(
    workspaceId: string,
    engagementId: string,
    entryId: string,
    facilitatorSelected: boolean,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/card-shortlist/${encodeURIComponent(entryId)}/selection`,
      {
        method: 'POST',
        body: { facilitatorSelected },
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public startLiveSession(
    workspaceId: string,
    engagementId: string,
    journeyStepId: string | null,
    startPrivate = false,
    signal?: AbortSignal,
  ): Promise<ApiResponse<LiveSession>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions`,
      {
        method: 'POST',
        body: { journeyStepId, startPrivate },
        validate: isLiveSession,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  // An ideation round is engagement-wide rather than scoped to one journey step; see
  // LiveSession's journeyStepId doc comment.
  public startIdeationSession(
    workspaceId: string,
    engagementId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<LiveSession>> {
    return this.startLiveSession(workspaceId, engagementId, null, false, signal)
  }

  public suggestDiscoveryCards(
    workspaceId: string,
    engagementId: string,
    journeyStepId: string,
    candidates: readonly DiscoveryCardCandidateInput[],
    signal?: AbortSignal,
  ): Promise<ApiResponse<DiscoveryCardSuggestionResult>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/journey-steps/${encodeURIComponent(journeyStepId)}/discovery-card-suggestions`,
      {
        method: 'POST',
        body: { candidates },
        validate: isDiscoveryCardSuggestionResult,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  // Lets a screen that didn't itself start the session (the dedicated board route) find
  // whichever session Discovery Cards' "Start a live vote" already minted for this step,
  // rather than minting a second, disconnected room. Null means no session is currently
  // running for this step, not an error, just an honest "nothing to attach to yet."
  public async getActiveLiveSession(
    workspaceId: string,
    engagementId: string,
    journeyStepId: string,
    signal?: AbortSignal,
  ): Promise<LiveSession | null> {
    try {
      const result = await this.#request<LiveSession>(
        `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/active?journeyStepId=${encodeURIComponent(journeyStepId)}`,
        {
          method: 'GET',
          validate: isLiveSession,
          ...(signal === undefined ? {} : { signal }),
        },
      )
      return result.data
    } catch (error: unknown) {
      if (error instanceof ApiRequestError && error.status === 404) return null
      throw error
    }
  }

  public closeLiveSession(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<LiveSession>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/close`,
      {
        method: 'POST',
        validate: isLiveSession,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public revealBoard(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<LiveSession>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/reveal-board`,
      {
        method: 'POST',
        validate: isLiveSession,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  // Starts a new private round on a session that was previously revealed. Only placements from
  // this point forward are hidden from other participants; see LiveSessionService's doc comment
  // for why already-visible cards don't retroactively disappear.
  public setBoardPrivate(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<LiveSession>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/private-board`,
      {
        method: 'POST',
        validate: isLiveSession,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public clearBoard(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<readonly LiveBoardCard[]>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/board/clear`,
      {
        method: 'POST',
        validate: isLiveBoardCardArray,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public snapshotBoard(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    items: readonly BoardSnapshotItem[],
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/board/snapshot`,
      {
        method: 'POST',
        body: { items },
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public suggestBoardClusters(
    workspaceId: string,
    engagementId: string,
    cards: readonly BoardClusterCardInput[],
    signal?: AbortSignal,
  ): Promise<ApiResponse<BoardClusterResult>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/board/cluster-suggestions`,
      {
        method: 'POST',
        body: { cards },
        validate: isBoardClusterResult,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public getLiveSessionTally(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<readonly CardVoteTally[]>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/tally`,
      {
        validate: isCardVoteTallyArray,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public promoteLiveVote(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    input: { readonly discoveryCardId: string; readonly journeyStepId: string; readonly rationale: string; readonly rank: number },
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/promote`,
      {
        method: 'POST',
        body: input,
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public getIdeationNotes(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<readonly LiveIdeationNote[]>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/ideation-notes`,
      {
        validate: isLiveIdeationNoteArray,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public curateIdeationNote(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    noteId: string,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/ideation-notes/curate`,
      {
        method: 'POST',
        body: { noteId },
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public getLivePinTally(
    workspaceId: string,
    engagementId: string,
    sessionId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<readonly CardPinTally[]>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/live-sessions/${encodeURIComponent(sessionId)}/pins`,
      {
        validate: isCardPinTallyArray,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public addOpportunity(
    workspaceId: string,
    engagementId: string,
    input: CreateOpportunityInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/opportunities`,
      {
        method: 'POST',
        body: input,
        etag,
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public getCards(
    workspaceId: string,
    engagementId: string,
    filter?: { readonly type?: string | undefined; readonly search?: string | undefined },
    signal?: AbortSignal,
  ): Promise<ApiResponse<readonly DerivedCard[]>> {
    const query = new URLSearchParams()
    if (filter?.type) query.set('type', filter.type)
    if (filter?.search) query.set('search', filter.search)
    const queryString = query.toString()
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/cards${queryString ? `?${queryString}` : ''}`,
      {
        validate: isDerivedCardArray,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public getOpportunityReview(
    workspaceId: string,
    engagementId: string,
    opportunityId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<OpportunityReview>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/opportunities/${encodeURIComponent(opportunityId)}/review`,
      {
        validate: isOpportunityReview,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public recordDecision(
    workspaceId: string,
    engagementId: string,
    input: RecordDecisionInput,
    etag: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<Engagement>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/decisions`,
      {
        method: 'POST',
        body: input,
        etag,
        idempotencyKey: createRequestId(),
        validate: isEngagement,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public requestRecommendation(
    workspaceId: string,
    engagementId: string,
    opportunityId: string,
    idempotencyKey: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<DurableOperation>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/recommendations`,
      {
        method: 'POST',
        body: { opportunityId },
        idempotencyKey,
        validate: isDurableOperation,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public getOperation(
    workspaceId: string,
    operationId: string,
    signal?: AbortSignal,
  ): Promise<ApiResponse<DurableOperation>> {
    return this.#request(
      `${workspacePath(workspaceId)}/operations/${encodeURIComponent(operationId)}`,
      {
        validate: isDurableOperation,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }

  public async pollOperation(
    workspaceId: string,
    operationId: string,
    options?: {
      readonly signal?: AbortSignal
      readonly onProgress?: (operation: DurableOperation) => void
      readonly sleep?: (milliseconds: number, signal?: AbortSignal) => Promise<void>
    },
  ): Promise<DurableOperation> {
    const startedAt = Date.now()
    const sleep = options?.sleep ?? wait

    for (let attempt = 0; attempt < this.#config.pollMaxAttempts; attempt += 1) {
      const response = await this.getOperation(
        workspaceId,
        operationId,
        options?.signal,
      )
      options?.onProgress?.(response.data)
      if (
        response.data.status !== OperationStatus.Queued &&
        response.data.status !== OperationStatus.Running
      ) {
        return response.data
      }
      if (Date.now() - startedAt >= this.#config.pollMaxElapsedMs) {
        break
      }
      const requestedDelay =
        response.retryAfterMs ?? response.data.retryAfterSeconds * 1_000
      const remainingBudgetMs = this.#config.pollMaxElapsedMs - (Date.now() - startedAt)
      const boundedDelay = Math.min(Math.max(requestedDelay, 500), 30_000, remainingBudgetMs)
      await sleep(boundedDelay, options?.signal)
    }

    throw new OperationPollingError(
      'Progress checks reached their limit. Return later to check the result.',
    )
  }

  public generateArtifact(
    workspaceId: string,
    engagementId: string,
    opportunityId: string,
    artifactType: ArtifactTypeValue,
    signal?: AbortSignal,
  ): Promise<ApiResponse<ArtifactEnvelope>> {
    return this.#request(
      `${workspacePath(workspaceId)}/engagements/${encodeURIComponent(engagementId)}/artifacts`,
      {
        method: 'POST',
        body: { opportunityId, artifactType },
        validate: isArtifactEnvelope,
        ...(signal === undefined ? {} : { signal }),
      },
    )
  }
}

function wait(milliseconds: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted === true) {
      reject(signal.reason)
      return
    }
    const finish = (): void => {
      signal?.removeEventListener('abort', abort)
      resolve()
    }
    const timer = window.setTimeout(finish, milliseconds)
    const abort = (): void => {
      window.clearTimeout(timer)
      signal?.removeEventListener('abort', abort)
      reject(signal?.reason)
    }
    signal?.addEventListener('abort', abort, { once: true })
  })
}
