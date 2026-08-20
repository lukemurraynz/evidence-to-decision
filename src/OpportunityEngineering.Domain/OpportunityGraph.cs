namespace OpportunityEngineering.Domain;

/// <summary>
/// Owns all authoritative engagement state. Projections and agents may read it but cannot bypass
/// these mutation methods.
/// </summary>
public sealed record OpportunityGraph
{
    private OpportunityGraph()
    {
    }

    public required string Id { get; init; }
    public required string WorkspaceId { get; init; }
    public required string MethodVersion { get; init; }
    public required string Owner { get; init; }
    public required string GovernanceOwner { get; init; }
    public long ObjectVersion { get; init; } = 1;
    public EngagementLifecycle LifecycleState { get; init; } = EngagementLifecycle.Discovery;
    public IReadOnlyList<string> Objectives { get; init; } = [];
    public IReadOnlyList<string> Participants { get; init; } = [];
    public IReadOnlyList<Workflow> Workflows { get; init; } = [];
    public IReadOnlyList<Problem> Problems { get; init; } = [];
    public IReadOnlyList<Persona> Personas { get; init; } = [];
    public IReadOnlyList<JourneyMap> JourneyMaps { get; init; } = [];
    public IReadOnlyList<CardShortlistEntry> CardShortlist { get; init; } = [];
    public IReadOnlyList<IdeationNote> IdeationNotes { get; init; } = [];
    public IReadOnlyList<BxtScore> BxtScores { get; init; } = [];
    public IReadOnlyList<SolutionRecommendation> SolutionRecommendations { get; init; } = [];
    public IReadOnlyList<Evidence> Evidence { get; init; } = [];
    public IReadOnlyList<MultimodalEvidenceAsset> MultimodalAssets { get; init; } = [];
    public IReadOnlyList<EvidenceConflict> EvidenceConflicts { get; init; } = [];
    public IReadOnlyList<Opportunity> Opportunities { get; init; } = [];
    public IReadOnlyList<DecisionRecord> Decisions { get; init; } = [];
    public IReadOnlyList<GovernanceBlocker> Blockers { get; init; } = [];
    public IReadOnlyList<Experiment> Experiments { get; init; } = [];
    public IReadOnlyList<PilotRecord> Pilots { get; init; } = [];
    public IReadOnlyList<OutcomeRecord> Outcomes { get; init; } = [];

    public static OpportunityGraph Create(
        string id,
        string workspaceId,
        string methodVersion,
        string owner,
        string governanceOwner,
        IEnumerable<string> objectives,
        IEnumerable<string> participants)
    {
        // A ternary here would push a 7-property initializer into the false-branch; the guard clause reads clearer.
#pragma warning disable IDE0046
        if (string.IsNullOrWhiteSpace(id) ||
            string.IsNullOrWhiteSpace(workspaceId) ||
            string.IsNullOrWhiteSpace(methodVersion) ||
            string.IsNullOrWhiteSpace(owner) ||
            string.IsNullOrWhiteSpace(governanceOwner))
        {
            throw new DomainException(
                "engagement.required",
                "Engagement ID, workspace, method version, owner, and governance owner are required.");
        }
#pragma warning restore IDE0046

        return new OpportunityGraph
        {
            Id = id,
            WorkspaceId = workspaceId,
            MethodVersion = methodVersion,
            Owner = owner,
            GovernanceOwner = governanceOwner,
            Objectives = [.. objectives],
            Participants = [.. participants]
        };
    }

    public OpportunityGraph UpdateDetails(IEnumerable<string> objectives, IEnumerable<string> participants) =>
        Increment() with { Objectives = [.. objectives], Participants = [.. participants] };

    public OpportunityGraph AddWorkflow(Workflow workflow)
    {
        EnsureUnique(Workflows.Select(item => item.Id), workflow.Id, "workflow");
        return Increment() with { Workflows = [.. Workflows, workflow] };
    }

