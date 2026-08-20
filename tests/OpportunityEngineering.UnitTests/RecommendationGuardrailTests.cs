using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class RecommendationGuardrailTests
{
    [TestMethod]
    public void ValidateAcceptsGroundedCompleteRecommendation()
    {
        var opportunity = TestData.CreateOpportunity() with
        {
            Concepts =
            [
                new Concept(
                    "concept-1",
                    "assist",
                    "triage",
                    "prioritize requests",
                    "agent",
                    "human-in-the-loop",
                    [],
                    [],
                    [],
                    "pilot")
            ]
        };
        var draft = ValidDraft() with { CandidateReferences = ["concept-1"] };

        var result = RecommendationOutputValidator.Validate(draft, opportunity);

        Assert.AreEqual(ConfidenceStatus.Supported, result.ConfidenceStatus);
        Assert.HasCount(1, result.EvidenceReferences);
        Assert.AreEqual("evidence-1", result.EvidenceReferences[0]);
    }

    [TestMethod]
    public void ValidateRejectsCitationOutsideAuthorizedContext()
    {
        var draft = ValidDraft() with { EvidenceReferences = ["other-workspace-evidence"] };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            RecommendationOutputValidator.Validate(draft, TestData.CreateOpportunity()));

        Assert.AreEqual("recommendation.invalid_citation", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsUnknownCandidateReference()
    {
        var draft = ValidDraft() with { CandidateReferences = ["invented-candidate"] };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            RecommendationOutputValidator.Validate(draft, TestData.CreateOpportunity()));

        Assert.AreEqual("recommendation.invalid_candidate", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsUnboundedOutput()
    {
        var draft = ValidDraft() with
        {
            Unknowns = [.. Enumerable.Repeat("unknown", 51)]
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            RecommendationOutputValidator.Validate(draft, TestData.CreateOpportunity()));

        Assert.AreEqual("recommendation.output_limit_exceeded", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsUnsupportedConfidenceStatus()
    {
        var draft = ValidDraft() with { ConfidenceStatus = "certain" };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            RecommendationOutputValidator.Validate(draft, TestData.CreateOpportunity()));

        Assert.AreEqual("recommendation.invalid_confidence", exception.Code);
        StringAssert.Contains(exception.Message, "certain");
    }

    // Regression test: a live Foundry run returned "Human Review Required" against a
    // prompt requiring the literal "human_review_required", and the model's harmless
    // formatting choice discarded an otherwise well-grounded recommendation. The model
    // conveys intent reliably; exact-token compliance is not guaranteed.
    [TestMethod]
    [DataRow("Supported", ConfidenceStatus.Supported)]
    [DataRow("LIMITED", ConfidenceStatus.Limited)]
    [DataRow(" abstain ", ConfidenceStatus.Abstain)]
    [DataRow("Human Review Required", ConfidenceStatus.HumanReviewRequired)]
    [DataRow("human-review-required", ConfidenceStatus.HumanReviewRequired)]
    [DataRow("requires_human_review", ConfidenceStatus.HumanReviewRequired)]
    public void ValidateNormalizesConfidenceStatusFormatting(
        string rawValue,
        ConfidenceStatus expected)
    {
        var draft = ValidDraft() with { ConfidenceStatus = rawValue };

        var result = RecommendationOutputValidator.Validate(draft, TestData.CreateOpportunity());

        Assert.AreEqual(expected, result.ConfidenceStatus);
    }

    private static RecommendationDraft ValidDraft() =>
        new()
        {
            CandidateReferences = [],
            FitDimensions =
            [
                new FitDimension(
                    "workflow fit",
                    "The proposed change addresses the measured delay.",
                    "The current sample covers one team.")
            ],
            EvidenceReferences = ["evidence-1"],
            Unknowns = ["Peak demand is not measured."],
            Limitations = ["A pilot is required before scaling."],
            ConfidenceStatus = "supported",
            RequiredReview = "A reviewer must confirm the pilot scope."
        };
}
