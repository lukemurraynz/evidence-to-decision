using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Infrastructure.Cosmos;

internal static class DocumentTypes
{
    public const string Graph = "graph";
    public const string Event = "event";
    public const string DomainAudit = "domain-audit";
    public const string PolicyAudit = "policy-audit";
    public const string Operation = "operation";
    public const string IdempotencyKey = "idempotency-key";
    public const string Recommendation = "recommendation";
    public const string Artifact = "artifact";
    public const string Analytics = "analytics";
    public const string Review = "review";
    public const string ReviewerNotification = "reviewer-notification";
    public const string ConsumerClaim = "consumer-claim";
    public const string LiveSession = "live-session";
    public const string LiveVote = "live-vote";
    public const string LiveIdeationNote = "live-ideation-note";
    public const string LivePin = "live-pin";
    public const string LiveBoardCard = "live-board-card";
}

internal sealed record GraphDocument(
    string id,
    string workspaceId,
    string documentType,
    OpportunityGraph payload);

internal sealed record EventDocument(
    string id,
    string workspaceId,
    string documentType,
    GraphChangedEvent payload,
    bool published = false);

internal sealed record DomainAuditDocument(
    string id,
    string workspaceId,
    string documentType,
    AuditRecord payload);

internal sealed record PolicyAuditDocument(
    string id,
    string workspaceId,
    string documentType,
    PolicyAuditRecord payload);

internal sealed record OperationDocument(
    string id,
    string workspaceId,
    string documentType,
    DurableOperation payload,
    RecommendationWorkItem workItem,
    bool published = false);

internal sealed record IdempotencyKeyDocument(
    string id,
    string workspaceId,
    string documentType,
    string operationId,
    string requestHash);

internal sealed record ProjectionDocument<T>(
    string id,
    string workspaceId,
    string documentType,
    T payload);

internal sealed record ConsumerClaimDocument(
    string id,
    string workspaceId,
    string documentType,
    string eventId,
    string consumerName,
    string status,
    ConsumerResult? result,
    int? ttl = null);

internal sealed record LiveSessionDocument(
    string id,
    string workspaceId,
    string documentType,
    LiveSession payload,
    int? ttl = null);

internal sealed record LiveVoteDocument(
    string id,
    string workspaceId,
    string documentType,
    LiveVote payload,
    int? ttl = null);

internal sealed record LiveIdeationNoteDocument(
    string id,
    string workspaceId,
    string documentType,
    LiveIdeationNote payload,
    int? ttl = null);

internal sealed record LivePinDocument(
    string id,
    string workspaceId,
    string documentType,
    LivePin payload,
    int? ttl = null);

internal sealed record LiveBoardCardDocument(
    string id,
    string workspaceId,
    string documentType,
    LiveBoardCard payload,
    int? ttl = null);