    public OpportunityGraph AddProblem(Problem problem)
    {
        EnsureUnique(Problems.Select(item => item.Id), problem.Id, "problem");
        EnsureExists(Workflows.Select(item => item.Id), problem.WorkflowId, "workflow");
        EnsureAllExist(Evidence.Select(item => item.Id), problem.EvidenceReferences, "evidence");
        return Increment() with { Problems = [.. Problems, problem] };
    }

    public OpportunityGraph AddPersona(Persona persona)
    {
        EnsureUnique(Personas.Select(item => item.Id), persona.Id, "persona");
        return Increment() with { Personas = [.. Personas, persona] };
    }

    public OpportunityGraph AddJourneyMap(JourneyMap journeyMap)
    {
        EnsureUnique(JourneyMaps.Select(item => item.Id), journeyMap.Id, "journey map");
        EnsureExists(Personas.Select(item => item.Id), journeyMap.PersonaId, "persona");
        if (journeyMap.WorkflowId is not null)
        {
            EnsureExists(Workflows.Select(item => item.Id), journeyMap.WorkflowId, "workflow");
        }

        var stepIds = journeyMap.Steps.Select(step => step.Id).ToArray();
        return stepIds.Distinct(StringComparer.Ordinal).Count() != stepIds.Length
            ? throw new DomainException(
                "journey_step.duplicate",
                "Journey step IDs must be unique within a journey map.")
            : Increment() with { JourneyMaps = [.. JourneyMaps, journeyMap] };
    }

    public OpportunityGraph AddCardShortlistEntry(CardShortlistEntry entry)
    {
        EnsureUnique(CardShortlist.Select(item => item.Id), entry.Id, "card shortlist entry");
        EnsureExists(
            JourneyMaps.SelectMany(map => map.Steps).Select(step => step.Id),
            entry.JourneyStepId,
            "journey step");
        return string.IsNullOrWhiteSpace(entry.DiscoveryCardId)
            ? throw new DomainException(
                "card_shortlist_entry.discovery_card_id_required",
                "A discovery card reference is required.")
            : Increment() with { CardShortlist = [.. CardShortlist, entry] };
    }

    public OpportunityGraph MarkCardShortlistSelection(string entryId, bool facilitatorSelected)
    {
        var entry = CardShortlist.SingleOrDefault(item => item.Id == entryId)
            ?? throw new DomainException(
                "card_shortlist_entry.not_found",
                "Card shortlist entry was not found.");

        return Increment() with
        {
            CardShortlist =
            [
                .. CardShortlist.Select(item =>
                    item.Id == entryId
                        ? item with { FacilitatorSelected = facilitatorSelected }
                        : item)
            ]
        };
    }

    public OpportunityGraph AddIdeationNote(IdeationNote note)
    {
        EnsureUnique(IdeationNotes.Select(item => item.Id), note.Id, "ideation note");
        return Increment() with { IdeationNotes = [.. IdeationNotes, note] };
    }

    public OpportunityGraph AddMultimodalAsset(MultimodalEvidenceAsset asset)
    {
        EnsureUnique(MultimodalAssets.Select(item => item.Id), asset.Id, "multimodal asset");

        // Three sequential guards; chaining them into ternaries nests throw-expressions inside
        // throw-expressions, which reads worse than the guard clauses it would replace.
#pragma warning disable IDE0046
        if (asset.ExtractionConfidence is < 0 or > 1)
        {
            throw new DomainException(
                "asset.confidence",
                "Multimodal extraction confidence must be between 0 and 1.");
        }

        if (asset.Modality is EvidenceModality.Transcript && asset.SpeakerSegments.Count == 0)
        {
            throw new DomainException(
                "asset.speaker_segments_required",
                "Transcript assets require speaker-attributed segments.");
        }

        if (asset.ExtractionConfidence < 0.80m && asset.ValidationStatus is ValidationStatus.Validated)
        {
            throw new DomainException(
                "asset.human_correction_required",
                "A multimodal extraction below 0.80 confidence requires human validation.");
        }
#pragma warning restore IDE0046

        return Increment() with { MultimodalAssets = [.. MultimodalAssets, asset] };
    }

