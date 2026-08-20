using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>Facilitator-triggered, whole-board clustering suggestion: the correctly-scoped AI
/// assist for the mural (a single-shot, reviewable draft over everything currently placed), not
/// the per-card chat this app deliberately removed. Has no dependency on
/// <see cref="IOpportunityGraphStore"/>: the board is ephemeral, not canonical, and the frontend
/// already has the placements plus their resolved catalog names in memory (see
/// <see cref="BoardClusterCardInput"/>), the same reason
/// <see cref="DiscoveryCardSuggestionService"/> takes caller-supplied candidates instead of
/// resolving a catalog itself.</summary>
public sealed class BoardClusterService(
    IBoardClusterAgent clusterAgent,
    IAgentPolicyEvaluator policyEvaluator,
    IAppendOnlyAuditSink auditSink,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    public async Task<BoardClusterResult> SuggestClustersAsync(
        ActorContext actor,
        IReadOnlyList<BoardClusterCardInput> cards,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);

        if (cards.Count == 0)
        {
            throw new DomainException(
                "board_cluster.no_cards",
                "There are no cards on the board to cluster.");
        }

        var modelCallPolicy = await policyEvaluator.EvaluateAsync(
            actor,
            "board_cluster.model_call",
            "foundry-board-cluster",
            cancellationToken);
        await auditSink.AppendAsync(
            new PolicyAuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                modelCallPolicy.PolicyVersion,
                "board_cluster.model_call",
                modelCallPolicy.Verdict,
                modelCallPolicy.Reason,
                timeProvider.GetUtcNow(),
                actor.CorrelationId,
                "foundry-board-cluster"),
            cancellationToken);
        if (!modelCallPolicy.Permitted)
        {
            throw new DomainException(
                "policy.model_call_denied",
                "Policy denied or escalated the model call.");
        }

        var result = await clusterAgent.SuggestAsync(cards, actor, cancellationToken);

        var outputPolicy = await policyEvaluator.EvaluateAsync(
            actor,
            "board_cluster.output",
            null,
            cancellationToken);
        await auditSink.AppendAsync(
            new PolicyAuditRecord(
                identifiers.Create(),
                actor.WorkspaceId,
                actor.ActorId,
                outputPolicy.PolicyVersion,
                "board_cluster.output",
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
                "Policy denied or escalated the clustering output.");
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
                "Reviewers cannot request a board clustering suggestion.");
        }
    }
}
