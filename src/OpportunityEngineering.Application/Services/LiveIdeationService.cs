using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>
/// Accepts and lists ephemeral idea sticky-notes for a live ideation round. Deliberately has no
/// dependency on <see cref="GraphCommandService"/>. Notes only become canonical once a
/// facilitator curates one via <see cref="GraphCommandService.AddIdeationNoteAsync"/>. Text
/// arrives already control-character-sanitized (see <c>DisplayNameModeration</c> in the Api
/// project, which this Application-layer service cannot reference); this service is the trust
/// boundary that enforces length, not spoofing-character shape.
/// </summary>
public sealed class LiveIdeationService(
    ILiveIdeationNoteStore noteStore,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider)
{
    private const int MaximumTextLength = 500;

    public async Task<IReadOnlyList<LiveIdeationNote>> SubmitAsync(
        ParticipantContext participant,
        string text,
        CancellationToken cancellationToken)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            throw new DomainException(
                "live_ideation_note.text_required",
                "An idea needs some text before it can be submitted.");
        }

        if (trimmed.Length > MaximumTextLength)
        {
            throw new DomainException(
                "live_ideation_note.text_too_long",
                $"An idea can be at most {MaximumTextLength} characters.");
        }

        var note = new LiveIdeationNote(
            identifiers.Create(),
            participant.WorkspaceId,
            participant.JoinSessionId,
            participant.ParticipantId,
            participant.DisplayName,
            trimmed,
            timeProvider.GetUtcNow());

        await noteStore.SubmitAsync(note, cancellationToken);
        return await GetNotesAsync(participant.WorkspaceId, participant.JoinSessionId, cancellationToken);
    }

    public Task<IReadOnlyList<LiveIdeationNote>> GetNotesAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken) =>
        noteStore.QueryBySessionAsync(workspaceId, joinSessionId, cancellationToken);

    public Task<LiveIdeationNote?> GetAsync(
        string workspaceId,
        string joinSessionId,
        string noteId,
        CancellationToken cancellationToken) =>
        noteStore.GetAsync(workspaceId, joinSessionId, noteId, cancellationToken);
}