    public OpportunityGraph AddEvidence(Evidence evidence)
    {
        EnsureUnique(Evidence.Select(item => item.Id), evidence.Id, "evidence");
        if (evidence.MultimodalAssetId is not null)
        {
            EnsureExists(
                MultimodalAssets.Select(item => item.Id),
                evidence.MultimodalAssetId,
                "multimodal asset");

            if (evidence.ValidationStatus is ValidationStatus.Validated)
            {
                var asset = MultimodalAssets.Single(item => item.Id == evidence.MultimodalAssetId);
                if (asset.ValidationStatus is not ValidationStatus.Validated)
                {
                    throw new DomainException(
                        "evidence.asset_not_validated",
                        "Validated transcript evidence must reference a validated multimodal asset.");
                }
            }
        }

        return Increment() with { Evidence = [.. Evidence, evidence] };
    }

    public OpportunityGraph CorrectEvidence(
        string evidenceId,
        string correctedStatement,
        string actor,
        DateTimeOffset correctedAt,
        string reason)
    {
        var evidence = Evidence.SingleOrDefault(item => item.Id == evidenceId)
            ?? throw new DomainException("evidence.not_found", "Evidence was not found.");
        var corrected = evidence.Correct(correctedStatement, actor, correctedAt, reason);

        return Increment() with
        {
            Evidence = [.. Evidence.Select(item => item.Id == evidenceId ? corrected : item)]
        };
    }

    public OpportunityGraph AddEvidenceConflict(EvidenceConflict conflict)
    {
        EnsureUnique(EvidenceConflicts.Select(item => item.Id), conflict.Id, "evidence conflict");
        EnsureExists(Evidence.Select(item => item.Id), conflict.FirstEvidenceId, "evidence");
        EnsureExists(Evidence.Select(item => item.Id), conflict.SecondEvidenceId, "evidence");

        return conflict.FirstEvidenceId == conflict.SecondEvidenceId
            ? throw new DomainException(
                "evidence.conflict_self_reference",
                "A conflict must link two different evidence records.")
            : Increment() with { EvidenceConflicts = [.. EvidenceConflicts, conflict] };
    }

    public OpportunityGraph AddOpportunity(Opportunity opportunity)
    {
        EnsureUnique(Opportunities.Select(item => item.Id), opportunity.Id, "opportunity");
        if (opportunity.LifecycleState is not EngagementLifecycle.Discovery)
        {
            throw new DomainException(
                "opportunity.initial_lifecycle_invalid",
                "New opportunities must begin in discovery.");
        }

        EnsureExists(Problems.Select(item => item.Id), opportunity.ProblemId, "problem");
        EnsureExists(Workflows.Select(item => item.Id), opportunity.WorkflowId, "workflow");
        EnsureAllExist(Evidence.Select(item => item.Id), opportunity.EvidenceReferences, "evidence");

        return Increment() with { Opportunities = [.. Opportunities, opportunity] };
    }

    public OpportunityGraph RecordDecision(DecisionRecord decision, GateEvaluation evaluation)
    {
        EnsureUnique(Decisions.Select(item => item.Id), decision.Id, "decision");
        var opportunity = Opportunities.SingleOrDefault(item => item.Id == decision.OpportunityId)
            ?? throw new DomainException("opportunity.not_found", "Opportunity was not found.");

        if (string.IsNullOrWhiteSpace(decision.Owner) ||
            string.IsNullOrWhiteSpace(decision.ApprovalPoint) ||
            string.IsNullOrWhiteSpace(decision.Rationale) ||
            string.IsNullOrWhiteSpace(decision.EscalationPath))
        {
            throw new DomainException(
                "decision.human_accountability_required",
                "A decision requires an owner, approval point, rationale, and escalation path.");
        }

        if (decision.PreviousState != opportunity.LifecycleState)
        {
            throw new DomainException(
                "decision.stale_state",
                "The decision was based on an outdated opportunity lifecycle state.");
        }

        if (decision.NewState is EngagementLifecycle.Pilot or EngagementLifecycle.ProductionReadiness &&
            evaluation.Status is GateStatus.Blocked)
        {
            throw new DomainException(
                "decision.gate_blocked",
                "Pilot and production-readiness progression is blocked until required controls pass.");
        }

        var updatedOpportunity = opportunity with
        {
            LifecycleState = decision.NewState,
            ObjectVersion = opportunity.ObjectVersion + 1
        };
        var next = Increment();

        return next with
        {
            LifecycleState = decision.NewState,
            Opportunities =
            [
                .. Opportunities.Select(item =>
                    item.Id == updatedOpportunity.Id ? updatedOpportunity : item)
            ],
            Decisions = [.. Decisions, decision with { ObjectVersion = next.ObjectVersion }]
        };
    }

