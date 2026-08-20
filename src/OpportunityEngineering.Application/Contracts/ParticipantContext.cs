namespace OpportunityEngineering.Application.Contracts;

/// <summary>
/// Identity for an unauthenticated workshop participant who joined a live session via a
/// join code. Deliberately unrelated to <see cref="ActorContext"/> and carries no
/// <see cref="ApplicationRole"/>. No service that mutates the canonical OpportunityGraph
/// accepts this type, so a participant can never reach canonical mutation rights.
/// </summary>
public sealed record ParticipantContext(
    string ParticipantId,
    string WorkspaceId,
    string EngagementId,
    string JoinSessionId,
    string DisplayName,
    string CorrelationId);
