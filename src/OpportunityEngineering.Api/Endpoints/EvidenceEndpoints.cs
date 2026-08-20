using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Api.Contracts;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api.Endpoints;

internal static class EvidenceEndpoints
{
    public static RouteGroupBuilder MapEvidenceEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/engagements/{engagementId}/multimodal-assets", async (
            string workspaceId,
            string engagementId,
            MultimodalEvidenceAsset asset,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.AddMultimodalAssetAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                asset,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/evidence", async (
            string workspaceId,
            string engagementId,
            CaptureEvidenceRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.AddEvidenceAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                Evidence.Capture(
                    request.Id,
                    request.Type,
                    request.Statement,
                    request.SourceReference,
                    request.CapturedAt,
                    request.Modality,
                    request.Confidence,
                    request.ValidationStatus,
                    request.ParticipantReference,
                    request.Interpretation,
                    request.MultimodalAssetId),
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/evidence/{evidenceId}/corrections", async (
            string workspaceId,
            string engagementId,
            string evidenceId,
            CorrectEvidenceRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.CorrectEvidenceAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                evidenceId,
                request.CorrectedStatement,
                request.Reason,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/evidence/{evidenceId}/quality-assessment", async (
            string workspaceId,
            string engagementId,
            string evidenceId,
            HttpContext context,
            WorkspaceActorResolver actors,
            EvidenceQualityService qualityAssessments,
            CancellationToken cancellationToken) =>
            Results.Ok(await qualityAssessments.AssessAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                evidenceId,
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/evidence-conflicts", async (
            string workspaceId,
            string engagementId,
            EvidenceConflict conflict,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.AddConflictAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                conflict,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        return api;
    }
}
