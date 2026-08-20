# Contract: Local Azure Developer CLI Deployment

## Scope

Azure Developer CLI (`azd`) is the local deployment mechanism for the first implementation. CI/CD pipelines, GitHub Actions, Azure DevOps pipelines, and automated promotion are explicitly out of scope at this stage.

The deployment must be repeatable, reviewable, and ordered. `azd` orchestrates the application and infrastructure deployment; it does not replace Entra governance or the canonical graph's authorization rules.

## Deployment layers

The local deployment flow must cover the complete dependency chain:

1. **Identity prerequisites**
   - Validate the target tenant and subscription context.
   - Create or validate the Entra application registration and API audience/scope configuration.
   - Create or validate three dedicated external security groups for facilitator, reviewer, and admin.
   - Assign immutable group object IDs through configuration, never display names or user identity comparisons.
   - Configure explicit workspace membership mappings separately from role groups.
   - Apply least-privilege permissions and record auditable administrative changes.

2. **Infrastructure**
   - Deploy the Australia East, single-region, zone-redundant Container Apps environment.
   - Deploy Cosmos DB for NoSQL with workspace partitioning, session consistency, continuous backup, and 90-day initial retention.
   - Deploy Service Bus with one topic and one subscription per downstream consumer, 50 events/second initial target, per-workspace ordering, 30-day replay retention, and dead-letter handling.
   - Provide the managed identities, role assignments, configuration stores, diagnostics, and secret references required by the API and agents.
   - Keep development/test data and production data isolated even though only one production environment is initially deployed.

3. **Agents**
   - Validate or provision the Microsoft Foundry project/model connections required by Microsoft Agent Framework.
   - Configure agent instructions, tool permissions, structured outputs, multimodal boundaries, retention/redaction policy, and human approval gates.
   - Ensure agents can recommend or prepare work but cannot make consequential domain decisions or bypass workspace authorization.
   - Store agent identifiers and model/deployment configuration as environment configuration, not hard-coded identity dispatch.

4. **Applications**
   - Build and deploy the ASP.NET Core/.NET 10 API to Azure Container Apps.
   - Inject configuration at deployment time using managed identity and approved secret references.
   - Configure controlled public HTTPS ingress with Entra authentication.
   - Configure health checks, correlation IDs, audit logging, readiness checks, and asynchronous job/event processing.
   - Validate the US1 path: multimodal evidence capture, provenance, conflict preservation, explainable recommendation, and human decision recording.

5. **Validation and handoff**
   - Run a preview/diff or deployment plan before writes.
   - Validate identity, infrastructure, agent, and application health separately and end to end.
   - Verify API p95, asynchronous completion, event throughput, queue-depth guardrails, workspace isolation, and human approval gates.
   - Record deployment outputs, resource identifiers, configuration versions, and validation results for the implementation change record.

## Configuration boundaries

- `azd` environment values hold non-secret deployment configuration and references.
- Secrets and credentials must not be committed, logged, or embedded in `azure.yaml`, Bicep, application settings, or agent instructions.
- Entra group object IDs, workspace mappings, model/deployment identifiers, and resource names are configuration inputs and must be validated at startup or deployment validation time.
- Application and agent code must use managed identity or approved workload identity for Azure resource access.

## Deployment lifecycle

The first lifecycle is local and operator-controlled:

`azd provision` -> identity/configuration validation -> `azd deploy` -> health and scenario validation -> documented teardown/rollback plan

No automated pipeline, merge-triggered deployment, environment promotion, or production write is implied by this contract.

## Operational safety controls

- Enforce one active deployment per environment at a time using an environment-scoped concurrency lock.
- Before any destructive teardown, verify tenant, subscription, resource group, and environment identifiers match the intended target.
- Use `azd down --purge` only with explicit typed confirmation from the operator; treat purge as irreversible.
- Failed validation in any layer (identity, infra, agent, app) blocks progression to the next layer.

## Asynchronous operation contract validation

- Deployment validation must confirm asynchronous operation endpoints return operation identifiers, status transitions, and terminal-state outcomes.
- Validation evidence must include correlation from request submission through terminal state for at least one agentic and one multimodal workflow.
- Retry and timeout behavior for asynchronous status polling must be exercised and recorded during deployment validation.

## Required evidence

- Preview or plan output before provisioning/deployment.
- Entra group and app-registration validation result.
- Infrastructure resource and role-assignment validation result.
- Agent configuration and tool-permission validation result.
- Application health and US1 scenario validation result.
- Evidence of workspace isolation, audit logging, retention, and rollback/restore readiness.
- Concurrency-lock evidence showing single active deployment per target environment.
- Purge guard evidence showing typed confirmation and environment/subscription verification steps.
- Asynchronous operation-contract validation evidence (operation ID, status endpoint, terminal-state trace).
