using Microsoft.AspNetCore.SignalR;
using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Api.Contracts;
using OpportunityEngineering.Api.Hubs;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api.Endpoints;

internal static class LiveSessionEndpoints
{
    public static RouteGroupBuilder MapLiveSessionEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/engagements/{engagementId}/live-sessions", async (
            string workspaceId,
            string engagementId,
            CreateLiveSessionRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveSessionService liveSessions,
            CancellationToken cancellationToken) =>
            Results.Ok(await liveSessions.CreateAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                request.JourneyStepId,
                request.StartPrivate,
                cancellationToken)));

        api.MapGet("/engagements/{engagementId}/live-sessions/active", async (
            string workspaceId,
            string engagementId,
            string journeyStepId,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveSessionService liveSessions,
            CancellationToken cancellationToken) =>
        {
            var session = await liveSessions.GetActiveByStepAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                journeyStepId,
                cancellationToken);
            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        api.MapPost("/engagements/{engagementId}/live-sessions/{sessionId}/close", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveSessionService liveSessions,
            IHubContext<CollaborationHub, ICollaborationClient> hubContext,
            CancellationToken cancellationToken) =>
        {
            var session = await liveSessions.CloseAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                sessionId,
                cancellationToken);
            await hubContext.Clients.Group($"session:{session.Id}").SessionClosed();
            return Results.Ok(session);
        });

        api.MapPost("/engagements/{engagementId}/live-sessions/{sessionId}/reveal-board", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveSessionService liveSessions,
            LiveBoardService board,
            IHubContext<CollaborationHub, ICollaborationClient> hubContext,
            CancellationToken cancellationToken) =>
        {
            var session = await liveSessions.RevealBoardAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                sessionId,
                cancellationToken);
            var fullBoard = await board.GetBoardAsync(session.WorkspaceId, session.Id, cancellationToken);
            await hubContext.Clients.Group($"session:{session.Id}").BoardUpdated(fullBoard, revealed: true);
            return Results.Ok(session);
        });

        // No hub broadcast here; see LiveSessionService.SetBoardPrivateAsync's doc comment for
        // why: only placements from this point forward get hidden, existing on-screen cards
        // deliberately stay visible rather than trying to retroactively re-filter every
        // already-connected participant.
        api.MapPost("/engagements/{engagementId}/live-sessions/{sessionId}/private-board", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveSessionService liveSessions,
            CancellationToken cancellationToken) =>
            Results.Ok(await liveSessions.SetBoardPrivateAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                sessionId,
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/live-sessions/{sessionId}/board/clear", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveBoardService board,
            ILiveSessionStore sessions,
            IHubContext<CollaborationHub, ICollaborationClient> hubContext,
            CancellationToken cancellationToken) =>
        {
            var actor = ApiEndpointHelpers.Actor(context, workspaceId, actors);
            if (!actor.Has(ApplicationRole.Facilitator))
            {
                throw new DomainException(
                    "authorization.board_clear_denied", "Reviewers cannot clear the shared board.");
            }

            var emptyBoard = await board.ClearAsync(actor.WorkspaceId, sessionId, cancellationToken);
            var session = await sessions.GetAsync(actor.WorkspaceId, sessionId, cancellationToken);
            await hubContext.Clients.Group($"session:{sessionId}").BoardUpdated(emptyBoard, session?.BoardRevealed ?? true);
            return Results.Ok(emptyBoard);
        });

        api.MapGet("/engagements/{engagementId}/live-sessions/{sessionId}/tally", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveVoteService liveVotes,
            CancellationToken cancellationToken) =>
            Results.Ok(await liveVotes.GetTallyAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors).WorkspaceId,
                sessionId,
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/live-sessions/{sessionId}/promote", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            PromoteLiveVoteRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            IIdentifierFactory identifiers,
            ILiveSessionStore sessions,
            IHubContext<CollaborationHub, ICollaborationClient> hubContext,
            CancellationToken cancellationToken) =>
        {
            var graph = await commands.AddCardShortlistEntryAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors),
                engagementId,
                new CardShortlistEntry(
                    identifiers.Create(),
                    request.JourneyStepId,
                    request.DiscoveryCardId,
                    request.Rationale,
                    request.Rank,
                    FacilitatorSelected: true),
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken);
            await ApiEndpointHelpers.BroadcastShortlistChangedAsync(
                sessions, hubContext, graph, workspaceId, engagementId, request.JourneyStepId, cancellationToken);
            return ApiEndpointHelpers.GraphResult(context.Response, graph);
        });

        api.MapPost("/engagements/{engagementId}/live-sessions/{sessionId}/board/snapshot", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            SnapshotBoardRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            GraphCommandService commands,
            IIdentifierFactory identifiers,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var actor = ApiEndpointHelpers.Actor(context, workspaceId, actors);
            if (!actor.Has(ApplicationRole.Facilitator))
            {
                throw new DomainException(
                    "authorization.board_snapshot_denied", "Reviewers cannot capture the board as evidence.");
            }

            if (request.Items.Count == 0)
            {
                throw new DomainException(
                    "live_board_card.snapshot_empty", "There are no cards on the board to snapshot.");
            }

            var expectedVersion = ApiEndpointHelpers.RequiredVersion(context.Request);
            var now = timeProvider.GetUtcNow();
            OpportunityGraph graph = null!;
            foreach (var item in request.Items)
            {
                var statement = item.CardDisplayName is { Length: > 0 } cardName
                    ? item.Rationale.Length > 0 ? $"{cardName}: {item.Rationale}" : cardName
                    : item.Rationale;
                var evidence = Evidence.Capture(
                    identifiers.Create(),
                    EvidenceType.CustomerStatement,
                    statement,
                    $"Live mural: {item.ZoneLabel}",
                    now,
                    EvidenceModality.Text,
                    confidence: 1.0m,
                    ValidationStatus.Unvalidated,
                    participantReference: item.PlacedByDisplayName,
                    interpretation: null,
                    multimodalAssetId: null);
                graph = await commands.AddEvidenceAsync(actor, engagementId, evidence, expectedVersion, cancellationToken);
                expectedVersion = graph.ObjectVersion;
            }

            return ApiEndpointHelpers.GraphResult(context.Response, graph);
        });

        api.MapGet("/engagements/{engagementId}/live-sessions/{sessionId}/ideation-notes", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveIdeationService ideation,
            CancellationToken cancellationToken) =>
            Results.Ok(await ideation.GetNotesAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors).WorkspaceId,
                sessionId,
                cancellationToken)));

        api.MapPost("/engagements/{engagementId}/live-sessions/{sessionId}/ideation-notes/curate", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            CurateIdeationNoteRequest request,
            HttpContext context,
            WorkspaceActorResolver actors,
            LiveIdeationService ideation,
            GraphCommandService commands,
            IIdentifierFactory identifiers,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var actor = ApiEndpointHelpers.Actor(context, workspaceId, actors);
            var note = await ideation.GetAsync(actor.WorkspaceId, sessionId, request.NoteId, cancellationToken)
                ?? throw new DomainException(
                    "live_ideation_note.not_found",
                    "That idea was not found. It may have expired.");
            var graph = await commands.AddIdeationNoteAsync(
                actor,
                engagementId,
                new IdeationNote(identifiers.Create(), note.Text, note.DisplayName, timeProvider.GetUtcNow()),
                ApiEndpointHelpers.RequiredVersion(context.Request),
                cancellationToken);
            return ApiEndpointHelpers.GraphResult(context.Response, graph);
        });

        api.MapGet("/engagements/{engagementId}/live-sessions/{sessionId}/pins", async (
            string workspaceId,
            string engagementId,
            string sessionId,
            HttpContext context,
            WorkspaceActorResolver actors,
            LivePinService pins,
            CancellationToken cancellationToken) =>
            Results.Ok(await pins.GetTallyAsync(
                ApiEndpointHelpers.Actor(context, workspaceId, actors).WorkspaceId,
                sessionId,
                cancellationToken)));

        return api;
    }
}
