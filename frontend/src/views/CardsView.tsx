import { useEffect, useState } from 'react'
import { ApiRequestError, type OpportunityApiClient } from '../api/client'
import { StalenessStatus, type DerivedCard, type Engagement } from '../api/contracts'
import { EmptyState, PageLoading } from '../components/AsyncStates'
import { navigateTo } from '../app/routing'

type CardsState =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly cards: readonly DerivedCard[] }
  | { readonly status: 'error'; readonly message: string; readonly retryable: boolean }

type CardsViewProps = {
  readonly client: OpportunityApiClient
  readonly workspaceId: string
  readonly engagement: Engagement
}

function stalenessLabel(value: DerivedCard['staleness']): string {
  if (value === StalenessStatus.Stale) return 'Stale'
  if (value === StalenessStatus.Unavailable) return 'Unavailable'
  return 'Current'
}

function CardIcon({ type }: { readonly type: string }) {
  if (type === 'opportunity') {
    return (
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="12" cy="12" r="8" stroke="currentColor" strokeWidth="2" />
        <circle cx="12" cy="12" r="4" stroke="currentColor" strokeWidth="2" />
        <circle cx="12" cy="12" r="1" fill="currentColor" />
      </svg>
    )
  }
  if (type === 'problem') {
    return (
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M12 3 3 20h18L12 3Z"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinejoin="round"
        />
        <path d="M12 10v4" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        <circle cx="12" cy="17" r="0.9" fill="currentColor" />
      </svg>
    )
  }
  return (
    <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <rect x="4" y="4" width="16" height="16" rx="3" stroke="currentColor" strokeWidth="2" />
    </svg>
  )
}

export function CardsView({ client, workspaceId, engagement }: CardsViewProps) {
  const [type, setType] = useState('')
  const [search, setSearch] = useState('')
  const [attempt, setAttempt] = useState(0)
  const [state, setState] = useState<CardsState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()
    const timeout = window.setTimeout(() => {
      setState({ status: 'loading' })
      void client
        .getCards(
          workspaceId,
          engagement.id,
          { type: type || undefined, search: search || undefined },
          controller.signal,
        )
        .then((result) => {
          setState({ status: 'ready', cards: result.data })
        })
        .catch((error: unknown) => {
          if (controller.signal.aborted) return
          setState({
            status: 'error',
            message:
              error instanceof ApiRequestError
                ? error.message
                : 'Cards could not be loaded. Check the connection and try again.',
            retryable:
              !(error instanceof ApiRequestError) ||
              ![401, 403, 404].includes(error.status),
          })
        })
    }, 250)
    return () => {
      window.clearTimeout(timeout)
      controller.abort()
    }
  }, [client, workspaceId, engagement.id, engagement.objectVersion, type, search, attempt])

  return (
    <section className="page cards-page">
      <header className="page-header">
        <div>
          <p className="eyebrow">Derived cards</p>
          <h1>Browse what the record has produced</h1>
          <p>
            Every problem and opportunity framed in this engagement appears here as
            a card, kept current with the record it was derived from.
          </p>
        </div>
        <div className="cards-filters">
          <div>
            <label htmlFor="cards-type">Type</label>
            <select id="cards-type" value={type} onChange={(event) => setType(event.target.value)}>
              <option value="">All types</option>
              <option value="opportunity">Opportunity</option>
              <option value="problem">Problem</option>
            </select>
          </div>
          <div>
            <label htmlFor="cards-search">Search</label>
            <input
              id="cards-search"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search title or description"
            />
          </div>
        </div>
      </header>

      {state.status === 'loading' && <PageLoading label="Loading cards" />}
      {state.status === 'error' && (
        <div className="inline-error" role="alert">
          <h2>Cards unavailable</h2>
          <p>{state.message}</p>
          {state.retryable && (
            <button type="button" onClick={() => setAttempt((current) => current + 1)}>
              Try again
            </button>
          )}
        </div>
      )}
      {state.status === 'ready' && state.cards.length === 0 && (
        <EmptyState
          title="No cards match"
          message="Frame a workflow, problem, or opportunity, or adjust the filters above."
        />
      )}
      {state.status === 'ready' && state.cards.length > 0 && (
        <ul className="card-list">
          {state.cards.map((card) => (
            <li key={card.id} className={`is-${stalenessLabel(card.staleness).toLowerCase()}`}>
              <div className="card-list-heading">
                <span className="card-type-badge">{card.type}</span>
                <span className="card-staleness">{stalenessLabel(card.staleness)}</span>
              </div>
              <div className="card-medallion">
                <CardIcon type={card.type} />
              </div>
              <div>
                <h3>{card.title}</h3>
                {card.tags.length > 0 && (
                  <ul className="card-tags">
                    {card.tags.map((tag) => (
                      <li key={tag}>{tag}</li>
                    ))}
                  </ul>
                )}
              </div>
              <p className="card-highlight">{card.description}</p>
              <dl className="card-stats">
                <div>
                  <dt>Derived from</dt>
                  <dd>v{card.derivedFromVersion}</dd>
                </div>
                <div>
                  <dt>Current record</dt>
                  <dd>v{card.currentCanonicalGraphVersion}</dd>
                </div>
              </dl>
              <div className="card-list-footer">
                <span>Ref {card.derivedFromId}</span>
                {card.type === 'opportunity' && (
                  <button
                    type="button"
                    className="button-secondary"
                    onClick={() => navigateTo('/review')}
                  >
                    Open in decision review
                  </button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
