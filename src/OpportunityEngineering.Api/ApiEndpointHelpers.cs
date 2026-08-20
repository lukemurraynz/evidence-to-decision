using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Api.Hubs;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api;

internal static class ApiEndpointHelpers
{
    public static ActorContext Actor(
        HttpContext context,
        string workspaceId,
        WorkspaceActorResolver resolver) =>
        resolver.Resolve(
            context.User,
            workspaceId,
            context.TraceIdentifier);

    public static long RequiredVersion(HttpRequest request)
    {
        var value = request.Headers[HeaderNames.IfMatch].ToString().Trim('"');
        return !long.TryParse(value, out var version) || version <= 0
            ? throw new DomainException(
                "graph.if_match_required",
                "A valid canonical graph version is required in the If-Match header.")
            : version;
    }

    public static IResult GraphResult(HttpResponse response, OpportunityGraph graph)
    {
        response.Headers.ETag = $"\"{graph.ObjectVersion}\"";
        return Results.Ok(graph);
    }

    /// <summary>Notifies a step's active live session (if any) that its shortlist changed,
    /// keeping a joined participant's votable card set live rather than frozen at join time.</summary>
    public static async Task BroadcastShortlistChangedAsync(
        ILiveSessionStore sessions,
        IHubContext<CollaborationHub, ICollaborationClient> hubContext,
        OpportunityGraph graph,
        string workspaceId,
        string engagementId,
        string journeyStepId,
        CancellationToken cancellationToken)
    {
        var session = await sessions.GetActiveByStepAsync(workspaceId, engagementId, journeyStepId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var cardIds = graph.CardShortlist
            .Where(entry => entry.JourneyStepId == journeyStepId)
            .Select(entry => entry.DiscoveryCardId)
            .Distinct()
            .ToList();
        await hubContext.Clients.Group($"session:{session.Id}").ShortlistUpdated(cardIds);
    }
}