    public OpportunityGraph RecordGateEvaluation(GateEvaluation evaluation)
    {
        EnsureExists(Opportunities.Select(item => item.Id), evaluation.OpportunityId, "opportunity");
        var next = Increment();
        var durableBlockers = evaluation.Blockers.Select(blocker =>
            blocker with { CanonicalGraphVersion = next.ObjectVersion });

        return next with
        {
            Blockers =
            [
                .. Blockers.Where(item => item.OpportunityId != evaluation.OpportunityId),
                .. durableBlockers
            ]
        };
    }

    public OpportunityGraph AddExperiment(Experiment experiment)
    {
        EnsureUnique(Experiments.Select(item => item.Id), experiment.Id, "experiment");
        EnsureExists(Opportunities.Select(item => item.Id), experiment.OpportunityId, "opportunity");
        EnsureAllExist(Evidence.Select(item => item.Id), experiment.EvidenceReferences, "evidence");
        return Increment() with { Experiments = [.. Experiments, experiment] };
    }

    public OpportunityGraph AddPilot(PilotRecord pilot)
    {
        EnsureUnique(Pilots.Select(item => item.Id), pilot.Id, "pilot");
        EnsureExists(Opportunities.Select(item => item.Id), pilot.OpportunityId, "opportunity");
        EnsureExists(Experiments.Select(item => item.Id), pilot.ExperimentId, "experiment");
        EnsureAllExist(Evidence.Select(item => item.Id), pilot.EvidenceReferences, "evidence");
        return Increment() with { Pilots = [.. Pilots, pilot] };
    }

    public OpportunityGraph AddOutcome(OutcomeRecord outcome)
    {
        EnsureUnique(Outcomes.Select(item => item.Id), outcome.Id, "outcome");
        EnsureExists(Opportunities.Select(item => item.Id), outcome.OpportunityId, "opportunity");
        EnsureExists(Pilots.Select(item => item.Id), outcome.PilotId, "pilot");
        EnsureAllExist(Evidence.Select(item => item.Id), outcome.EvidenceReferences, "evidence");
        return Increment() with { Outcomes = [.. Outcomes, outcome] };
    }

    private OpportunityGraph Increment() => this with { ObjectVersion = ObjectVersion + 1 };
    private static void EnsureUnique(
        IEnumerable<string> existingIds,
        string candidateId,
        string resourceName)
    {
        if (string.IsNullOrWhiteSpace(candidateId))
        {
            throw new DomainException(
                $"{resourceName}.id_required",
                $"{resourceName} ID is required.");
        }

        if (existingIds.Contains(candidateId, StringComparer.Ordinal))
        {
            throw new DomainException(
                $"{resourceName}.duplicate",
                $"A {resourceName} with that ID already exists.");
        }
    }

    private static void EnsureExists(
        IEnumerable<string> existingIds,
        string candidateId,
        string resourceName)
    {
        if (!existingIds.Contains(candidateId, StringComparer.Ordinal))
        {
            throw new DomainException(
                $"{resourceName}.not_found",
                $"Referenced {resourceName} was not found.");
        }
    }

    private static void EnsureAllExist(
        IEnumerable<string> existingIds,
        IEnumerable<string> candidateIds,
        string resourceName)
    {
        var known = existingIds.ToHashSet(StringComparer.Ordinal);
        if (candidateIds.Any(candidateId => !known.Contains(candidateId)))
        {
            throw new DomainException(
                $"{resourceName}.not_found",
                $"One or more referenced {resourceName} records were not found.");
        }
    }
}
