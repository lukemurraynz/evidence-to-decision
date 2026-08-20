using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public static class EvidenceQualityOutputValidator
{
    private const int MaximumConcerns = 10;
    private const int MaximumConcernLength = 300;
    private const int MaximumSuggestionLength = 2_000;
    private const int MaximumReviewLength = 1_000;

    public static (IReadOnlyList<string> Concerns, string Suggestion, ConfidenceStatus ConfidenceStatus, string RequiredReview)
        Validate(EvidenceQualityDraft draft)
    {
        if (draft.Concerns.Count > MaximumConcerns ||
            draft.Concerns.Any(concern => string.IsNullOrWhiteSpace(concern) || concern.Length > MaximumConcernLength))
        {
            throw new DomainException(
                "evidence_quality.invalid_output",
                "The quality concerns are empty, too many, or exceed the configured size limit.");
        }

        // A ternary here would push the return tuple into the false-branch of a chained
        // throw-expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
        if (string.IsNullOrWhiteSpace(draft.Suggestion) || draft.Suggestion.Length > MaximumSuggestionLength)
        {
            throw new DomainException(
                "evidence_quality.invalid_output",
                "The suggestion is empty or exceeds its configured size limit.");
        }

        if (string.IsNullOrWhiteSpace(draft.RequiredReview) || draft.RequiredReview.Length > MaximumReviewLength)
        {
            throw new DomainException(
                "evidence_quality.invalid_output",
                "The required-review note is empty or exceeds its configured size limit.");
        }
#pragma warning restore IDE0046

        return (draft.Concerns, draft.Suggestion, ParseConfidence(draft.ConfidenceStatus), draft.RequiredReview);
    }

    private static ConfidenceStatus ParseConfidence(string value) =>
        ConfidenceStatusParser.Parse(value, invalid => new DomainException(
            "evidence_quality.invalid_confidence",
            $"The quality assessment returned an unsupported confidence status: \"{invalid}\"."));
}
