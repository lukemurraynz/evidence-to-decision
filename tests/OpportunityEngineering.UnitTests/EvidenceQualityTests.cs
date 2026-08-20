using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class EvidenceQualityOutputValidatorTests
{
    [TestMethod]
    public void ValidateMapsAWellFormedDraft()
    {
        var draft = new EvidenceQualityDraft
        {
            Concerns = ["The statement is vague about who was affected."],
            Suggestion = "A more specific rewording of the statement.",
            ConfidenceStatus = "limited",
            RequiredReview = "A human should confirm this assessment.",
        };

        var (concerns, suggestion, confidence, requiredReview) =
            EvidenceQualityOutputValidator.Validate(draft);

        Assert.HasCount(1, concerns);
        Assert.AreEqual(draft.Suggestion, suggestion);
        Assert.AreEqual(ConfidenceStatus.Limited, confidence);
        Assert.AreEqual(draft.RequiredReview, requiredReview);
    }

    [TestMethod]
    public void ValidateRejectsAnEmptySuggestion()
    {
        var draft = new EvidenceQualityDraft
        {
            Concerns = [],
            Suggestion = "   ",
            ConfidenceStatus = "supported",
            RequiredReview = "Review this.",
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            EvidenceQualityOutputValidator.Validate(draft));

        Assert.AreEqual("evidence_quality.invalid_output", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnUnrecognizedConfidenceStatus()
    {
        var draft = new EvidenceQualityDraft
        {
            Concerns = [],
            Suggestion = "A suggestion.",
            ConfidenceStatus = "very confident",
            RequiredReview = "Review this.",
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            EvidenceQualityOutputValidator.Validate(draft));

        Assert.AreEqual("evidence_quality.invalid_confidence", exception.Code);
    }
}

[TestClass]
public sealed class EvidenceQualityServiceTests
{
    [TestMethod]
    public async Task AssessAsyncRejectsReviewerRequests()
    {
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new EvidenceQualityService(
            graphStore,
            new FakeEvidenceQualityAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.AssessAsync(TestData.Reviewer(), "engagement-1", "evidence-1", CancellationToken.None));

        Assert.AreEqual("authorization.canonical_mutation_denied", exception.Code);
    }

    [TestMethod]
    public async Task AssessAsyncRejectsAnUnknownEvidenceId()
    {
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new EvidenceQualityService(
            graphStore,
            new FakeEvidenceQualityAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.AssessAsync(TestData.Facilitator(), "engagement-1", "evidence-missing", CancellationToken.None));

        Assert.AreEqual("evidence.not_found", exception.Code);
    }

    [TestMethod]
    public async Task AssessAsyncReturnsTheAgentAssessmentForKnownEvidence()
    {
        var graphStore = new InMemoryGraphStore(TestData.CreateGraph());
        var agent = new FakeEvidenceQualityAgent();
        var service = new EvidenceQualityService(
            graphStore,
            agent,
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var assessment = await service.AssessAsync(
            TestData.Facilitator(), "engagement-1", "evidence-1", CancellationToken.None);

        Assert.AreEqual("evidence-1", assessment.EvidenceId);
        Assert.AreEqual("evidence-1", agent.LastAssessedEvidenceId);
    }
}

internal sealed class FakeEvidenceQualityAgent : IEvidenceQualityAgent
{
    public string? LastAssessedEvidenceId { get; private set; }

    public Task<EvidenceQualityAssessment> AssessAsync(
        OpportunityGraph graph,
        Evidence evidence,
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        LastAssessedEvidenceId = evidence.Id;
        return Task.FromResult(new EvidenceQualityAssessment(
            evidence.Id,
            [],
            "Test suggestion.",
            ConfidenceStatus.Supported,
            "test",
            graph.ObjectVersion,
            actor.CorrelationId,
            "test-model",
            TestData.Now));
    }
}
