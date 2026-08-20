# Evidence to Decision

An AI opportunity engineering system for evidence-first, human-approved AI
opportunity discovery. A facilitator captures what people actually said in a
workshop, the room votes and clusters live on a shared mural, an Azure AI
Foundry agent drafts a starting problem framing from that evidence and
nothing else, and a reviewer approves or blocks it before it becomes a
delivery document.

This repository implements a workspace-isolated opportunity engineering API
on .NET 11 and a React/TypeScript single-page frontend. The canonical
Opportunity Graph is the only authoritative engagement state. Recommendations,
cards, review views, handoff artifacts, events, and portfolio analytics are
derived and cannot approve or mutate consequential state. An AI agent can
draft and recommend, but it can never approve or mutate canonical state on
its own.

The implementation is not production-ready until the live Azure, Entra,
Foundry, restore, load, and optional Fabric validation gates in the
[implementation record](docs/implementation-record.md) pass.

## Repository structure

- `src/OpportunityEngineering.Domain`: canonical graph and invariants
- `src/OpportunityEngineering.Application`: commands, queries, gates, and ports
- `src/OpportunityEngineering.Infrastructure`: Cosmos DB, Service Bus, policy,
  Foundry, and Fabric governance adapters
- `src/OpportunityEngineering.Api`: authenticated API and asynchronous workers
- `frontend`: the React/TypeScript SPA; see [frontend/README.md](frontend/README.md)
  for setup and [frontend/PRODUCT.md](frontend/PRODUCT.md) for user journeys
- `tests`: deterministic domain, application, authorization, policy, replay,
  and governance tests
- `infra`: Azure Container Apps + Static Web Apps deployment (`azd up`), see
  [azure.yaml](azure.yaml)
- `scripts`: operator safety controls
- `docs/operations`: restore, replay, and deployment runbooks
- `specs/001-production-ai-opportunity-engineering-system`: the original
  spec-kit feature spec, plan, and data model this system was built from
- `ai-envisioning-workshop-system-assets`: source and normalized data for the
  Discovery Cards catalog the frontend ships (`frontend/src/data/discoveryCards.ts`)

## Quickstart

To run the full stack locally, start the API (below), then in a second
terminal follow [frontend/README.md](frontend/README.md) (`npm install &&
npm run dev`). To deploy both services to Azure with managed identity
end-to-end, see [azure.yaml](azure.yaml) and run `azd up`.

## Build and test

Prerequisites:

- .NET SDK 11.0 preview (11.0.100-preview.7.26381.103, matches `global.json`)
- Azure CLI with Bicep for infrastructure validation
- Docker with BuildKit for container validation

```bash
dotnet restore OpportunityEngineering.slnx --locked-mode
dotnet build OpportunityEngineering.slnx --configuration Release --no-restore
dotnet test --solution OpportunityEngineering.slnx \
  --configuration Release \
  --no-build \
  --minimum-expected-tests 22
az bicep build --file infra/main.bicep --stdout > /tmp/opportunity-engineering.json
docker build --tag opportunity-engineering:validation .
```

## Runtime configuration

The API fails startup when required authorization or guardrail configuration is
missing or invalid. Deployment configuration must provide:

- `EntraID__TenantID` and `EntraID__ClientID`
- `WorkspaceAuthorizationJson`, containing workspace IDs and immutable
  facilitator, reviewer, and admin group object IDs
- `Cosmos__AccountEndpoint`, `Cosmos__DatabaseName`, and
  `Cosmos__ContainerName`
- `ServiceBus__FullyQualifiedNamespace`, topic, subscription, and queue names
- `Foundry__ProjectEndpoint`, model deployment name, and reproducible model
  identity
- `Guardrails__Mode`, either `evaluation-only` or `enforce`
- optional `FabricJson`; the readiness gate remains blocked until every
  prerequisite and approved derived dataset is configured

Azure access uses a user-assigned managed identity. Do not configure account
keys, connection strings, client secrets, or display-name-based group mappings.

## Deployment safety

Review the [deployment runbook](docs/operations/deployment.md) before any Azure
write. Infrastructure authoring and local validation do not authorize a
production deployment. Destructive teardown must use
`scripts/azd-down-safe.sh`, which requires an environment lock and exact typed
confirmation.
