using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

public static class FrameCritiqueOutputValidator
{
    private const int MaximumConcerns = 10;
    private const int MaximumConcernLength = 300;

    public static IReadOnlyList<string> Validate(FrameCritiqueDraft draft)
    {
        return draft.Concerns.Count > MaximumConcerns ||
            draft.Concerns.Any(concern => string.IsNullOrWhiteSpace(concern) || concern.Length > MaximumConcernLength)
            ? throw new DomainException(
                "frame_critique.invalid_output",
                "The citation concerns are too many or exceed the configured size limit.")
            : draft.Concerns;
    }
}
