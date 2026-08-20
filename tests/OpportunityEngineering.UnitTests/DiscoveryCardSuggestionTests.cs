using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class DiscoveryCardSuggestionOutputValidatorTests
{
    private static readonly IReadOnlyList<DiscoveryCardCandidate> Candidates =
    [
        new("card-1", "Copilot Studio Agent", "agentic", "Builds a conversational agent."),
        new("card-2", "Document Intelligence", "information-management", "Extracts structured data."),
    ];

    [TestMethod]
    public void ValidateMapsAWellFormedDraft()
    {
        var draft = new DiscoveryCardSuggestionDraft
        {
            Suggestions =
            [
                new DiscoveryCardSuggestionItemDraft { DiscoveryCardId = "card-1", Rationale = "Fits the pain point." }
            ],
            ConfidenceStatus = "supported",
            RequiredReview = "A human should confirm before shortlisting.",
        };

        var (suggestions, confidence, requiredReview) =
            DiscoveryCardSuggestionOutputValidator.Validate(draft, Candidates);

        Assert.HasCount(1, suggestions);
        Assert.AreEqual("card-1", suggestions[0].DiscoveryCardId);
        Assert.AreEqual(ConfidenceStatus.Supported, confidence);
        Assert.AreEqual("A human should confirm before shortlisting.", requiredReview);
    }

    [TestMethod]
    public void ValidateRejectsACardIdOutsideTheCandidateSet()
    {
        var draft = new DiscoveryCardSuggestionDraft
        {
            Suggestions =
            [
                new DiscoveryCardSuggestionItemDraft { DiscoveryCardId = "invented-card", Rationale = "Made up." }
            ],
            ConfidenceStatus = "supported",
            RequiredReview = "Review needed.",
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            DiscoveryCardSuggestionOutputValidator.Validate(draft, Candidates));

        Assert.AreEqual("discovery_card_suggestion.invalid_candidate", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnUnrecognizedConfidenceStatus()
    {
        var draft = new DiscoveryCardSuggestionDraft
        {
            Suggestions = [],
            ConfidenceStatus = "very confident",
            RequiredReview = "Review needed.",
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            DiscoveryCardSuggestionOutputValidator.Validate(draft, Candidates));

        Assert.AreEqual("discovery_card_suggestion.invalid_confidence", exception.Code);
    }
}

[TestClass]
public sealed class DiscoveryCardSuggestionServiceTests
{
    [TestMethod]
    public async Task SuggestAsyncRejectsReviewerRequests()
    {
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new DiscoveryCardSuggestionService(
            graphStore,
            new FakeDiscoveryCardSuggestionAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.SuggestAsync(TestData.Reviewer(), "engagement-1", "step-1", [], CancellationToken.None));

        Assert.AreEqual("authorization.canonical_mutation_denied", exception.Code);
    }

    [TestMethod]
    public async Task SuggestAsyncExcludesCardsAlreadyShortlistedForTheStep()
    {
        var graph = TestData.CreateGraph()
            .AddPersona(new Persona("persona-1", "Alex", "Advisor", ["Resolve faster"], ["Slow handling"], []))
            .AddJourneyMap(new JourneyMap(
                "journey-map-1",
                "persona-1",
                null,
                [new JourneyStep("step-1", 1, "Triage", "Manual triage is slow", "Automate triage", "median handling time")]))
            .AddCardShortlistEntry(new CardShortlistEntry(
                "entry-1", "step-1", "card-1", "Already added", 1, true));
        var graphStore = new InMemoryGraphStore(graph);
        var agent = new FakeDiscoveryCardSuggestionAgent();
        var service = new DiscoveryCardSuggestionService(
            graphStore,
            agent,
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());
        var candidates = new DiscoveryCardCandidate[]
        {
            new("card-1", "Already shortlisted", "agentic", "..."),
            new("card-2", "Still eligible", "information-management", "..."),
        };

        await service.SuggestAsync(TestData.Facilitator(), "engagement-1", "step-1", candidates, CancellationToken.None);

        Assert.HasCount(1, agent.LastCandidates!);
        Assert.AreEqual("card-2", agent.LastCandidates![0].Id);
    }
}

internal sealed class FakeDiscoveryCardSuggestionAgent : IDiscoveryCardSuggestionAgent
{
    public IReadOnlyList<DiscoveryCardCandidate>? LastCandidates { get; private set; }

    public Task<DiscoveryCardSuggestionResult> SuggestAsync(
        OpportunityGraph graph,
        JourneyStep journeyStep,
        IReadOnlyList<DiscoveryCardCandidate> candidates,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        LastCandidates = candidates;
        return Task.FromResult(new DiscoveryCardSuggestionResult(
            [],
            ConfidenceStatus.Abstain,
            "test",
            graph.ObjectVersion,
            actor.CorrelationId,
            "test-model",
            TestData.Now));
    }
}
