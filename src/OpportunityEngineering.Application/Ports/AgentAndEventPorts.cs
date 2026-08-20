using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Ports;

public interface IRecommendationAgent
{
    Task<OpportunityRecommendation> RecommendAsync(
        OpportunityGraph graph,
        Opportunity opportunity,
        ActorContext actor,
        CancellationToken cancellationToken);
}

public interface IDiscoveryCardSuggestionAgent
{
    Task<DiscoveryCardSuggestionResult> SuggestAsync(
        OpportunityGraph graph,
        JourneyStep journeyStep,
        IReadOnlyList<DiscoveryCardCandidate> candidates,
        ActorContext actor,
        CancellationToken cancellationToken);
}

public interface IArtifactNarrativeAgent
{
    Task<ArtifactNarrative> SummarizeAsync(
        OpportunityGraph graph,
        Opportunity opportunity,
        ArtifactType artifactType,
        IArtifactContent content,
        ActorContext actor,
        CancellationToken cancellationToken);
}

public interface IEvidenceQualityAgent
{
    Task<EvidenceQualityAssessment> AssessAsync(
        OpportunityGraph graph,
        Evidence evidence,
        ActorContext actor,
        CancellationToken cancellationToken);
}

public interface IFrameDraftAgent
{
    Task<FrameDraftResult> DraftAsync(
        OpportunityGraph graph,
        ActorContext actor,
        CancellationToken cancellationToken);
}

/// <summary>Checks whether a drafted candidate's cited evidence actually substantiates the
/// specific claims it's attached to: citation groundedness, not evidence wording quality (that's
/// <see cref="IEvidenceQualityAgent"/>'s concern). Chained automatically after
/// <see cref="IFrameDraftAgent.DraftAsync"/> via a <c>Microsoft.Agents.AI.Workflows</c> graph in
/// <c>FrameDraftService</c>; never called standalone.</summary>
public interface IFrameCritiqueAgent
{
    Task<IReadOnlyList<string>> CritiqueAsync(
        OpportunityGraph graph,
        FrameDraftCandidate candidate,
        ActorContext actor,
        CancellationToken cancellationToken);
}

public interface IBoardClusterAgent
{
    Task<BoardClusterResult> SuggestAsync(
        IReadOnlyList<BoardClusterCardInput> cards,
        ActorContext actor,
        CancellationToken cancellationToken);
}

public interface IAgentPolicyEvaluator
{
    Task<PolicyDecision> EvaluateAsync(
        ActorContext actor,
        string evaluationPoint,
        string? toolName,
        CancellationToken cancellationToken);
}

public sealed record PolicyDecision(
    PolicyVerdict Verdict,
    string PolicyVersion,
    string Reason)
{
    public bool Permitted => Verdict is PolicyVerdict.Allow or PolicyVerdict.Warn;
}

public interface IEventPublisher
{
    Task PublishAsync(GraphChangedEvent graphEvent, CancellationToken cancellationToken);
}

public interface IEventConsumerClaimStore
{
    Task<bool> TryClaimAsync(
        string workspaceId,
        string eventId,
        string consumerName,
        CancellationToken cancellationToken);

    Task CompleteAsync(ConsumerResult result, CancellationToken cancellationToken);

    Task ReleaseAsync(
        string workspaceId,
        string eventId,
        string consumerName,
        CancellationToken cancellationToken);
}
