using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class ArtifactNarrativeOutputValidatorTests
{
    [TestMethod]
    public void ValidateReturnsTheSummaryAndReviewNoteForAWellFormedDraft()
    {
        var draft = new ArtifactNarrativeDraft
        {
            Summary = "This handoff covers reducing handling time for the advisor workflow.",
            RequiredReview = "A human should verify this summary against the structured fields.",
        };

        var (summary, requiredReview) = ArtifactNarrativeOutputValidator.Validate(draft);

        Assert.AreEqual(draft.Summary, summary);
        Assert.AreEqual(draft.RequiredReview, requiredReview);
    }

    [TestMethod]
    public void ValidateRejectsAnEmptySummary()
    {
        var draft = new ArtifactNarrativeDraft { Summary = "   ", RequiredReview = "Review this." };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            ArtifactNarrativeOutputValidator.Validate(draft));

        Assert.AreEqual("artifact_narrative.invalid_output", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnEmptyRequiredReview()
    {
        var draft = new ArtifactNarrativeDraft { Summary = "A valid summary.", RequiredReview = "" };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            ArtifactNarrativeOutputValidator.Validate(draft));

        Assert.AreEqual("artifact_narrative.invalid_output", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsAnOversizedSummary()
    {
        var draft = new ArtifactNarrativeDraft
        {
            Summary = new string('a', 5_000),
            RequiredReview = "Review this.",
        };

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            ArtifactNarrativeOutputValidator.Validate(draft));

        Assert.AreEqual("artifact_narrative.invalid_output", exception.Code);
    }
}
