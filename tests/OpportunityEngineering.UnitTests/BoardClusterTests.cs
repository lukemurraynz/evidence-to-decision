using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class BoardClusterOutputValidatorTests
{
    private static readonly IReadOnlyList<string> ApprovedPlacementIds = ["placement-1", "placement-2"];

    private static BoardClusterDraft ValidDraft(string confidenceStatus = "limited") => new()
    {
        Clusters =
        [
            new BoardClusterSuggestionDraftItem
            {
                Label = "Onboarding automation",
                PlacementIds = ["placement-1", "placement-2"],
                Rationale = "Both cards target first-run setup.",
            }
        ],
        OutlierPlacementIds = [],
        ConfidenceStatus = confidenceStatus,
        RequiredReview = "A facilitator should confirm these groupings before acting on them.",
    };

    [TestMethod]
    public void ValidateMapsAWellFormedDraft()
    {
        var result = BoardClusterOutputValidator.Validate(
            ValidDraft(), ApprovedPlacementIds, "correlation-1", "test-model", TestData.Now);

        Assert.HasCount(1, result.Clusters);
        Assert.AreEqual("Onboarding automation", result.Clusters[0].Label);
        Assert.HasCount(2, result.Clusters[0].PlacementIds);
        Assert.AreEqual(ConfidenceStatus.Limited, result.ConfidenceStatus);
    }

    [TestMethod]
    public void ValidateAllowsAnEmptyClusterListWhenNothingGroupsMeaningfully()
    {
        var draft = ValidDraft() with { Clusters = [] };

        var result = BoardClusterOutputValidator.Validate(
            draft, ApprovedPlacementIds, "correlation-1", "test-model", TestData.Now);

        Assert.HasCount(0, result.Clusters);
    }

    [TestMethod]
    public void ValidateRejectsMoreClustersThanTheConfiguredCap()
    {
        var draft = ValidDraft() with
        {
            Clusters = [.. Enumerable.Range(0, 9).Select(_ => ValidDraft().Clusters[0])]
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            BoardClusterOutputValidator.Validate(draft, ApprovedPlacementIds, "correlation-1", "test-model", TestData.Now));

        Assert.AreEqual("board_cluster.output_limit_exceeded", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAClusterReferencingAnUnapprovedPlacement()
    {
        var draft = ValidDraft() with
        {
            Clusters = [ValidDraft().Clusters[0] with { PlacementIds = ["invented-placement"] }]
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            BoardClusterOutputValidator.Validate(draft, ApprovedPlacementIds, "correlation-1", "test-model", TestData.Now));

        Assert.AreEqual("board_cluster.invalid_reference", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnOutlierReferencingAnUnapprovedPlacement()
    {
        var draft = ValidDraft() with { OutlierPlacementIds = ["invented-placement"] };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            BoardClusterOutputValidator.Validate(draft, ApprovedPlacementIds, "correlation-1", "test-model", TestData.Now));

        Assert.AreEqual("board_cluster.invalid_reference", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAClusterWithNoPlacements()
    {
        var draft = ValidDraft() with
        {
            Clusters = [ValidDraft().Clusters[0] with { PlacementIds = [] }]
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            BoardClusterOutputValidator.Validate(draft, ApprovedPlacementIds, "correlation-1", "test-model", TestData.Now));

        Assert.AreEqual("board_cluster.invalid_output", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnUnsupportedConfidenceStatus()
    {
        var draft = ValidDraft("very confident");

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            BoardClusterOutputValidator.Validate(draft, ApprovedPlacementIds, "correlation-1", "test-model", TestData.Now));

        Assert.AreEqual("board_cluster.invalid_confidence", exception.Code);
    }
}

[TestClass]
public sealed class BoardClusterServiceTests
{
    private static readonly IReadOnlyList<BoardClusterCardInput> Cards =
        [new BoardClusterCardInput("placement-1", "Automate onboarding", "Fits our persona.", 0.2, 0.2)];

    [TestMethod]
    public async Task SuggestClustersAsyncRejectsReviewerRequests()
    {
        var service = new BoardClusterService(
            new FakeBoardClusterAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.SuggestClustersAsync(TestData.Reviewer(), Cards, CancellationToken.None));

        Assert.AreEqual("authorization.canonical_mutation_denied", exception.Code);
    }

    [TestMethod]
    public async Task SuggestClustersAsyncRejectsAnEmptyBoard()
    {
        var service = new BoardClusterService(
            new FakeBoardClusterAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.SuggestClustersAsync(TestData.Facilitator(), [], CancellationToken.None));

        Assert.AreEqual("board_cluster.no_cards", exception.Code);
    }

    [TestMethod]
    public async Task SuggestClustersAsyncReturnsTheAgentResultForFacilitators()
    {
        var agent = new FakeBoardClusterAgent();
        var service = new BoardClusterService(
            agent,
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var result = await service.SuggestClustersAsync(TestData.Facilitator(), Cards, CancellationToken.None);

        Assert.HasCount(1, result.Clusters);
        Assert.IsTrue(agent.WasCalled);
    }

    [TestMethod]
    public async Task SuggestClustersAsyncNeverCallsTheAgentWhenPolicyDeniesTheModelCall()
    {
        var agent = new FakeBoardClusterAgent();
        var service = new BoardClusterService(
            agent,
            new FixedPolicyEvaluator(PolicyVerdict.Deny),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.SuggestClustersAsync(TestData.Facilitator(), Cards, CancellationToken.None));

        Assert.IsFalse(agent.WasCalled);
    }
}

internal sealed class FakeBoardClusterAgent : IBoardClusterAgent
{
    public bool WasCalled { get; private set; }

    public Task<BoardClusterResult> SuggestAsync(
        IReadOnlyList<BoardClusterCardInput> cards,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.FromResult(new BoardClusterResult(
            [new BoardClusterSuggestion("Test cluster", [.. cards.Select(card => card.PlacementId)], "Test rationale.")],
            [],
            ConfidenceStatus.Limited,
            "test",
            actor.CorrelationId,
            "test-model",
            TestData.Now));
    }
}
