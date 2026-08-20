# Deployment runbook

## Approval boundary

Azure provisioning, RBAC changes, Entra changes, model deployment, and
destructive teardown require explicit human approval. Do not apply this runbook
to production without a reviewed preview and confirmed target.

## Pre-deployment gate

1. Record the intended tenant, subscription, environment, and resource group.
2. Confirm the approved data classification and Australia East residency.
3. Validate that the Entra application and three role-group object IDs exist.
4. Confirm the existing Foundry account, project, model deployment, and model
   identity.
5. Keep Fabric disabled unless capacity, tenant AI settings, workspace identity,
   OneLake security, audit, dataset approval, and query-mode evidence all pass.
6. Acquire the environment-scoped deployment lock.
7. Run `azd provision --preview` and save the reviewable output.
8. Review all resource, role-assignment, ingress, deletion, and replacement
   changes before approval.

## Ordered deployment

After approval:

1. Provision infrastructure and managed-identity role assignments.
2. Verify Cosmos DB uses `/workspaceId`, session consistency, continuous backup,
   local authentication disabled, and one Australia East write region.
3. Verify Service Bus sessions, duplicate detection, dead-lettering, and the
   review subscription.
4. Validate Foundry managed-identity access and the locked agent/model
   configuration.
5. Deploy the immutable container image.
6. Verify HTTPS ingress, Entra authentication, `/health/live`, and one denied
   cross-workspace request.
7. Execute the specification quickstart and retain correlation IDs and audit
   evidence.

Release the deployment lock only after validation finishes or rollback is
complete.

## Rollback

Container Apps uses single-revision traffic. Roll back to the last reviewed
immutable image digest, then re-run health, authentication, workspace isolation,
and operation-status checks. Do not roll canonical data backward to match an
application revision. Use the restore runbook only for confirmed data recovery.

## Destructive teardown

Confirm the tenant, subscription, resource group, and `AZURE_ENV_NAME`, then run:

```bash
scripts/azd-down-safe.sh <environment-name>
```

The script requires the exact phrase `DELETE <environment-name>` before it
invokes `azd down --purge`. Purge is irreversible and requires separate human
approval.
