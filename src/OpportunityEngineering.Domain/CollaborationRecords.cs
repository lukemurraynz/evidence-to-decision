namespace OpportunityEngineering.Domain;

/// <summary>
/// A null <see cref="JourneyStepId"/> means this session is engagement-wide rather than
/// scoped to one journey step (the ideation board's live round is the current example).
/// There is deliberately no separate "purpose"/"session type" field: the null check on
/// JourneyStepId is the whole distinction, mirroring how JourneyMap.WorkflowId already uses
/// nullability for an optional relationship elsewhere in this domain.
/// <see cref="BoardRevealed"/> defaults true so a session document persisted before this
/// field existed still deserializes to today's always-visible mural, no backfill needed.
/// </summary>
public sealed record LiveSession(
    string Id,
    string WorkspaceId,
    string EngagementId,
    string? JourneyStepId,
    string JoinCode,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string Status,
    bool BoardRevealed = true);

public sealed record LiveVote(
    string Id,
    string WorkspaceId,
    string JoinSessionId,
    string ParticipantId,
    string DiscoveryCardId,
    string JourneyStepId,
    DateTimeOffset CastAt);

/// <summary>Ephemeral, pre-curation idea sticky-note. Becomes a durable <see cref="IdeationNote"/>
/// only once a facilitator curates it (see GraphCommandService.AddIdeationNoteAsync).
/// <see cref="DisplayName"/> is captured at submission time (not looked up later from
/// <see cref="ParticipantId"/>, which is an opaque per-join identifier, not a name) so a curated
/// note can credit a human-readable submitter rather than a GUID.</summary>
public sealed record LiveIdeationNote(
    string Id,
    string WorkspaceId,
    string JoinSessionId,
    string ParticipantId,
    string DisplayName,
    string Text,
    DateTimeOffset SubmittedAt);

/// <summary>A participant's personal, catalog-wide "worth a second look" marker, distinct from
/// <see cref="LiveVote"/>, which stays scoped to the facilitator's official shortlist. Never
/// promoted directly; a facilitator still adds a pinned card to the shortlist through the same
/// promote path a voted card uses.</summary>
public sealed record LivePin(
    string Id,
    string WorkspaceId,
    string JoinSessionId,
    string ParticipantId,
    string DiscoveryCardId,
    string JourneyStepId,
    DateTimeOffset PinnedAt);

/// <summary>A participant's (or facilitator's) instance placed on the shared live board, not
/// the catalog card itself. A null <see cref="DiscoveryCardId"/> means this placement is a
/// freeform sticky note rather than a catalog card reference, for capturing an idea the 79-card
/// catalog doesn't cover; <see cref="Rationale"/> carries the note's text either way. The same
/// DiscoveryCardId can appear as multiple independent placements (duplicated by different
/// people, or the same person testing it in a different spot); each gets its own Id so moving
/// one placement never conflicts with moving another that happens to wrap the same catalog card.
/// <see cref="X"/>/<see cref="Y"/> are normalized 0..1 coordinates on the open mural canvas;
/// there is no separate lane/category field. A card's position on the board IS its
/// categorization, exactly like a physical sticky note, with background zone labels rendered
/// purely as a decorative backdrop the frontend never reads placement data against.
/// Blind-upsert, last-write-wins on position. The only real contention (two people moving the
/// SAME placement at once) is rare and low-stakes, matching this app's ephemeral-state
/// convention of not importing the canonical graph's ETag machinery for disposable
/// workshop-session state.</summary>
public sealed record LiveBoardCard(
    string Id,
    string WorkspaceId,
    string JoinSessionId,
    string PlacedByParticipantId,
    string PlacedByDisplayName,
    string? DiscoveryCardId,
    double X,
    double Y,
    string Rationale,
    DateTimeOffset PlacedAt,
    DateTimeOffset LastMovedAt);
