using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public static class ArtifactNarrativeOutputValidator
{
    private const int MaximumSummaryLength = 4_000;
    private const int MaximumReviewLength = 1_000;

    public static (string Summary, string RequiredReview) Validate(ArtifactNarrativeDraft draft)
    {
        // A ternary here would push the return tuple into the false-branch of a chained
        // throw-expression, which reads worse than the guard clauses they'd replace.
#pragma warning disable IDE0046
        if (string.IsNullOrWhiteSpace(draft.Summary) || draft.Summary.Length > MaximumSummaryLength)
        {
            throw new DomainException(
                "artifact_narrative.invalid_output",
                "The narrative summary is empty or exceeds its configured size limit.");
        }

        if (string.IsNullOrWhiteSpace(draft.RequiredReview) || draft.RequiredReview.Length > MaximumReviewLength)
        {
            throw new DomainException(
                "artifact_narrative.invalid_output",
                "The narrative's required-review note is empty or exceeds its configured size limit.");
        }
#pragma warning restore IDE0046

        return (draft.Summary, draft.RequiredReview);
    }
}
