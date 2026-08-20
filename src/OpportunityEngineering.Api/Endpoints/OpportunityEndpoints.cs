using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Api.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;
using OpportunityEngineering.Infrastructure.Fabric;

namespace OpportunityEngineering.Api.Endpoints;

internal static class OpportunityEndpoints
{
    public static RouteGroupBuilder MapOpportunityEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/engagements/{engagementId}/opportunities", async (
            string workspaceId,
            string engagementId,
            AddOpportunityRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.AddOpportunityAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                new Opportunity
                {
                    Id = request.Id,
                    ProblemId = request.ProblemId,
                    WorkflowId = request.WorkflowId,
                    DesiredOutcome = request.DesiredOutcome,
                    KpiReference = request.KpiReference,
                    Owner = request.Owner,
                    ValueProfile = request.ValueProfile,
                    ConfidenceProfile = request.ConfidenceProfile,
                    TrustProfile = request.TrustProfile,
                    ReadinessProfile = request.ReadinessProfile,
                    EvidenceReferences = request.EvidenceReferences ?? [],
                    Concepts = request.Concepts ?? [],
                    Assumptions = request.Assumptions ?? []
                },
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/recommendations", async (
            string workspaceId,
            string engagementId,
            RecommendationRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            RecommendationSubmissionService recommendations,
            CancellationToken cancellationToken) =>
        {
            var idempotencyKey = context.Request.Headers["Idempotency-Key"].ToString();
            var operation = await recommendations.SubmitAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                request.OpportunityId,
                idempotencyKey,
                $"{engagementId}\n{request.OpportunityId}",
                cancellationToken);
            return Results.Accepted(
                $"/api/v1/workspaces/{workspaceId}/operations/{operation.Id}",
                operation);
        });

        api.MapGet("/operations/{operationId}", async (
            string workspaceId,
            string operationId,
            HttpContext context,
            WorkspaceActorResolver actors,
            IDurableOperationStore operations,
            CancellationToken cancellationToken) =>
        {
            _ = ApiEndpointHelpers.Actor(context, workspaceId, actors);
            var operation = await operations.GetAsync(workspaceId, operationId, cancellationToken)
                ?? throw new DomainException("operation.not_found", "Operation was not found.");
            if (operation.Status is OperationStatus.Queued or OperationStatus.Running)
            {
                context.Response.Headers.RetryAfter = operation.RetryAfterSeconds.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            return Results.Ok(operation);
        });

        api.MapGet("/engagements/{engagementId}/opportunities/{opportunityId}/review", async (
            string workspaceId,
            string engagementId,
            string opportunityId,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphQueryService queries,
            CancellationToken cancellationToken) =>
            Results.Ok(await queries.GetReviewAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                opportunityId,
                cancellationToken)));

        api.MapGet("/review-notifications", async (
            string workspaceId,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphQueryService queries,
            CancellationToken cancellationToken,
            int pageSize = 50,
            string? continuationToken = null) =>
        {
            return Results.Ok(await queries.GetReviewerNotificationsAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                pageSize,
                continuationToken,
                cancellationToken));
        });

        api.MapPost("/engagements/{engagementId}/opportunities/{opportunityId}/gate-evaluations", async (
            string workspaceId,
            string engagementId,
            string opportunityId,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            Results.Ok(await commands.EvaluateGatesAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                opportunityId,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/decisions", async (
            string workspaceId,
            string engagementId,
            DecisionRecord decision,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.RecordDecisionAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                decision,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/artifacts", async (
            string workspaceId,
            string engagementId,
            GenerateArtifactRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphQueryService queries,
            CancellationToken cancellationToken) =>
            Results.Ok(await queries.GenerateArtifactAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                request.OpportunityId,
                request.ArtifactType,
                cancellationToken)));

        api.MapGet("/artifacts/{artifactId}", async (
            string workspaceId,
            string artifactId,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphQueryService queries,
            CancellationToken cancellationToken) =>
            Results.Ok(await queries.GetArtifactAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                artifactId,
                cancellationToken)));

        api.MapPost("/analytics/projections", async (
            string workspaceId,
            AnalyticsRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphQueryService queries,
            CancellationToken cancellationToken) =>
            Results.Ok(await queries.GenerateWorkspaceAnalyticsAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                request.WindowStart,
                request.WindowEnd,
                cancellationToken)));

        api.MapGet("/fabric/readiness", (
            string workspaceId,
            HttpContext context,
            WorkspaceActorResolver actors,
            FabricGovernanceGate gate) =>
        {
            _ = ApiEndpointHelpers.Actor(context, workspaceId, actors);
            return Results.Ok(gate.Evaluate());
        });

        api.MapGet("/audits", async (
            string workspaceId,
            string correlationId,
            HttpContext context,
            WorkspaceActorResolver actors,
            IActivityAuditSink activityAudit,
            IAppendOnlyAuditSink policyAudit,
            CancellationToken cancellationToken) =>
        {
            _ = ApiEndpointHelpers.Actor(context, workspaceId, actors);
            var activity = await activityAudit.QueryAsync(
                workspaceId,
                correlationId,
                cancellationToken);
            var policy = await policyAudit.QueryAsync(
                workspaceId,
                correlationId,
                cancellationToken);
            return Results.Ok(new
            {
                Activity = activity,
                Policy = policy
            });
        });

        return api;
    }
}
