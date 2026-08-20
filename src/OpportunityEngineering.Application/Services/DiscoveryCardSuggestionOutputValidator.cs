using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public static class DiscoveryCardSuggestionOutputValidator
{
    private const int MaximumSuggestions = 10;
    private const int MaximumTextLength = 2_000;

    public static (IReadOnlyList<DiscoveryCardSuggestion> Suggestions, ConfidenceStatus ConfidenceStatus, string RequiredReview)
        Validate(DiscoveryCardSuggestionDraft draft, IReadOnlyList<DiscoveryCardCandidate> candidates)
    {
        if (draft.Suggestions.Count > MaximumSuggestions)
        {
            throw new DomainException(
                "discovery_card_suggestion.output_limit_exceeded",
                "The suggestion output exceeded its configured size limit.");
        }

        if (!IsValidText(draft.RequiredReview) ||
            draft.Suggestions.Any(item => !IsValidText(item.DiscoveryCardId) || !IsValidText(item.Rationale)))
        {
            throw new DomainException(
                "discovery_card_suggestion.invalid_output",
                "The suggestion output is incomplete or contains invalid text.");
        }

        var approvedCandidates = candidates
            .Select(candidate => candidate.Id)
            .ToHashSet(StringComparer.Ordinal);
        // A ternary here would push the multi-line return tuple into the false-branch of a
        // throw-expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
        if (draft.Suggestions.Any(item => !approvedCandidates.Contains(item.DiscoveryCardId)))
        {
            throw new DomainException(
                "discovery_card_suggestion.invalid_candidate",
                "The suggestion referenced a card outside its authorized candidate set.");
        }
#pragma warning restore IDE0046

        return (
            [.. draft.Suggestions.Select(item => new DiscoveryCardSuggestion(item.DiscoveryCardId, item.Rationale))],
            ParseConfidence(draft.ConfidenceStatus),
            draft.RequiredReview);
    }

    private static bool IsValidText(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= MaximumTextLength;

    private static ConfidenceStatus ParseConfidence(string value) =>
        ConfidenceStatusParser.Parse(value, invalid => new DomainException(
            "discovery_card_suggestion.invalid_confidence",
            $"The suggestion output returned an unsupported confidence status: \"{invalid}\"."));
}
