using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api.Authorization;

public sealed record ParticipantTokenSettings
{
    public required string SigningKey { get; init; }
}

/// <summary>
/// Mints short-lived bearer tokens for workshop participants who joined via a join code,
/// no Entra login involved. Tokens carry only enough to resolve a
/// <see cref="ParticipantContext"/>; they are never accepted by any Entra-scheme
/// (<c>Bearer</c>) endpoint.
/// </summary>
public sealed class ParticipantTokenIssuer(ParticipantTokenSettings settings, TimeProvider timeProvider)
{
    public const string Scheme = "Participant";

    public string Issue(LiveSession session, string participantId, string displayName)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = session.ExpiresAt < now ? now : session.ExpiresAt;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, participantId),
            new Claim("wid", session.WorkspaceId),
            new Claim("eid", session.EngagementId),
            new Claim("sid", session.Id),
            new Claim("name", displayName),
            new Claim("scope", "participant"),
        };
        var credentials = new SigningCredentials(SigningKey(settings), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static SymmetricSecurityKey SigningKey(ParticipantTokenSettings settings) =>
        new(Encoding.UTF8.GetBytes(settings.SigningKey));
}

public sealed class ParticipantContextResolver
{
    /// <summary>Resolves a participant directly from token claims. Used by the SignalR hub,
    /// which has no HTTP route to cross-check the token against.</summary>
    public ParticipantContext Resolve(ClaimsPrincipal principal, string correlationId)
    {
        var participantId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? throw Denied();
        var tokenWorkspaceId = principal.FindFirstValue("wid") ?? throw Denied();
        var tokenEngagementId = principal.FindFirstValue("eid") ?? throw Denied();
        var joinSessionId = principal.FindFirstValue("sid") ?? throw Denied();
        var displayName = principal.FindFirstValue("name") ?? throw Denied();
        return new ParticipantContext(participantId, tokenWorkspaceId, tokenEngagementId, joinSessionId, displayName, correlationId);
    }

    /// <summary>Resolves a participant and verifies the token's workspace/engagement claims
    /// match the route it was presented to: a token minted for one engagement must never be
    /// replayed against another.</summary>
    public ParticipantContext Resolve(
        ClaimsPrincipal principal,
        string workspaceId,
        string engagementId,
        string correlationId)
    {
        var participant = Resolve(principal, correlationId);
        return !string.Equals(participant.WorkspaceId, workspaceId, StringComparison.Ordinal)
            || !string.Equals(participant.EngagementId, engagementId, StringComparison.Ordinal)
                ? throw Denied()
                : participant;
    }

    private static DomainException Denied() =>
        new(
            "authorization.participant_access_denied",
            "This participant session is not valid for the requested engagement.");
}
