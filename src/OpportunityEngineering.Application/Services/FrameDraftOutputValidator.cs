using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public static class FrameDraftOutputValidator
{
    private const int MaximumCandidates = 5;
    private const int MaximumListLength = 20;
    private const int MaximumItemLength = 300;
    private const int MaximumFieldLength = 500;
    private const int MaximumReviewLength = 1_000;

    public static IReadOnlyList<FrameDraftCandidate> Validate(
        FrameDraft draft, IReadOnlyList<string> approvedEvidenceIds)
    {
        if (draft.Candidates.Count == 0)
        {
            throw new DomainException(
                "frame_draft.invalid_output",
                "The frame draft returned no candidates.");
        }

        if (draft.Candidates.Count > MaximumCandidates)
        {
            throw new DomainException(
                "frame_draft.output_limit_exceeded",
                "The frame draft returned more candidates than its configured size limit.");
        }

        var approved = approvedEvidenceIds.ToHashSet(StringComparer.Ordinal);
        return [.. draft.Candidates.Select(candidate => ValidateCandidate(candidate, approved))];
    }

    private static FrameDraftCandidate ValidateCandidate(
        FrameDraftCandidateDraft candidate, HashSet<string> approvedEvidenceIds)
    {
        var workflow = candidate.Workflow;
        var problem = candidate.Problem;

        if (!IsValidText(workflow.Trigger, MaximumFieldLength) ||
            workflow.Steps.Count == 0 ||
            !IsValidList(workflow.Steps) ||
            !IsValidList(workflow.Actors) ||
            !IsValidList(workflow.Inputs) ||
            !IsValidList(workflow.Decisions) ||
            !IsValidList(workflow.Systems) ||
            !IsValidList(workflow.Handoffs) ||
            !IsValidList(workflow.Exceptions) ||
            !IsValidList(workflow.Outputs))
        {
            throw new DomainException(
                "frame_draft.invalid_output",
                "A drafted workflow is incomplete or exceeds its configured size limit.");
        }

        if (!IsValidText(problem.User, MaximumFieldLength) ||
            !IsValidText(problem.Goal, MaximumFieldLength) ||
            !IsValidText(problem.Constraint, MaximumFieldLength) ||
            !IsValidText(problem.Impact, MaximumFieldLength) ||
            problem.Confidence < 0 || problem.Confidence > 1)
        {
            throw new DomainException(
                "frame_draft.invalid_output",
                "A drafted problem is incomplete, invalid, or exceeds its configured size limit.");
        }

        // A ternary here would push the return record into the false-branch of a
        // throw-expression, which reads worse than the guard clause it would replace.
#pragma warning disable IDE0046
        if (problem.EvidenceReferences.Any(id => !approvedEvidenceIds.Contains(id)))
        {
            throw new DomainException(
                "frame_draft.invalid_citation",
                "A drafted problem cited evidence outside its authorized context.");
        }

        if (!IsValidText(candidate.RequiredReview, MaximumReviewLength))
        {
            throw new DomainException(
                "frame_draft.invalid_output",
                "A required-review note is empty or exceeds its configured size limit.");
        }
#pragma warning restore IDE0046

        // CitationConcerns starts empty. FrameDraftService fills it in after the critique step,
        // which runs after this validator (the draft must be valid before there's anything to
        // critique).
        return new FrameDraftCandidate(workflow, problem, ParseConfidence(candidate.ConfidenceStatus), candidate.RequiredReview, []);
    }

    private static bool IsValidList(IReadOnlyList<string> values) =>
        values.Count <= MaximumListLength &&
        values.All(value => IsValidText(value, MaximumItemLength));

    private static bool IsValidText(string value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength;

    private static ConfidenceStatus ParseConfidence(string value) =>
        ConfidenceStatusParser.Parse(value, invalid => new DomainException(
            "frame_draft.invalid_confidence",
            $"The frame draft returned an unsupported confidence status: \"{invalid}\"."));
}
