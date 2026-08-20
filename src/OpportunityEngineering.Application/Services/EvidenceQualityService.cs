using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>Interactive, synchronous counterpart to the async recommendation flow. A
/// facilitator wants quality feedback on evidence right after capturing it, not to poll an
/// operation. Carries the same guardrail policy checks and audit trail every model call in
/// this codebase goes through.</summary>
public sealed class EvidenceQualityService(
    IOpportunityGraphStore graphStore,
    IEvidenceQualityAgent qualityAgent,
    IAgentPolicyEvaluator policyEvaluator,
    IAppendOnlyAuditSink auditSink,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    public async Task<EvidenceQualityAssessment> AssessAsync(
        ActorContext actor,
        string engagementId,
        string evidenceId,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);

        var graph = await graphStore.GetAsync(actor.WorkspaceId, engagementId, cancellationToken)
            ?? throw new DomainException("engagement.not_found", "Engagement was not found.");
        var evidence = graph.Evidence.SingleOrDefault(item => item.Id == evidenceId)
            ?? throw new DomainException("evidence.not_found", "Evidence was not found.");

        var modelCallPolicy = await policyEvaluator.EvaluateAsync(
            actor,
            "evidence_quality.model_call",
            "foundry-evidence-quality",
            cancellationToken);
        await auditSink.AppendAsync(
            new PolicyAuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                modelCallPolicy.PolicyVersion,
                "evidence_quality.model_call",
                modelCallPolicy.Verdict,
                modelCallPolicy.Reason,
                timeProvider.GetUtcNow(),
                actor.CorrelationId,
                "foundry-evidence-quality"),
            cancellationToken);
        if (!modelCallPolicy.Permitted)
        {
            throw new DomainException(
                "policy.model_call_denied",
                "Policy denied or escalated the model call.");
        }

        var assessment = await qualityAgent.AssessAsync(graph, evidence, actor, cancellationToken);

        var outputPolicy = await policyEvaluator.EvaluateAsync(
            actor,
            "evidence_quality.output",
            null,
            cancellationToken);
        await auditSink.AppendAsync(
            new PolicyAuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                outputPolicy.PolicyVersion,
                "evidence_quality.output",
                outputPolicy.Verdict,
                outputPolicy.Reason,
                timeProvider.GetUtcNow(),
                actor.CorrelationId,
                null),
            cancellationToken);
        // A ternary here would push the return value into the false-branch of a
        // throw-expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
        if (!outputPolicy.Permitted)
        {
            throw new DomainException(
                "policy.output_denied",
                "Policy denied or escalated the assessment output.");
        }
#pragma warning restore IDE0046

        return assessment;
    }

    private static void RequireFacilitatorMutation(ActorContext actor)
    {
        if (!actor.Has(ApplicationRole.Facilitator))
        {
            throw new DomainException(
                "authorization.canonical_mutation_denied",
                "Reviewers cannot request evidence quality assessments.");
        }
    }
}
