using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>Executes authorized human mutations against the canonical graph.</summary>
public sealed class GraphCommandService(
    IOpportunityGraphStore graphStore,
    IIdentifierFactory identifiers,
    TimeProvider timeProvider,
    GateEvaluator gateEvaluator)
{
    public async Task<OpportunityGraph> CreateEngagementAsync(
        ActorContext actor,
        string engagementId,
        string methodVersion,
        string owner,
        string governanceOwner,
        IReadOnlyList<string> objectives,
        IReadOnlyList<string> participants,
        CancellationToken cancellationToken)
    {
        RequireFacilitatorMutation(actor);
        var graph = OpportunityGraph.Create(
            engagementId,
            actor.WorkspaceId,
            methodVersion,
            owner,
            governanceOwner,
            objectives,
            participants);
        var domainEvent = CreateEvent("EngagementCreated", graph, actor);
        var audit = CreateAudit("engagement.create", graph.Id, "recorded", "Created engagement.", graph, actor);

        await graphStore.CreateAsync(graph, domainEvent, audit, cancellationToken);
        return graph;
    }

    public Task<OpportunityGraph> UpdateEngagementDetailsAsync(
        ActorContext actor,
        string engagementId,
        IReadOnlyList<string> objectives,
        IReadOnlyList<string> participants,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "EngagementDetailsChanged",
            "engagement.update_details",
            engagementId,
            graph => graph.UpdateDetails(objectives, participants),
            cancellationToken);

    public Task<OpportunityGraph> AddWorkflowAsync(
        ActorContext actor,
        string engagementId,
        Workflow workflow,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "WorkflowChanged",
            "workflow.create",
            workflow.Id,
            graph => graph.AddWorkflow(workflow),
            cancellationToken);

    public Task<OpportunityGraph> AddProblemAsync(
        ActorContext actor,
        string engagementId,
        Problem problem,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "ProblemChanged",
            "problem.create",
            problem.Id,
            graph => graph.AddProblem(problem),
            cancellationToken);

    public Task<OpportunityGraph> AddPersonaAsync(
        ActorContext actor,
        string engagementId,
        Persona persona,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "PersonaChanged",
            "persona.create",
            persona.Id,
            graph => graph.AddPersona(persona),
            cancellationToken);

    public Task<OpportunityGraph> AddJourneyMapAsync(
        ActorContext actor,
        string engagementId,
        JourneyMap journeyMap,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "JourneyMapChanged",
            "journey_map.create",
            journeyMap.Id,
            graph => graph.AddJourneyMap(journeyMap),
            cancellationToken);

    public Task<OpportunityGraph> AddCardShortlistEntryAsync(
        ActorContext actor,
        string engagementId,
        CardShortlistEntry entry,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "CardShortlistEntryChanged",
            "card_shortlist_entry.create",
            entry.Id,
            graph => graph.AddCardShortlistEntry(entry),
            cancellationToken);

    public Task<OpportunityGraph> AddIdeationNoteAsync(
        ActorContext actor,
        string engagementId,
        IdeationNote note,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "IdeationNoteChanged",
            "ideation_note.create",
            note.Id,
            graph => graph.AddIdeationNote(note),
            cancellationToken);

    public Task<OpportunityGraph> MarkCardShortlistSelectionAsync(
        ActorContext actor,
        string engagementId,
        string entryId,
        bool facilitatorSelected,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "CardShortlistEntryChanged",
            "card_shortlist_entry.selection",
            entryId,
            graph => graph.MarkCardShortlistSelection(entryId, facilitatorSelected),
            cancellationToken);

    public Task<OpportunityGraph> AddMultimodalAssetAsync(
        ActorContext actor,
        string engagementId,
        MultimodalEvidenceAsset asset,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "MultimodalAssetAdded",
            "multimodal-asset.create",
            asset.Id,
            graph => graph.AddMultimodalAsset(asset),
            cancellationToken);

    public Task<OpportunityGraph> AddEvidenceAsync(
        ActorContext actor,
        string engagementId,
        Evidence evidence,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "EvidenceAdded",
            "evidence.create",
            evidence.Id,
            graph => graph.AddEvidence(evidence),
            cancellationToken);

    public Task<OpportunityGraph> CorrectEvidenceAsync(
        ActorContext actor,
        string engagementId,
        string evidenceId,
        string correctedStatement,
        string reason,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "EvidenceCorrected",
            "evidence.correct",
            evidenceId,
            graph => graph.CorrectEvidence(
                evidenceId,
                correctedStatement,
                actor.ActorId,
                timeProvider.GetUtcNow(),
                reason),
            cancellationToken);

    public Task<OpportunityGraph> AddConflictAsync(
        ActorContext actor,
        string engagementId,
        EvidenceConflict conflict,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "EvidenceConflictDetected",
            "evidence-conflict.create",
            conflict.Id,
            graph => graph.AddEvidenceConflict(conflict),
            cancellationToken);

    public Task<OpportunityGraph> AddOpportunityAsync(
        ActorContext actor,
        string engagementId,
        Opportunity opportunity,
        long expectedVersion,
        CancellationToken cancellationToken) =>
        MutateAsync(
            actor,
            engagementId,
            expectedVersion,
            "OpportunityCreated",
            "opportunity.create",
            opportunity.Id,
            graph => graph.AddOpportunity(opportunity),
            cancellationToken,
            opportunity.Id);

    public async Task<GateEvaluation> EvaluateGatesAsync(
        ActorContext actor,
        string engagementId,
        string opportunityId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        RequireReview(actor);
        var graph = await GetRequiredAsync(actor, engagementId, cancellationToken);
        EnsureExpectedVersion(graph, expectedVersion);
        var opportunity = graph.Opportunities.SingleOrDefault(item => item.Id == opportunityId)
            ?? throw new DomainException("opportunity.not_found", "Opportunity was not found.");
        var evaluation = gateEvaluator.Evaluate(
            opportunity,
            actor.ActorId,
            graph.ObjectVersion,
            identifiers.Create);
        var updated = graph.RecordGateEvaluation(evaluation);

        await graphStore.ReplaceAsync(
            updated,
            expectedVersion,
            CreateEvent("GateEvaluationChanged", updated, actor, opportunityId),
            CreateAudit(
                "gate.evaluate",
                opportunityId,
                evaluation.Status.ToString().ToLowerInvariant(),
                $"{evaluation.Blockers.Count} blocker(s) recorded.",
                updated,
                actor),
            cancellationToken);

        return evaluation with
        {
            EvaluatedGraphVersion = updated.ObjectVersion,
            Blockers = [.. evaluation.Blockers.Select(b => b with { CanonicalGraphVersion = updated.ObjectVersion })]
        };
    }

    public async Task<OpportunityGraph> RecordDecisionAsync(
        ActorContext actor,
        string engagementId,
        DecisionRecord decision,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        RequireReview(actor);
        var graph = await GetRequiredAsync(actor, engagementId, cancellationToken);
        EnsureExpectedVersion(graph, expectedVersion);
        var opportunity = graph.Opportunities.SingleOrDefault(item => item.Id == decision.OpportunityId)
            ?? throw new DomainException("opportunity.not_found", "Opportunity was not found.");
        var evaluation = gateEvaluator.Evaluate(
            opportunity,
            actor.ActorId,
            graph.ObjectVersion,
            identifiers.Create);
        var updated = graph.RecordDecision(decision, evaluation);

        await graphStore.ReplaceAsync(
            updated,
            expectedVersion,
            CreateEvent("DecisionChanged", updated, actor, decision.OpportunityId),
            CreateAudit(
                "decision.record",
                decision.Id,
                "recorded",
                decision.Rationale,
                updated,
                actor),
            cancellationToken);

        return updated;
    }

    public async Task DeleteEngagementAsync(
        ActorContext actor,
        string engagementId,
        string typedConfirmation,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        if (!actor.Has(ApplicationRole.Facilitator))
        {
            throw new DomainException(
                "authorization.deletion_denied",
                "Only a facilitator can delete an engagement.");
        }

        if (!string.Equals(
                typedConfirmation,
                $"DELETE {engagementId}",
                StringComparison.Ordinal))
        {
            throw new DomainException(
                "engagement.deletion_confirmation_required",
                "The typed engagement deletion confirmation did not match.");
        }

        var graph = await GetRequiredAsync(actor, engagementId, cancellationToken);
        EnsureExpectedVersion(graph, expectedVersion);
        var deletedVersion = graph.ObjectVersion + 1;
        await graphStore.DeleteAsync(
            graph,
            expectedVersion,
            new GraphChangedEvent(
                identifiers.Create(),
                "EngagementDeleted",
                graph.Id,
                graph.WorkspaceId,
                deletedVersion,
                actor.ActorId,
                timeProvider.GetUtcNow(),
                actor.CorrelationId),
            new AuditRecord(
                identifiers.Create(),
                graph.WorkspaceId,
                actor.ActorId,
                "engagement.delete",
                graph.Id,
                "deleted",
                "Authorized engagement deletion completed.",
                timeProvider.GetUtcNow(),
                actor.CorrelationId,
                deletedVersion),
            cancellationToken);
    }

    private async Task<OpportunityGraph> MutateAsync(
        ActorContext actor,
        string engagementId,
        long expectedVersion,
        string eventType,
        string action,
        string targetId,
        Func<OpportunityGraph, OpportunityGraph> mutation,
        CancellationToken cancellationToken,
        string? affectedOpportunityId = null)
    {
        RequireFacilitatorMutation(actor);
        var graph = await GetRequiredAsync(actor, engagementId, cancellationToken);
        EnsureExpectedVersion(graph, expectedVersion);
        var updated = mutation(graph);

        await graphStore.ReplaceAsync(
            updated,
            expectedVersion,
            CreateEvent(eventType, updated, actor, affectedOpportunityId),
            CreateAudit(action, targetId, "recorded", "Canonical graph changed.", updated, actor),
            cancellationToken);

        return updated;
    }

    private async Task<OpportunityGraph> GetRequiredAsync(
        ActorContext actor,
        string engagementId,
        CancellationToken cancellationToken)
    {
        return await graphStore.GetAsync(actor.WorkspaceId, engagementId, cancellationToken)
            ?? throw new DomainException("engagement.not_found", "Engagement was not found.");
    }

    private GraphChangedEvent CreateEvent(
        string eventType,
        OpportunityGraph graph,
        ActorContext actor,
        string? affectedOpportunityId = null) =>
        new(
            identifiers.Create(),
            eventType,
            graph.Id,
            graph.WorkspaceId,
            graph.ObjectVersion,
            actor.ActorId,
            timeProvider.GetUtcNow(),
            actor.CorrelationId,
            affectedOpportunityId);

    private AuditRecord CreateAudit(
        string action,
        string targetId,
        string result,
        string reason,
        OpportunityGraph graph,
        ActorContext actor) =>
        new(
            identifiers.Create(),
            graph.WorkspaceId,
            actor.ActorId,
            action,
            targetId,
            result,
            reason,
            timeProvider.GetUtcNow(),
            actor.CorrelationId,
            graph.ObjectVersion);

    private static void EnsureExpectedVersion(OpportunityGraph graph, long expectedVersion)
    {
        if (graph.ObjectVersion != expectedVersion)
        {
            throw new DomainException(
                "graph.version_conflict",
                "The canonical graph changed. Reread it before retrying.");
        }
    }

    private static void RequireFacilitatorMutation(ActorContext actor)
    {
        if (!actor.Has(ApplicationRole.Facilitator))
        {
            throw new DomainException(
                "authorization.canonical_mutation_denied",
                "Reviewers cannot mutate the canonical graph.");
        }
    }

    // A facilitator can also approve. There's no separate admin tier to grant that
    // independently of either role, so a dual-role actor (see TestData.FacilitatorAndReviewer)
    // is the only way to hold both capabilities.
    private static void RequireReview(ActorContext actor)
    {
        if (!actor.Has(ApplicationRole.Reviewer) && !actor.Has(ApplicationRole.Facilitator))
        {
            throw new DomainException(
                "authorization.approval_denied",
                "Only reviewers and facilitators can approve decisions or governance overrides.");
        }
    }
}
