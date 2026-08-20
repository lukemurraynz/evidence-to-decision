# Contract: Opportunity Recommendation

## Response

- `recommendationId`
- `candidateReferences`
- `fitDimensions`: explicit dimensions and explanations
- `evidenceReferences`
- `unknowns`
- `limitations`
- `confidenceStatus`: supported | limited | abstain | human_review_required
- `requiredReview`
- `canonicalGraphVersion`
- `correlationId`

## Rules

- Recommendations are advisory and do not mutate lifecycle or decision state.
- Every candidate must expose why it fits and why it may not fit.
- Stale, contradictory, incomplete, or unauthorized context must lower confidence or produce abstention.
- A human domain action is required for consequential lifecycle changes.
- The response must not expose evidence outside the caller's workspace authorization boundary.
