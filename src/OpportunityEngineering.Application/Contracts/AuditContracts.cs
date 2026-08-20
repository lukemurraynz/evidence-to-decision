using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Contracts;

public sealed record AuditRecord(
    string Id,
    string WorkspaceId,
    string ActorId,
    string Action,
    string TargetId,
    string Result,
    string Reason,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    long CanonicalGraphVersion);

public sealed record PolicyAuditRecord(
    string Id,
    string WorkspaceId,
    string ActorId,
    string PolicyVersion,
    string EvaluationPoint,
    PolicyVerdict Verdict,
    string Reason,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string? ToolName);
