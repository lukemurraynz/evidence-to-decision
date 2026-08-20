import { ApiParseError, ApiRequestError } from './client'
import { isJoinLiveSessionResponse, isRecord, type JoinLiveSessionResponse } from './contracts'

/**
 * Redeems a workshop join code for a short-lived participant token. Deliberately separate
 * from OpportunityApiClient: that client is shaped end-to-end around an Entra
 * AccessTokenProvider, and a participant never signs in.
 */
export async function redeemJoinCode(
  apiBaseUrl: string,
  joinCode: string,
  displayName: string,
  signal?: AbortSignal,
): Promise<JoinLiveSessionResponse> {
  const response = await fetch(
    new URL(`/api/v1/join/${encodeURIComponent(joinCode)}`, apiBaseUrl),
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({ displayName }),
      cache: 'no-store',
      ...(signal === undefined ? {} : { signal }),
    },
  )

  if (!response.ok) {
    if (response.status === 429) {
      throw new ApiRequestError(
        'Too many join attempts from this connection. Wait a minute and try again.',
        response.status,
        null,
        null,
      )
    }
    let detail: string | undefined
    try {
      const problem: unknown = await response.json()
      detail = isRecord(problem) && typeof problem.detail === 'string' ? problem.detail : undefined
    } catch {
      detail = undefined
    }
    throw new ApiRequestError(
      detail ?? 'That join code could not be used. Check it and try again.',
      response.status,
      null,
      null,
    )
  }

  const body: unknown = await response.json()
  if (!isJoinLiveSessionResponse(body)) {
    throw new ApiParseError('The join response did not match the expected contract.')
  }

  return body
}
