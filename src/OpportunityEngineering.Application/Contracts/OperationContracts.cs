using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Contracts;

public sealed record DurableOperation
{
    public required string Id { get; init; }
    public required string WorkspaceId { get; init; }
    public required string OperationType { get; init; }
    public required OperationStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required string CorrelationId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string RequestHash { get; init; }
    public string? ResultReference { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorDetail { get; init; }
    public int RetryAfterSeconds { get; init; } = 10;
}

public sealed record RecommendationWorkItem(
    string OperationId,
    string WorkspaceId,
    string EngagementId,
    string OpportunityId,
    string RequestedBy,
    string CorrelationId);
