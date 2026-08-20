using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>
/// Shared by every output validator in this codebase. Models reliably convey intent but not
/// always the exact literal token requested in the prompt (casing, spaces vs.
/// underscores/hyphens). Normalize before matching so a harmless formatting difference doesn't
/// discard an otherwise-valid, well-grounded output; a value that still doesn't map to a known
/// status is still rejected. The caller supplies its own exception so the DomainException code
/// and message stay feature-specific (e.g. "frame_draft.invalid_confidence" vs
/// "recommendation.invalid_confidence") for audit-trail diagnosability.
/// </summary>
internal static class ConfidenceStatusParser
{
    public static ConfidenceStatus Parse(string value, Func<string, DomainException> onInvalid)
    {
        var normalized = value.Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
        return normalized switch
        {
            "supported" => ConfidenceStatus.Supported,
            "limited" => ConfidenceStatus.Limited,
            "abstain" or "abstained" => ConfidenceStatus.Abstain,
            "human_review_required" or "requires_human_review" or "human_review" =>
                ConfidenceStatus.HumanReviewRequired,
            _ => throw onInvalid(value)
        };
    }
}
