using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>
/// Interactive, synchronous counterpart to RecommendationSubmissionService/
/// RecommendationExecutionService's durable-operation flow. A facilitator wants a suggestion
/// while looking at the journey step, not to poll an operation. Still carries the same
/// guardrail policy checks and audit trail every model call in this codebase goes through.
/// </summary>
public sealed class DiscoveryCardSuggestionService(
    IOpportunityGraphStore graphStore,
    IDiscoveryCardSuggestionAgent suggestionAgent,
    IAgentPolicyEvaluator policyEvaluator,
    IAppendOnlyAuditSink auditSink,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    private const int MaximumCandidates = 79;

    public async Task<DiscoveryCardSuggestionResult> SuggestAsync(
        ActorContext actor,
        string engagementId,
        string journeyStepId,
        IReadOnlyList<DiscoveryCardCandidate> candidates,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);

        if (candidates.Count > MaximumCandidates)
        {
            throw new DomainException(
                "discovery_card_suggestion.candidate_limit_exceeded",
                "The supplied candidate set exceeded the known discovery card catalog size.");
        }

        var graph = await graphStore.GetAsync(actor.WorkspaceId, engagementId, cancellationToken)
            ?? throw new DomainException("engagement.not_found", "Engagement was not found.");
        var step = graph.JourneyMaps
            .SelectMany(map => map.Steps)
            .SingleOrDefault(item => item.Id == journeyStepId)
            ?? throw new DomainException("journey_step.not_found", "Journey step was not found.");

        var alreadyShortlisted = graph.CardShortlist
            .Where(entry => entry.JourneyStepId == journeyStepId)
            .Select(entry => entry.DiscoveryCardId)
            .ToHashSet(StringComparer.Ordinal);
        var eligibleCandidates = candidates
            .Where(candidate => !alreadyShortlisted.Contains(candidate.Id))
            .ToArray();

        var modelCallPolicy = await policyEvaluator.EvaluateAsync(
            actor,
            "discovery_card_suggestion.model_call",
            "foundry-discovery-card-suggestion",
            cancellationToken);
        await auditSink.AppendAsync(
            new PolicyAuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                modelCallPolicy.PolicyVersion,
                "discovery_card_suggestion.model_call",
                modelCallPolicy.Verdict,
                modelCallPolicy.Reason,
                timeProvider.GetUtcNow(),
                actor.CorrelationId,
                "foundry-discovery-card-suggestion"),
            cancellationToken);
        if (!modelCallPolicy.Permitted)
        {
            throw new DomainException(
                "policy.model_call_denied",
                "Policy denied or escalated the model call.");
        }

        var result = await suggestionAgent.SuggestAsync(graph, step, eligibleCandidates, actor, cancellationToken);

        var outputPolicy = await policyEvaluator.EvaluateAsync(
            actor,
            "discovery_card_suggestion.output",
            null,
            cancellationToken);
        await auditSink.AppendAsync(
            new PolicyAuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                outputPolicy.PolicyVersion,
                "discovery_card_suggestion.output",
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
                "Policy denied or escalated the suggestion output.");
        }
#pragma warning restore IDE0046

        return result;
    }

    private static void RequireFacilitatorMutation(ActorContext actor)
    {
        if (!actor.Has(ApplicationRole.Facilitator))
        {
            throw new DomainException(
                "authorization.canonical_mutation_denied",
                "Reviewers cannot request discovery card suggestions.");
        }
    }
}
