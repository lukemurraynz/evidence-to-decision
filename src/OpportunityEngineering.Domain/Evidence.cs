namespace OpportunityEngineering.Domain;

/// <summary>Preserves attributable source wording and validation history for one claim.</summary>
public sealed record Evidence
{
    private Evidence()
    {
    }

    public required string Id { get; init; }
    public required EvidenceType Type { get; init; }
    public required string Statement { get; init; }
    public string? Interpretation { get; init; }
    public required string SourceReference { get; init; }
    public string? ParticipantReference { get; init; }
    public required DateTimeOffset CapturedAt { get; init; }
    public required EvidenceModality Modality { get; init; }
    public required decimal Confidence { get; init; }
    public required ValidationStatus ValidationStatus { get; init; }
    public string? MultimodalAssetId { get; init; }
    public long ObjectVersion { get; init; } = 1;
    public IReadOnlyList<EvidenceRevision> Revisions { get; init; } = [];

    public static Evidence Capture(
        string id,
        EvidenceType type,
        string statement,
        string sourceReference,
        DateTimeOffset capturedAt,
        EvidenceModality modality,
        decimal confidence,
        ValidationStatus validationStatus,
        string? participantReference,
        string? interpretation,
        string? multimodalAssetId)
    {
        if (string.IsNullOrWhiteSpace(id) ||
            string.IsNullOrWhiteSpace(statement) ||
            string.IsNullOrWhiteSpace(sourceReference))
        {
            throw new DomainException("evidence.required", "Evidence ID, statement, and source are required.");
        }

        if (confidence is < 0 or > 1)
        {
            throw new DomainException("evidence.confidence", "Evidence confidence must be between 0 and 1.");
        }

        if (modality is EvidenceModality.Transcript && string.IsNullOrWhiteSpace(multimodalAssetId))
        {
            throw new DomainException(
                "evidence.transcript_asset_required",
                "Transcript evidence must reference its multimodal asset.");
        }

        // A ternary here would push a 9-property initializer into the false-branch; the guard clause reads clearer.
#pragma warning disable IDE0046
        if (modality is EvidenceModality.Transcript && confidence < 0.80m &&
            validationStatus is ValidationStatus.Validated)
        {
            throw new DomainException(
                "evidence.human_correction_required",
                "A transcript below 0.80 confidence requires a human correction before validation.");
        }
#pragma warning restore IDE0046

        return new Evidence
        {
            Id = id,
            Type = type,
            Statement = statement,
            Interpretation = interpretation,
            SourceReference = sourceReference,
            ParticipantReference = participantReference,
            CapturedAt = capturedAt,
            Modality = modality,
            Confidence = confidence,
            ValidationStatus = validationStatus,
            MultimodalAssetId = multimodalAssetId
        };
    }

    public Evidence Correct(
        string correctedStatement,
        string correctedBy,
        DateTimeOffset correctedAt,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(correctedStatement) ||
            string.IsNullOrWhiteSpace(correctedBy) ||
            string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                "evidence.correction_required",
                "A correction requires wording, actor, and reason.");
        }

        var revision = new EvidenceRevision(
            ObjectVersion + 1,
            correctedStatement,
            correctedBy,
            correctedAt,
            reason);

        return this with
        {
            ValidationStatus = ValidationStatus.Validated,
            ObjectVersion = ObjectVersion + 1,
            Revisions = [.. Revisions, revision]
        };
    }

    public string EffectiveStatement =>
        Revisions.Count == 0 ? Statement : Revisions[^1].CorrectedStatement;
}

public sealed record EvidenceRevision(
    long Version,
    string CorrectedStatement,
    string CorrectedBy,
    DateTimeOffset CorrectedAt,
    string Reason);

public sealed record EvidenceConflict(
    string Id,
    string FirstEvidenceId,
    string SecondEvidenceId,
    string Subject,
    string ValidationAction,
    DateTimeOffset RecordedAt,
    string RecordedBy);

public sealed record SpeakerSegment(
    string SpeakerReference,
    TimeSpan Start,
    TimeSpan End,
    string Text,
    decimal Confidence,
    bool HumanCorrected);

public sealed record MultimodalEvidenceAsset
{
    public required string Id { get; init; }
    public required EvidenceModality Modality { get; init; }
    public required string StorageReference { get; init; }
    public required DateTimeOffset CapturedAt { get; init; }
    public required string SourceReference { get; init; }
    public required decimal ExtractionConfidence { get; init; }
    public required ValidationStatus ValidationStatus { get; init; }
    public IReadOnlyList<SpeakerSegment> SpeakerSegments { get; init; } = [];
    public string RedactionStatus { get; init; } = "pending";
}
