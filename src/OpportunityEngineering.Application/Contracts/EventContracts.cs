namespace OpportunityEngineering.Application.Contracts;

public sealed record GraphChangedEvent(
    string EventId,
    string EventType,
    string AggregateId,
    string WorkspaceId,
    long CanonicalGraphVersion,
    string ActorReference,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string? AffectedOpportunityId = null,
    string SchemaVersion = "1.1");

public sealed record ConsumerResult(
    string ConsumerName,
    string EventId,
    string WorkspaceId,
    long CanonicalGraphVersion,
    string Result,
    DateTimeOffset CompletedAt,
    string CorrelationId);
