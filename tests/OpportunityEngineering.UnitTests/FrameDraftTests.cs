using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class FrameDraftOutputValidatorTests
{
    private static readonly IReadOnlyList<string> ApprovedEvidenceIds = ["evidence-1"];

    private static FrameDraftCandidateDraft ValidCandidate(string confidenceStatus = "limited") => new()
    {
        Workflow = new WorkflowDraftContent(
            "A customer submits a claim.",
            ["Advisor"],
            ["Claim form"],
            ["Receive claim", "Review documents", "Approve or deny"],
            ["Approve or deny"],
            ["CRM"],
            [],
            [],
            ["Decision letter"]),
        Problem = new ProblemDraftContent(
            "Advisor",
            "Respond to claims faster",
            "Manual triage",
            "Slower resolution and lower satisfaction",
            ["evidence-1"],
            0.7m),
        ConfidenceStatus = confidenceStatus,
        RequiredReview = "A human should confirm this draft against the source evidence.",
    };

    private static FrameDraft ValidDraft(int candidateCount = 1) => new()
    {
        Candidates = [.. Enumerable.Range(0, candidateCount).Select(_ => ValidCandidate())]
    };

    [TestMethod]
    public void ValidateMapsAWellFormedDraft()
    {
        var candidates = FrameDraftOutputValidator.Validate(ValidDraft(), ApprovedEvidenceIds);

        Assert.HasCount(1, candidates);
        Assert.AreEqual("A customer submits a claim.", candidates[0].Workflow.Trigger);
        Assert.HasCount(3, candidates[0].Workflow.Steps);
        Assert.AreEqual("Advisor", candidates[0].Problem.User);
        Assert.HasCount(1, candidates[0].Problem.EvidenceReferences);
        Assert.AreEqual(ConfidenceStatus.Limited, candidates[0].ConfidenceStatus);
        Assert.AreEqual(ValidCandidate().RequiredReview, candidates[0].RequiredReview);
    }

    [TestMethod]
    public void ValidateMapsSeveralDistinctCandidatesPreservingEachOwnConfidence()
    {
        var draft = new FrameDraft
        {
            Candidates = [ValidCandidate("supported"), ValidCandidate("abstain")]
        };

        var candidates = FrameDraftOutputValidator.Validate(draft, ApprovedEvidenceIds);

        Assert.HasCount(2, candidates);
        Assert.AreEqual(ConfidenceStatus.Supported, candidates[0].ConfidenceStatus);
        Assert.AreEqual(ConfidenceStatus.Abstain, candidates[1].ConfidenceStatus);
    }

    [TestMethod]
    public void ValidateRejectsAnEmptyCandidateList()
    {
        var draft = new FrameDraft { Candidates = [] };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            FrameDraftOutputValidator.Validate(draft, ApprovedEvidenceIds));

        Assert.AreEqual("frame_draft.invalid_output", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsMoreCandidatesThanTheConfiguredCap()
    {
        var draft = ValidDraft(candidateCount: 6);

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            FrameDraftOutputValidator.Validate(draft, ApprovedEvidenceIds));

        Assert.AreEqual("frame_draft.output_limit_exceeded", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAWorkflowWithNoSteps()
    {
        var draft = new FrameDraft
        {
            Candidates = [ValidCandidate() with { Workflow = ValidCandidate().Workflow with { Steps = [] } }]
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            FrameDraftOutputValidator.Validate(draft, ApprovedEvidenceIds));

        Assert.AreEqual("frame_draft.invalid_output", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnEvidenceReferenceOutsideTheApprovedSet()
    {
        var draft = new FrameDraft
        {
            Candidates = [ValidCandidate() with { Problem = ValidCandidate().Problem with { EvidenceReferences = ["invented-evidence"] } }]
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            FrameDraftOutputValidator.Validate(draft, ApprovedEvidenceIds));

        Assert.AreEqual("frame_draft.invalid_citation", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnOutOfRangeConfidence()
    {
        var draft = new FrameDraft
        {
            Candidates = [ValidCandidate() with { Problem = ValidCandidate().Problem with { Confidence = 1.5m } }]
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            FrameDraftOutputValidator.Validate(draft, ApprovedEvidenceIds));

        Assert.AreEqual("frame_draft.invalid_output", exception.Code);
    }
}

[TestClass]
public sealed class FrameDraftServiceTests
{
    [TestMethod]
    public async Task DraftAsyncRejectsReviewerRequests()
    {
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new FrameDraftService(
            graphStore,
            new FakeFrameDraftAgent(),
            new FakeFrameCritiqueAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.DraftAsync(TestData.Reviewer(), "engagement-1", CancellationToken.None));

        Assert.AreEqual("authorization.canonical_mutation_denied", exception.Code);
    }

    [TestMethod]
    public async Task DraftAsyncReturnsTheAgentDraftForFacilitators()
    {
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var agent = new FakeFrameDraftAgent();
        var service = new FrameDraftService(
            graphStore,
            agent,
            new FakeFrameCritiqueAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var result = await service.DraftAsync(TestData.Facilitator(), "engagement-1", CancellationToken.None);

        Assert.HasCount(1, result.Candidates);
        Assert.AreEqual("Test trigger.", result.Candidates[0].Workflow.Trigger);
        Assert.IsTrue(agent.WasCalled);
    }

    [TestMethod]
    public async Task DraftAsyncSkipsCritiqueWhenNoCandidateCitesEvidence()
    {
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var critiqueAgent = new FakeFrameCritiqueAgent();
        var service = new FrameDraftService(
            graphStore,
            new FakeFrameDraftAgent(),
            critiqueAgent,
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var result = await service.DraftAsync(TestData.Facilitator(), "engagement-1", CancellationToken.None);

        Assert.IsFalse(critiqueAgent.WasCalled);
        Assert.IsEmpty(result.Candidates[0].CitationConcerns);
    }

    [TestMethod]
    public async Task DraftAsyncRunsCritiqueAndMergesConcernsWhenACandidateCitesEvidence()
    {
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var critiqueAgent = new FakeFrameCritiqueAgent(["This citation looks like a stretch."]);
        var service = new FrameDraftService(
            graphStore,
            new FakeFrameDraftAgent(evidenceReferences: ["evidence-1"]),
            critiqueAgent,
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var result = await service.DraftAsync(TestData.Facilitator(), "engagement-1", CancellationToken.None);

        Assert.IsTrue(critiqueAgent.WasCalled);
        Assert.HasCount(1, result.Candidates[0].CitationConcerns);
        Assert.AreEqual("This citation looks like a stretch.", result.Candidates[0].CitationConcerns[0]);
    }
}

internal sealed class FakeFrameDraftAgent(IReadOnlyList<string>? evidenceReferences = null) : IFrameDraftAgent
{
    public bool WasCalled { get; private set; }

    public Task<FrameDraftResult> DraftAsync(
        OpportunityGraph graph,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.FromResult(new FrameDraftResult(
            [
                new FrameDraftCandidate(
                    new WorkflowDraftContent("Test trigger.", [], [], ["Step 1"], [], [], [], [], []),
                    new ProblemDraftContent("User", "Goal", "Constraint", "Impact", evidenceReferences ?? [], 0.5m),
                    ConfidenceStatus.Limited,
                    "test",
                    [])
            ],
            graph.ObjectVersion,
            actor.CorrelationId,
            "test-model",
            TestData.Now));
    }
}

internal sealed class FakeFrameCritiqueAgent(IReadOnlyList<string>? concerns = null) : IFrameCritiqueAgent
{
    public bool WasCalled { get; private set; }

    public Task<IReadOnlyList<string>> CritiqueAsync(
        OpportunityGraph graph,
        FrameDraftCandidate candidate,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        return Task.FromResult(concerns ?? []);
    }
}

[TestClass]
public sealed class FrameCritiqueOutputValidatorTests
{
    [TestMethod]
    public void ValidateMapsWellFormedConcerns()
    {
        var draft = new FrameCritiqueDraft { Concerns = ["Evidence-1 doesn't support the stated Impact."] };

        var concerns = FrameCritiqueOutputValidator.Validate(draft);

        Assert.HasCount(1, concerns);
        Assert.AreEqual("Evidence-1 doesn't support the stated Impact.", concerns[0]);
    }

    [TestMethod]
    public void ValidateAllowsAnEmptyConcernsList()
    {
        var draft = new FrameCritiqueDraft { Concerns = [] };

        var concerns = FrameCritiqueOutputValidator.Validate(draft);

        Assert.IsEmpty(concerns);
    }

    [TestMethod]
    public void ValidateRejectsMoreConcernsThanTheConfiguredCap()
    {
        var draft = new FrameCritiqueDraft
        {
            Concerns = [.. Enumerable.Range(0, 11).Select(i => $"Concern {i}.")]
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            FrameCritiqueOutputValidator.Validate(draft));

        Assert.AreEqual("frame_critique.invalid_output", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnEmptyConcernString()
    {
        var draft = new FrameCritiqueDraft { Concerns = [" "] };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            FrameCritiqueOutputValidator.Validate(draft));

        Assert.AreEqual("frame_critique.invalid_output", exception.Code);
    }
}
