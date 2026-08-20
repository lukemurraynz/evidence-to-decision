# Product context

## Purpose

Evidence to Decision is an enterprise opportunity review interface. It connects
workshop evidence to reviewable opportunities, accountable decisions, executive
outcomes, and delivery documents. The interface uses business language while the
API remains the authoritative record.

## People and work

- Workshop facilitators capture attributed evidence, frame opportunities, and
  run live in-workshop card voting with the room.
- Workshop participants join a live session anonymously via a 6-character
  code or link (no account, no sign-in) to vote on Discovery Cards
  alongside the facilitator in real time.
- Decision reviewers evaluate trust and readiness, identify blockers, and record
  decisions.
- Executives review outcomes, confidence, readiness, and open blockers.
- Delivery leads create reproducible documents from approved records.

## Main journeys

| Route | User goal |
|---|---|
| `#/` | Choose a responsibility, connect an engagement, or start a new workshop |
| `#/discover` | Capture and review the evidence trail |
| `#/discovery-cards` | Browse AI capability cards, shortlist candidates for a journey step, and run a live vote with the room |
| `#/journey-map` | Capture personas and journey steps |
| `#/frame` | Frame problems and opportunities |
| `#/cards` | Browse derived opportunity/problem cards |
| `#/review` | Assess an opportunity and record a decision |
| `#/outcomes` | Review portfolio outcomes and readiness |
| `#/handoff` | Generate a delivery document |
| `#/progress/{operationId}` | Follow review brief preparation |
| `#/join/{joinCode}` | Anonymous participant entry point: join a live vote session by code, no sign-in |

Workspace and engagement references use the page query string:
`?workspace=<workspace-reference>&engagement=<engagement-reference>`. The
application does not store them in browser storage. A new engagement can be
minted directly from `#/` ("Start a new workshop") rather than only opened by
reference. Objectives and participants can only be set at that point, since
no later mutation updates them.

## Live workshop sessions

A facilitator starts a live vote scoped to a journey step's shortlist (not
the full card catalog: voting is the room deciding among candidates already
narrowed down, not re-running the card exploration live). Participants join
over a second, additive auth scheme (`Participant`, HMAC-signed short-lived
JWT, issued by `POST /api/v1/join/{joinCode}`) that never reaches Entra
sign-in and structurally cannot touch the canonical graph. The facilitator's
explicit "promote" action is the only path from a live tally into a durable
`CardShortlistEntry`. Real-time updates (vote tallies, shortlist changes)
flow over an Azure SignalR-backed hub (`/hubs/collaboration`).

## Runtime configuration

The application requests `/config.json` before opening API-dependent views.
When the file returns `404`, the application visibly selects the page origin as
the API origin. Other configuration failures stop API-dependent rendering and
show a setup recovery action.

Supported non-secret values:

```json
{
  "apiBaseUrl": "https://workshop.example/",
  "requestTimeoutMs": 15000,
  "pollMaxAttempts": 20,
  "pollMaxElapsedMs": 180000
}
```

The client validates configuration and API payloads at runtime. It applies
timeouts and cancellation, checks ETags for record changes, assigns
idempotency keys to side-effecting requests, parses RFC 9457 Problem Details,
and bounds operation polling while respecting `Retry-After`.

## Security and offline policy

- No access token, refresh token, API key, evidence record, or decision record is
  written to `localStorage`, `sessionStorage`, IndexedDB, or Cache Storage.
- Authentication remains the responsibility of the approved hosting and
  identity boundary. The frontend includes browser-managed credentials but does
  not acquire or persist bearer tokens.
- The production service worker caches the application shell and static assets
  after a successful visit. It never caches `/api/` responses or `/config.json`.
- An evidence draft remains in memory while the application is open. The
  interface warns before a page unload and blocks saving while offline.
- Offline mode keeps navigation and setup guidance available. Server records
  remain unavailable until a real request succeeds.

## Failure behavior

Expected empty records receive guidance rather than error treatment. Connection,
configuration, authorization, parsing, and service failures remain distinct.
Unauthorized, forbidden, missing, and invalid requests do not offer a retry that
cannot help. Rate-limited and long-running requests respect server timing.
