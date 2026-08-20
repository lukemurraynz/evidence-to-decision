using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public static class BoardClusterOutputValidator
{
    private const int MaximumClusters = 8;
    private const int MaximumPlacementsPerCluster = 30;
    private const int MaximumLabelLength = 80;
    private const int MaximumFieldLength = 500;
    private const int MaximumReviewLength = 1_000;

    public static BoardClusterResult Validate(
        BoardClusterDraft draft,
        IReadOnlyList<string> approvedPlacementIds,
        string correlationId,
        string generatedBy,
        DateTimeOffset generatedAt)
    {
        if (draft.Clusters.Count > MaximumClusters)
        {
            throw new DomainException(
                "board_cluster.output_limit_exceeded",
                "The clustering agent returned more clusters than its configured size limit.");
        }

        if (!IsValidText(draft.RequiredReview, MaximumReviewLength))
        {
            throw new DomainException(
                "board_cluster.invalid_output",
                "A required-review note is empty or exceeds its configured size limit.");
        }

        var approved = approvedPlacementIds.ToHashSet(StringComparer.Ordinal);
        var clusters = draft.Clusters.Select(cluster => ValidateCluster(cluster, approved)).ToList();

        // A ternary here would push the return value into the false-branch of a
        // throw-expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
        if (draft.OutlierPlacementIds.Count > MaximumPlacementsPerCluster ||
            draft.OutlierPlacementIds.Any(id => !approved.Contains(id)))
        {
            throw new DomainException(
                "board_cluster.invalid_reference",
                "The clustering agent referenced a placement outside its authorized context.");
        }
#pragma warning restore IDE0046

        return new BoardClusterResult(
            clusters,
            draft.OutlierPlacementIds,
            ParseConfidence(draft.ConfidenceStatus),
            draft.RequiredReview,
            correlationId,
            generatedBy,
            generatedAt);
    }

    private static BoardClusterSuggestion ValidateCluster(
        BoardClusterSuggestionDraftItem cluster, HashSet<string> approvedPlacementIds)
    {
        if (!IsValidText(cluster.Label, MaximumLabelLength) ||
            !IsValidText(cluster.Rationale, MaximumFieldLength) ||
            cluster.PlacementIds.Count == 0 ||
            cluster.PlacementIds.Count > MaximumPlacementsPerCluster)
        {
            throw new DomainException(
                "board_cluster.invalid_output",
                "A suggested cluster is incomplete or exceeds its configured size limit.");
        }

        // A ternary here would push the return record into the false-branch of a
        // throw-expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
        if (cluster.PlacementIds.Any(id => !approvedPlacementIds.Contains(id)))
        {
            throw new DomainException(
                "board_cluster.invalid_reference",
                "A suggested cluster referenced a placement outside its authorized context.");
        }
#pragma warning restore IDE0046

        return new BoardClusterSuggestion(cluster.Label, cluster.PlacementIds, cluster.Rationale);
    }

    private static bool IsValidText(string value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;

    private static ConfidenceStatus ParseConfidence(string value) =>
        ConfidenceStatusParser.Parse(value, invalid => new DomainException(
            "board_cluster.invalid_confidence",
            $"The clustering agent returned an unsupported confidence status: \"{invalid}\"."));
}
