using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Api.Contracts;
using OpportunityEngineering.Api.Hubs;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api.Endpoints;

internal static class EngagementEndpoints
{
    public static RouteGroupBuilder MapEngagementEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/engagements", async (
            string workspaceId,
            CreateEngagementRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
        {
            var graph = await commands.CreateEngagementAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                request.EngagementId,
                request.MethodVersion,
                request.Owner,
                request.GovernanceOwner,
                request.Objectives,
                request.Participants,
                cancellationToken);
            context.Response.Headers.ETag = $"\"{graph.ObjectVersion}\"";
            return Results.Created(
                $"/api/v1/workspaces/{workspaceId}/engagements/{graph.Id}",
                graph);
        });

        api.MapGet("/engagements", async (
            string workspaceId,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphQueryService queries,
            CancellationToken cancellationToken) =>
        {
            var graphs = await queries.ListEngagementsAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                cancellationToken);
            return Results.Ok(graphs);
        });

        api.MapGet("/engagements/{engagementId}", async (
            string workspaceId,
            string engagementId,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphQueryService queries,
            CancellationToken cancellationToken) =>
        {
            var graph = await queries.GetGraphAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                cancellationToken)
                ?? throw new DomainException("engagement.not_found", "Engagement was not found.");
            context.Response.Headers.ETag = $"\"{graph.ObjectVersion}\"";
            return Results.Ok(graph);
        });

        api.MapPost("/engagements/{engagementId}/details", async (
            string workspaceId,
            string engagementId,
            UpdateEngagementDetailsRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
        {
            var graph = await commands.UpdateEngagementDetailsAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                request.Objectives,
                request.Participants,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken);
            context.Response.Headers.ETag = $"\"{graph.ObjectVersion}\"";
            return Results.Ok(graph);
        });

        api.MapDelete("/engagements/{engagementId}", async (
            string workspaceId,
            string engagementId,
            [FromBody] DeleteEngagementRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
        {
            await commands.DeleteEngagementAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                request.TypedConfirmation,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken);
            return Results.NoContent();
        });

        api.MapPost("/engagements/{engagementId}/frame-draft", async (
            string workspaceId,
            string engagementId,
            HttpContext context,
            WorkspaceActorResolver actors,
            FrameDraftService drafts,
            CancellationToken cancellationToken) =>
            Results.Ok(await drafts.DraftAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/board/cluster-suggestions", async (
            string workspaceId,
            string engagementId,
            BoardClusterSuggestionRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            BoardClusterService clusters,
            CancellationToken cancellationToken) =>
            Results.Ok(await clusters.SuggestClustersAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                request.Cards,
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/workflows", async (
            string workspaceId,
            string engagementId,
            Workflow workflow,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.AddWorkflowAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                workflow,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/problems", async (
            string workspaceId,
            string engagementId,
            Problem problem,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.AddProblemAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                problem,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/personas", async (
            string workspaceId,
            string engagementId,
            Persona persona,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.AddPersonaAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                persona,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/journey-maps", async (
            string workspaceId,
            string engagementId,
            JourneyMap journeyMap,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.AddJourneyMapAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                journeyMap,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/journey-steps/{journeyStepId}/discovery-card-suggestions", async (
            string workspaceId,
            string engagementId,
            string journeyStepId,
            SuggestDiscoveryCardsRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            DiscoveryCardSuggestionService suggestions,
            CancellationToken cancellationToken) =>
            Results.Ok(await suggestions.SuggestAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                journeyStepId,
                request.Candidates,
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/card-shortlist", async (
            string workspaceId,
            string engagementId,
            CardShortlistEntry entry,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            ILiveSessionStore sessions,
            IHubContext<CollaborationHub, ICollaborationClient> hubContext,
            CancellationToken cancellationToken) =>
        {
            var graph = await commands.AddCardShortlistEntryAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                entry,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken);
            await ApiEndpointHelpers.BroadcastShortlistChangedAsync(
                sessions, hubContext, graph, workspaceId, engagementId, entry.JourneyStepId, cancellationToken);
            return ApiEndpointHelpers.GraphResult(context.Response, graph);
        });

        api.MapPost("/engagements/{engagementId}/card-shortlist/{entryId}/selection", async (
            string workspaceId,
            string engagementId,
            string entryId,
            MarkCardShortlistSelectionRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            CancellationToken cancellationToken) =>
            ApiEndpointHelpers.GraphResult(context.Response, await commands.MarkCardShortlistSelectionAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                entryId,
                request.FacilitatorSelected,
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken)));

        api.MapGet("/engagements/{engagementId}/cards", async (
            string workspaceId,
            string engagementId,
            string? type,
            string? search,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphQueryService queries,
            CancellationToken cancellationToken) =>
            Results.Ok(await queries.GetCardsAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                type,
                search,
                cancellationToken)));

        return api;
    }
}
