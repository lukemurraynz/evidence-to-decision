using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Api.Contracts;
using OpportunityEngineering.Api.Hubs;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api.Endpoints;

internal static class ParticipantEndpoints
{
    public static WebApplication MapParticipantEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/join/{joinCode}", async (
            string joinCode,
            JoinLiveSessionRequest request,
            LiveSessionService liveSessions,
            ParticipantTokenIssuer tokenIssuer,
            CancellationToken cancellationToken) =>
        {
            var displayName = DisplayNameModeration.Sanitize(request.DisplayName);
            if (displayName.Length is 0 or > 60)
            {
                throw new DomainException(
                    "live_session.invalid_display_name",
                    "A display name between 1 and 60 characters is required.");
            }

            var (session, participantId, step, shortlistedDiscoveryCardIds) =
                await liveSessions.RedeemJoinCodeAsync(joinCode, cancellationToken);
            var token = tokenIssuer.Issue(session, participantId, displayName);
            return Results.Ok(new JoinLiveSessionResponse(
                token,
                session.WorkspaceId,
                session.EngagementId,
                session.Id,
                session.JourneyStepId,
                step?.Name,
                step?.PainPoint,
                shortlistedDiscoveryCardIds));
        }).AllowAnonymous().RequireRateLimiting("join-code");

        app.MapHub<CollaborationHub>("/hubs/collaboration")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{ParticipantTokenIssuer.Scheme}"
            });

        return app;
    }
}
