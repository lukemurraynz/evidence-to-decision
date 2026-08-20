# Implementation record

## Implemented

- .NET 11 layered solution with an authoritative, versioned Opportunity Graph
- immutable evidence wording, revisions, provenance, multimodal confidence, and
  conflict preservation
- workspace-scoped Entra group authorization with facilitator, reviewer, and
  admin boundaries
- trust/readiness blockers and human-only consequential decisions
- Cosmos DB transactional graph, event, and audit writes with ETag concurrency
- durable idempotent operations, Service Bus sessions, retries, dead-lettering,
  and replay-safe consumers
- fail-closed policy bundle loading and append-only policy decision audit
- typed Microsoft Agent Framework recommendations over authorized canonical
  context
- derived cards, reviews, handoff artifacts, staleness, and portfolio analytics
- Fabric prerequisite and Direct Lake query-mode readiness gate
- Container Apps, Cosmos DB, Service Bus, managed identity, diagnostics, RBAC,
  health probes, deployment safety, restore, and replay assets
- React/TypeScript frontend (evidence capture, live SignalR mural, Frame
  drafting, Decision Review, Outcomes, delivery documents) deployed to Azure
  Static Web Apps alongside the API

## Validation evidence

| Check | Result | Evidence |
| --- | --- | --- |
| Domain and application tests | Passed | 118 passed, 0 failed, 0 skipped (`OpportunityEngineering.UnitTests`) |
| Authorization, policy, and hub/collaboration tests | Passed | 44 passed, 0 failed, 0 skipped (`OpportunityEngineering.ApiTests`); one unrelated, pre-existing flake in `EndpointRouteTableBuildsWithoutError` (Azure ServiceBus processor teardown timeout during `WebApplicationFactory` disposal, not a functional regression) |
| Release solution build | Passed | 0 warnings, 0 errors |
| Bicep compilation | Passed | `az bicep build --stdout` |
| Bash syntax | Passed | `bash -n scripts/azd-down-safe.sh` |
| Container build | Passed | pinned .NET 11 SDK and ASP.NET runtime images |
| NuGet audit | Passed | restore rejected vulnerable OpenAPI 2.0.0; direct pin to patched 2.7.5 restored without audit warnings |
| Frontend build, typecheck, and test suite | Passed | `npm run typecheck`, `npm test` (28 passed, 0 failed) |
| Azure deployment | Passed | `azd up` deployed Container Apps API + Static Web Apps frontend to a live Australia East environment |
| Entra group and app validation | Passed | facilitator and reviewer sign-in verified against a real Entra tenant end-to-end |
| Foundry live recommendation | Passed | live Frame Draft → Frame Critique Agent Framework Workflow verified against Application Insights `gen_ai.*` traces on a real deployed engagement |
| Cosmos restore drill | Not run | requires provisioned account and approved recovery exercise |
| Load and SLO validation | Not run | requires deployed environment and representative workload |
| Fabric/Data Agent live checks | Not run | optional capacity and tenant prerequisites were not supplied |
| Direct Lake fallback observation | Not run | optional Fabric semantic model was not supplied |

## Security controls

- ASI01: model output is typed, validated, advisory, and cannot mutate canonical
  state.
- ASI03: allowed evaluation points and tools are explicit; policy failures deny.
- ASI06: identity, workspace, role, tool, and human approval boundaries are
  enforced independently.
- ASI08: immutable evidence, graph versions, model identity, events, and durable
  audits preserve traceability.
- ASI09: per-workspace sessions, bounded retries, dead-lettering, idempotency
  claims, and canonical rereads constrain cascading failures.

## Remaining release gates

The system must not be described as production-ready until all not-run live
checks above pass with retained evidence. Data classification is unknown and
blocks final retention, Fabric aggregation, and production governance approval.
