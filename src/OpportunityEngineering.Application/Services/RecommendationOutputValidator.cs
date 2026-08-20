using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public static class RecommendationOutputValidator
{
    private const int MaximumCandidates = 25;
    private const int MaximumFitDimensions = 20;
    private const int MaximumEvidenceReferences = 100;
    private const int MaximumUnknowns = 50;
    private const int MaximumLimitations = 50;
    private const int MaximumTextLength = 2_000;

    public static ValidatedRecommendationDraft Validate(
        RecommendationDraft draft,
        Opportunity opportunity)
    {
        EnsureCount(draft.CandidateReferences, MaximumCandidates);
        EnsureCount(draft.FitDimensions, MaximumFitDimensions);
        EnsureCount(draft.EvidenceReferences, MaximumEvidenceReferences);
        EnsureCount(draft.Unknowns, MaximumUnknowns);
        EnsureCount(draft.Limitations, MaximumLimitations);

        // Three sequential guards ahead of a 7-argument constructor call; chaining them into
        // ternaries nests throw-expressions inside throw-expressions, which reads worse than
        // the guard clauses it would replace.
#pragma warning disable IDE0046
        if (draft.FitDimensions.Count == 0 ||
            draft.FitDimensions.Any(item =>
                !IsValidText(item.Name) ||
                !IsValidText(item.Explanation) ||
                !IsValidText(item.Limitation)) ||
            !IsValidText(draft.RequiredReview) ||
            draft.Unknowns.Any(item => !IsValidText(item)) ||
            draft.Limitations.Any(item => !IsValidText(item)))
        {
            throw new DomainException(
                "recommendation.invalid_output",
                "The recommendation output is incomplete or contains invalid text.");
        }

        if (draft.EvidenceReferences.Any(reference =>
                !opportunity.EvidenceReferences.Contains(reference, StringComparer.Ordinal)))
        {
            throw new DomainException(
                "recommendation.invalid_citation",
                "The recommendation cited evidence outside its authorized context.");
        }

        var approvedCandidates = opportunity.Concepts
            .Select(concept => concept.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (draft.CandidateReferences.Any(reference => !approvedCandidates.Contains(reference)))
        {
            throw new DomainException(
                "recommendation.invalid_candidate",
                "The recommendation cited a candidate outside its authorized context.");
        }
#pragma warning restore IDE0046

        return new ValidatedRecommendationDraft(
            draft.CandidateReferences,
            draft.FitDimensions,
            draft.EvidenceReferences,
            draft.Unknowns,
            draft.Limitations,
            ParseConfidence(draft.ConfidenceStatus),
            draft.RequiredReview);
    }

    private static bool IsValidText(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= MaximumTextLength;

    private static void EnsureCount<T>(IReadOnlyCollection<T> values, int maximum)
    {
        if (values.Count > maximum)
        {
            throw new DomainException(
                "recommendation.output_limit_exceeded",
                "The recommendation output exceeded its configured size limit.");
        }
    }

    private static ConfidenceStatus ParseConfidence(string value) =>
        ConfidenceStatusParser.Parse(value, invalid => new DomainException(
            "recommendation.invalid_confidence",
            $"The recommendation returned an unsupported confidence status: \"{invalid}\"."));
}
