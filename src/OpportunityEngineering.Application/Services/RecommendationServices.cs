using System.Security.Cryptography;
using System.Text;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public sealed class RecommendationSubmissionService(
    IDurableOperationStore operationStore,
    IAgentPolicyEvaluator policyEvaluator,
    IAppendOnlyAuditSink auditSink,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    public async Task<DurableOperation> SubmitAsync(
        ActorContext actor,
        string engagementId,
        string opportunityId,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new DomainException(
                "operation.idempotency_key_required",
                "An idempotency key is required.");
        }

        var requestHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(requestFingerprint)));
        var existing = await operationStore.GetByIdempotencyKeyAsync(
            actor.WorkspaceId,
            idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            return !CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(existing.RequestHash),
                    Convert.FromHexString(requestHash))
                ? throw new DomainException(
                    "operation.idempotency_conflict",
                    "The idempotency key was already used with a different request.")
                : existing;
        }

        var policy = await policyEvaluator.EvaluateAsync(
            actor,
            "recommendation.submit",
            null,
            cancellationToken);
        await auditSink.AppendAsync(
            new PolicyAuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                policy.PolicyVersion,
                "recommendation.submit",
                policy.Verdict,
                policy.Reason,
                timeProvider.GetUtcNow(),
                actor.CorrelationId,
                null),
            cancellationToken);

        if (!policy.Permitted)
        {
            throw new DomainException(
                "policy.recommendation_denied",
                "Policy denied or escalated the recommendation request.");
        }

        var operationId = identifiers.Create();
        var operation = new DurableOperation
        {
            Id = operationId,
            WorkspaceId = actor.WorkspaceId,
            OperationType = "recommendation",
            Status = OperationStatus.Queued,
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow(),
            CorrelationId = actor.CorrelationId,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash
        };
        var workItem = new RecommendationWorkItem(
            operationId,
            actor.WorkspaceId,
            engagementId,
            opportunityId,
            actor.ActorId,
            actor.CorrelationId);

        return await operationStore.CreateAsync(operation, workItem, cancellationToken);
    }
}

public sealed class RecommendationExecutionService(
    IDurableOperationStore operationStore,
    IOpportunityGraphStore graphStore,
    IProjectionStore projectionStore,
    IRecommendationAgent recommendationAgent,
    IAgentPolicyEvaluator policyEvaluator,
    IAppendOnlyAuditSink auditSink,
    IEventConsumerClaimStore claimStore,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(
        RecommendationWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var claimed = await claimStore.TryClaimAsync(
            workItem.WorkspaceId,
            workItem.OperationId,
            "recommendation-executor",
            cancellationToken);
        if (!claimed)
        {
            return;
        }

        try
        {
            var operation = await operationStore.GetAsync(
                workItem.WorkspaceId,
                workItem.OperationId,
                cancellationToken)
                ?? throw new DomainException("operation.not_found", "Operation was not found.");
            if (operation.Status is OperationStatus.Succeeded or
                OperationStatus.Failed or
                OperationStatus.Canceled)
            {
                return;
            }

            await operationStore.UpdateAsync(
                operation with
                {
                    Status = OperationStatus.Running,
                    UpdatedAt = timeProvider.GetUtcNow()
                },
                cancellationToken);

            var graph = await graphStore.GetAsync(
                workItem.WorkspaceId,
                workItem.EngagementId,
                cancellationToken)
                ?? throw new DomainException("engagement.not_found", "Engagement was not found.");
            var opportunity = graph.Opportunities.SingleOrDefault(
                item => item.Id == workItem.OpportunityId)
                ?? throw new DomainException("opportunity.not_found", "Opportunity was not found.");
            var actor = new ActorContext(
                workItem.RequestedBy,
                workItem.WorkspaceId,
                new HashSet<ApplicationRole> { ApplicationRole.Facilitator },
                workItem.CorrelationId);
            var policy = await policyEvaluator.EvaluateAsync(
                actor,
                "recommendation.model_call",
                "foundry-recommendation",
                cancellationToken);
            await auditSink.AppendAsync(
                new PolicyAuditRecord(
                    identifiers.Create(),
                    actor.WorkspaceId,
                    actor.ActorId,
                    policy.PolicyVersion,
                    "recommendation.model_call",
                    policy.Verdict,
                    policy.Reason,
                    timeProvider.GetUtcNow(),
                    actor.CorrelationId,
                    "foundry-recommendation"),
                cancellationToken);
            if (!policy.Permitted)
            {
                throw new DomainException(
                    "policy.model_call_denied",
                    "Policy denied or escalated the model call.");
            }

            var recommendation = await recommendationAgent.RecommendAsync(
                graph,
                opportunity,
                actor,
                cancellationToken);
            var outputPolicy = await policyEvaluator.EvaluateAsync(
                actor,
                "recommendation.output",
                null,
                cancellationToken);
            await auditSink.AppendAsync(
                new PolicyAuditRecord(
                    identifiers.Create(),
                    actor.WorkspaceId,
                    actor.ActorId,
                    outputPolicy.PolicyVersion,
                    "recommendation.output",
                    outputPolicy.Verdict,
                    outputPolicy.Reason,
                    timeProvider.GetUtcNow(),
                    actor.CorrelationId,
                    null),
                cancellationToken);
            if (!outputPolicy.Permitted)
            {
                throw new DomainException(
                    "policy.output_denied",
                    "Policy denied or escalated the recommendation output.");
            }

            await projectionStore.SaveRecommendationAsync(
                workItem.WorkspaceId,
                recommendation,
                cancellationToken);
            await operationStore.UpdateAsync(
                operation with
                {
                    Status = OperationStatus.Succeeded,
                    UpdatedAt = timeProvider.GetUtcNow(),
                    ResultReference = recommendation.RecommendationId
                },
                cancellationToken);
            await claimStore.CompleteAsync(
                new ConsumerResult(
                    "recommendation-executor",
                    workItem.OperationId,
                    workItem.WorkspaceId,
                    graph.ObjectVersion,
                    "succeeded",
                    timeProvider.GetUtcNow(),
                    workItem.CorrelationId),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await claimStore.ReleaseAsync(
                workItem.WorkspaceId,
                workItem.OperationId,
                "recommendation-executor",
                cancellationToken);
            throw;
        }
    }
}
