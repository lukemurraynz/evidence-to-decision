using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class ApplicationServiceTests
{
    [TestMethod]
    public async Task AddWorkflowAsyncRejectsReviewerCanonicalMutation()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.AddWorkflowAsync(
                TestData.Reviewer(),
                "engagement-1",
                new Workflow(
                    "workflow-2",
                    "trigger",
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    []),
                store.Graph.ObjectVersion,
                CancellationToken.None));

        Assert.AreEqual("authorization.canonical_mutation_denied", exception.Code);
        Assert.AreEqual(TestData.CreateGraph().ObjectVersion, store.Graph.ObjectVersion);
    }

    // There is no separate Admin role. A facilitator already needs full authority over the
    // engagements they run, so a Facilitator alone (no Reviewer role) can record a decision
    // just as Admin always could. Reviewer still exists independently for when an actual
    // second person approves; this isn't a rejection test because that restriction no longer
    // exists by design.
    [TestMethod]
    public async Task RecordDecisionAsyncAllowsFacilitatorApproval()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var decision = CreateDecision(store.Graph);

        var updated = await service.RecordDecisionAsync(
            TestData.Facilitator(),
            store.Graph.Id,
            decision,
            store.Graph.ObjectVersion,
            CancellationToken.None);

        Assert.IsTrue(updated.Decisions.Any(recorded => recorded.Id == decision.Id));
    }

    // Regression test for the group-membership lockout this replaced: resolving a
    // multi-group principal to a single "highest privilege" role meant a facilitator
    // who also reviews lost their mutation rights entirely (Reviewer outranked
    // Facilitator). ActorContext now grants the union of matched roles, so a dual-role
    // actor keeps both capabilities.
    [TestMethod]
    public async Task DualRoleActorCanBothMutateTheGraphAndRecordADecision()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var actor = TestData.FacilitatorAndReviewer();

        var afterMutation = await service.AddWorkflowAsync(
            actor,
            "engagement-1",
            new Workflow("workflow-2", "trigger", [], [], [], [], [], [], [], []),
            store.Graph.ObjectVersion,
            CancellationToken.None);

        Assert.IsTrue(afterMutation.Workflows.Any(workflow => workflow.Id == "workflow-2"));

        var decision = CreateDecision(afterMutation);
        var afterDecision = await service.RecordDecisionAsync(
            actor,
            afterMutation.Id,
            decision,
            afterMutation.ObjectVersion,
            CancellationToken.None);

        Assert.IsTrue(afterDecision.Decisions.Any(recorded => recorded.Id == decision.Id));
    }

    [TestMethod]
    public async Task UpdateEngagementDetailsAsyncReplacesObjectivesAndParticipants()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));

        var updated = await service.UpdateEngagementDetailsAsync(
            TestData.Facilitator(),
            "engagement-1",
            ["Updated objective"],
            ["Updated participant"],
            store.Graph.ObjectVersion,
            CancellationToken.None);

        Assert.HasCount(1, updated.Objectives);
        Assert.AreEqual("Updated objective", updated.Objectives[0]);
        Assert.HasCount(1, updated.Participants);
        Assert.AreEqual("Updated participant", updated.Participants[0]);
    }

    [TestMethod]
    public async Task UpdateEngagementDetailsAsyncRejectsReviewerCanonicalMutation()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.UpdateEngagementDetailsAsync(
                TestData.Reviewer(),
                "engagement-1",
                ["Updated objective"],
                ["Updated participant"],
                store.Graph.ObjectVersion,
                CancellationToken.None));

        Assert.AreEqual("authorization.canonical_mutation_denied", exception.Code);
    }

    [TestMethod]
    public async Task AddPersonaAsyncRejectsReviewerCanonicalMutation()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.AddPersonaAsync(
                TestData.Reviewer(),
                "engagement-1",
                new Persona("persona-1", "Alex", "Claims advisor", [], [], []),
                store.Graph.ObjectVersion,
                CancellationToken.None));

        Assert.AreEqual("authorization.canonical_mutation_denied", exception.Code);
    }

    [TestMethod]
    public async Task PersonaJourneyMapAndCardShortlistFlowSucceeds()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var actor = TestData.Facilitator();

        var afterPersona = await service.AddPersonaAsync(
            actor,
            "engagement-1",
            new Persona(
                "persona-1",
                "Alex",
                "Claims advisor",
                ["Close cases faster"],
                ["Re-keying data from intake"],
                ["Handles 20+ cases a day"]),
            store.Graph.ObjectVersion,
            CancellationToken.None);
        Assert.IsTrue(afterPersona.Personas.Any(persona => persona.Id == "persona-1"));

        var afterJourneyMap = await service.AddJourneyMapAsync(
            actor,
            "engagement-1",
            new JourneyMap(
                "journey-map-1",
                "persona-1",
                "workflow-1",
                [
                    new JourneyStep(
                        "step-1",
                        1,
                        "Receive the claim",
                        "Data is re-keyed from the intake form",
                        "Auto-populate from intake",
                        "Minutes to first action")
                ]),
            afterPersona.ObjectVersion,
            CancellationToken.None);
        Assert.IsTrue(afterJourneyMap.JourneyMaps.Any(map => map.Id == "journey-map-1"));

        var afterShortlist = await service.AddCardShortlistEntryAsync(
            actor,
            "engagement-1",
            new CardShortlistEntry(
                "shortlist-1",
                "step-1",
                "navigation-and-control-automate-home-operations",
                "Automating intake data entry removes the re-keying pain point.",
                1,
                false),
            afterJourneyMap.ObjectVersion,
            CancellationToken.None);
        var shortlisted = afterShortlist.CardShortlist.Single(entry => entry.Id == "shortlist-1");
        Assert.IsFalse(shortlisted.FacilitatorSelected);

        var afterSelection = await service.MarkCardShortlistSelectionAsync(
            actor,
            "engagement-1",
            "shortlist-1",
            true,
            afterShortlist.ObjectVersion,
            CancellationToken.None);
        Assert.IsTrue(
            afterSelection.CardShortlist.Single(entry => entry.Id == "shortlist-1").FacilitatorSelected);
    }

    [TestMethod]
    public async Task AddJourneyMapAsyncRejectsUnknownPersona()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.AddJourneyMapAsync(
                TestData.Facilitator(),
                "engagement-1",
                new JourneyMap("journey-map-1", "missing-persona", null, []),
                store.Graph.ObjectVersion,
                CancellationToken.None));

        Assert.AreEqual("persona.not_found", exception.Code);
    }

    [TestMethod]
    public async Task SubmitAsyncReusesOperationForSameIdempotentRequest()
    {
        var operations = new InMemoryOperationStore();
        var service = new RecommendationSubmissionService(
            operations,
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var first = await service.SubmitAsync(
            TestData.Facilitator(),
            "engagement-1",
            "opportunity-1",
            "request-1",
            "fingerprint",
            CancellationToken.None);
        var second = await service.SubmitAsync(
            TestData.Facilitator(),
            "engagement-1",
            "opportunity-1",
            "request-1",
            "fingerprint",
            CancellationToken.None);

        Assert.AreEqual(first.Id, second.Id);
        Assert.AreEqual(1, operations.CreateCount);
    }

    [TestMethod]
    public async Task SubmitAsyncRejectsReusedKeyForDifferentRequest()
    {
        var operations = new InMemoryOperationStore();
        var service = new RecommendationSubmissionService(
            operations,
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());
        await service.SubmitAsync(
            TestData.Facilitator(),
            "engagement-1",
            "opportunity-1",
            "request-1",
            "first",
            CancellationToken.None);

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.SubmitAsync(
                TestData.Facilitator(),
                "engagement-1",
                "opportunity-1",
                "request-1",
                "different",
                CancellationToken.None));

        Assert.AreEqual("operation.idempotency_conflict", exception.Code);
    }

    [TestMethod]
    public async Task SubmitAsyncDeniesAndAuditsPolicyFailure()
    {
        var audit = new InMemoryAuditSink();
        var service = new RecommendationSubmissionService(
            new InMemoryOperationStore(),
            new FixedPolicyEvaluator(PolicyVerdict.Deny),
            audit,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.SubmitAsync(
                TestData.Facilitator(),
                "engagement-1",
                "opportunity-1",
                "request-1",
                "fingerprint",
                CancellationToken.None));

        Assert.AreEqual("policy.recommendation_denied", exception.Code);
        Assert.HasCount(1, audit.Records);
        Assert.AreEqual(PolicyVerdict.Deny, audit.Records[0].Verdict);
    }

    [TestMethod]
    public async Task ReviewConsumerIgnoresDuplicateAndRereadsCurrentGraph()
    {
        var graph = TestData.CreateGraph();
        var claims = new InMemoryClaimStore();
        var projections = new InMemoryProjectionStore();
        var consumer = new ReplaySafeReviewConsumer(
            claims,
            new InMemoryGraphStore(graph),
            projections,
            new GateEvaluator(new FixedTimeProvider()),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());
        var oldEvent = new GraphChangedEvent(
            "event-1",
            "OpportunityCreated",
            graph.Id,
            graph.WorkspaceId,
            graph.ObjectVersion - 1,
            "actor-1",
            TestData.Now,
            "correlation-1");

        await consumer.ConsumeAsync(oldEvent, CancellationToken.None);
        await consumer.ConsumeAsync(oldEvent, CancellationToken.None);

        Assert.AreEqual(1, projections.ReviewSaveCount);
        Assert.AreEqual(graph.ObjectVersion, projections.LastReview?.CanonicalGraphVersion);
        Assert.HasCount(1, projections.Notifications);
        Assert.AreEqual(
            ReviewAttentionReason.NewOpportunity,
            projections.Notifications[0].Reason);
        Assert.HasCount(1, claims.Results);
    }

    [TestMethod]
    public async Task ReviewConsumerReevaluatesCurrentControlsWithoutMutatingGraph()
    {
        var graph = TestData.CreateGraph(gatesPass: false);
        var originalVersion = graph.ObjectVersion;
        var projections = new InMemoryProjectionStore();
        var consumer = new ReplaySafeReviewConsumer(
            new InMemoryClaimStore(),
            new InMemoryGraphStore(graph),
            projections,
            new GateEvaluator(new FixedTimeProvider()),
            new SequentialIdentifierFactory(),
            new FixedTimeProvider());
        var graphEvent = new GraphChangedEvent(
            "event-2",
            "EvidenceConflictDetected",
            graph.Id,
            graph.WorkspaceId,
            graph.ObjectVersion,
            "actor-1",
            TestData.Now,
            "correlation-2");

        await consumer.ConsumeAsync(graphEvent, CancellationToken.None);

        Assert.AreEqual(originalVersion, graph.ObjectVersion);
        Assert.IsNotNull(projections.LastReview);
        Assert.IsTrue(projections.LastReview.Blockers.Count > 0);
        Assert.IsTrue(projections.LastReview.Blockers.All(
            blocker => blocker.CanonicalGraphVersion == originalVersion));
        Assert.AreEqual(
            ReviewAttentionReason.EvidenceConflict,
            projections.Notifications.Single().Reason);
    }

    [TestMethod]
    public async Task RecordDecisionAsyncPersistsReviewerOverrideAccountability()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var decision = CreateDecision(store.Graph) with
        {
            Rationale = "Reviewer override after validating the source evidence.",
            ApprovalPoint = "governance-review",
            EscalationPath = "executive-sponsor"
        };

        var updated = await service.RecordDecisionAsync(
            TestData.Reviewer(),
            store.Graph.Id,
            decision,
            store.Graph.ObjectVersion,
            CancellationToken.None);

        var recorded = updated.Decisions.Single();
        Assert.AreEqual(TestData.Reviewer().ActorId, recorded.Owner);
        Assert.AreEqual("governance-review", recorded.ApprovalPoint);
        Assert.AreEqual("executive-sponsor", recorded.EscalationPath);
        Assert.AreEqual(EngagementLifecycle.Validation, updated.LifecycleState);
    }

    [TestMethod]
    public async Task DeleteEngagementAsyncRequiresFacilitator()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));

        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.DeleteEngagementAsync(
                TestData.Reviewer(),
                store.Graph.Id,
                $"DELETE {store.Graph.Id}",
                store.Graph.ObjectVersion,
                CancellationToken.None));

        Assert.AreEqual("authorization.deletion_denied", exception.Code);
    }

    [TestMethod]
    public async Task DeleteEngagementAsyncRequiresExactConfirmation()
    {
        var store = new InMemoryGraphStore(TestData.CreateGraph());
        var service = new GraphCommandService(
            store,
            new SequentialIdentifierFactory(),
            new FixedTimeProvider(),
            new GateEvaluator(new FixedTimeProvider()));
        var exception = await Assert.ThrowsExactlyAsync<DomainException>(() =>
            service.DeleteEngagementAsync(
                TestData.Facilitator(),
                store.Graph.Id,
                "DELETE wrong-engagement",
                store.Graph.ObjectVersion,
                CancellationToken.None));

        Assert.AreEqual("engagement.deletion_confirmation_required", exception.Code);
    }

    [TestMethod]
    public async Task GenerateArtifactAsyncAttachesTheAgentNarrativeWhenGenerationSucceeds()
    {
        var graph = DecidedGraphForArtifactTests();
        var service = new GraphQueryService(
            new InMemoryGraphStore(graph),
            new InMemoryProjectionStore(),
            new InMemoryActivityAuditSink(),
            new SequentialIdentifierFactory(),
            new ProjectionFactory(new FixedTimeProvider()),
            new FakeArtifactNarrativeAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new FixedTimeProvider());

        var artifact = await service.GenerateArtifactAsync(
            TestData.Facilitator(), "engagement-1", "opportunity-1", ArtifactType.DecisionRecord, CancellationToken.None);

        Assert.IsNotNull(artifact.NarrativeSummary);
        Assert.AreEqual("Test narrative.", artifact.NarrativeSummary.Summary);
    }

    [TestMethod]
    public async Task GenerateArtifactAsyncStillReturnsTheArtifactWhenNarrativeGenerationFails()
    {
        var graph = DecidedGraphForArtifactTests();
        var activityAudit = new InMemoryActivityAuditSink();
        var service = new GraphQueryService(
            new InMemoryGraphStore(graph),
            new InMemoryProjectionStore(),
            activityAudit,
            new SequentialIdentifierFactory(),
            new ProjectionFactory(new FixedTimeProvider()),
            new ThrowingArtifactNarrativeAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new FixedTimeProvider());

        var artifact = await service.GenerateArtifactAsync(
            TestData.Facilitator(), "engagement-1", "opportunity-1", ArtifactType.DecisionRecord, CancellationToken.None);

        Assert.IsNull(artifact.NarrativeSummary);
        Assert.IsTrue(activityAudit.Records.Any(record => record.Action == "artifact.narrative_failed"));
    }

    [TestMethod]
    public async Task GenerateArtifactAsyncSkipsTheNarrativeWhenPolicyDeniesTheModelCall()
    {
        var graph = DecidedGraphForArtifactTests();
        var agent = new FakeArtifactNarrativeAgent();
        var service = new GraphQueryService(
            new InMemoryGraphStore(graph),
            new InMemoryProjectionStore(),
            new InMemoryActivityAuditSink(),
            new SequentialIdentifierFactory(),
            new ProjectionFactory(new FixedTimeProvider()),
            agent,
            new FixedPolicyEvaluator(PolicyVerdict.Deny),
            new InMemoryAuditSink(),
            new FixedTimeProvider());

        var artifact = await service.GenerateArtifactAsync(
            TestData.Facilitator(), "engagement-1", "opportunity-1", ArtifactType.DecisionRecord, CancellationToken.None);

        Assert.IsNull(artifact.NarrativeSummary);
    }

    private static OpportunityGraph DecidedGraphForArtifactTests()
    {
        var graph = TestData.CreateGraph();
        var evaluation = new GateEvaluator(new FixedTimeProvider()).Evaluate(
            graph.Opportunities.Single(),
            "reviewer-1",
            graph.ObjectVersion,
            () => "blocker");
        return graph.RecordDecision(CreateDecision(graph), evaluation);
    }

    [TestMethod]
    public async Task AnalyticsProjectionRetainsCanonicalSourceVersions()
    {
        var graph = TestData.CreateGraph();
        var evaluation = new GateEvaluator(new FixedTimeProvider()).Evaluate(
            graph.Opportunities.Single(),
            "reviewer-1",
            graph.ObjectVersion,
            () => "blocker");
        graph = graph.RecordDecision(CreateDecision(graph), evaluation);
        var projections = new InMemoryProjectionStore();
        var audit = new InMemoryActivityAuditSink();
        var service = new GraphQueryService(
            new InMemoryGraphStore(graph),
            projections,
            audit,
            new SequentialIdentifierFactory(),
            new ProjectionFactory(new FixedTimeProvider()),
            new FakeArtifactNarrativeAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new FixedTimeProvider());

        var result = await service.GenerateWorkspaceAnalyticsAsync(
            TestData.Reviewer(),
            TestData.Now.AddMinutes(-1),
            TestData.Now.AddMinutes(1),
            CancellationToken.None);

        Assert.IsTrue(result.SourceGraphVersions.Contains(
            $"{graph.Id}:v{graph.ObjectVersion}",
            StringComparer.Ordinal));
        Assert.IsTrue(result.Metrics.All(metric =>
            metric.SourceGraphVersions.SequenceEqual(result.SourceGraphVersions)));
        Assert.HasCount(1, audit.Records);
        Assert.AreEqual("analytics.generate", audit.Records[0].Action);
    }

    [TestMethod]
    public async Task ListEngagementsAsyncScopesToTheActorWorkspace()
    {
        var graph = TestData.CreateGraph();
        var service = new GraphQueryService(
            new InMemoryGraphStore(graph),
            new InMemoryProjectionStore(),
            new InMemoryActivityAuditSink(),
            new SequentialIdentifierFactory(),
            new ProjectionFactory(new FixedTimeProvider()),
            new FakeArtifactNarrativeAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new FixedTimeProvider());

        var sameWorkspace = await service.ListEngagementsAsync(
            TestData.Facilitator() with { WorkspaceId = graph.WorkspaceId },
            CancellationToken.None);
        var otherWorkspace = await service.ListEngagementsAsync(
            TestData.Facilitator() with { WorkspaceId = "workspace-other" },
            CancellationToken.None);

        Assert.HasCount(1, sameWorkspace);
        Assert.AreEqual(graph.Id, sameWorkspace[0].Id);
        Assert.HasCount(0, otherWorkspace);
    }

    [TestMethod]
    public async Task GetReviewerNotificationsAsyncAllowsReviewerAndScopesToWorkspace()
    {
        var projections = new InMemoryProjectionStore();
        projections.Notifications.Add(new ReviewerNotification(
            "notif-1",
            "workspace-1",
            "engagement-1",
            "opportunity-1",
            ReviewAttentionReason.NewOpportunity,
            "A new opportunity is ready for review.",
            1,
            TestData.Now,
            "correlation-1"));
        projections.Notifications.Add(new ReviewerNotification(
            "notif-2",
            "workspace-other",
            "engagement-2",
            "opportunity-2",
            ReviewAttentionReason.NewOpportunity,
            "A new opportunity is ready for review.",
            1,
            TestData.Now,
            "correlation-2"));
        var service = new GraphQueryService(
            new InMemoryGraphStore(TestData.CreateGraph()),
            projections,
            new InMemoryActivityAuditSink(),
            new SequentialIdentifierFactory(),
            new ProjectionFactory(new FixedTimeProvider()),
            new FakeArtifactNarrativeAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new FixedTimeProvider());

        var page = await service.GetReviewerNotificationsAsync(
            TestData.Reviewer(),
            50,
            null,
            CancellationToken.None);

        Assert.HasCount(1, page.Items);
        Assert.AreEqual("workspace-1", page.Items[0].WorkspaceId);
    }

    [TestMethod]
    public async Task GetReviewerNotificationsAsyncAllowsFacilitator()
    {
        var projections = new InMemoryProjectionStore();
        var service = new GraphQueryService(
            new InMemoryGraphStore(TestData.CreateGraph()),
            projections,
            new InMemoryActivityAuditSink(),
            new SequentialIdentifierFactory(),
            new ProjectionFactory(new FixedTimeProvider()),
            new FakeArtifactNarrativeAgent(),
            new FixedPolicyEvaluator(PolicyVerdict.Allow),
            new InMemoryAuditSink(),
            new FixedTimeProvider());

        var page = await service.GetReviewerNotificationsAsync(
            TestData.Facilitator(),
            50,
            null,
            CancellationToken.None);

        Assert.IsNotNull(page);
    }

    private static DecisionRecord CreateDecision(OpportunityGraph graph) =>
        new()
        {
            Id = "decision-1",
            OpportunityId = "opportunity-1",
            PreviousState = EngagementLifecycle.Discovery,
            NewState = EngagementLifecycle.Validation,
            DecisionClass = DecisionClass.Validate,
            Rationale = "Proceed to validation.",
            Owner = "reviewer-1",
            ApprovalPoint = "review meeting",
            EscalationPath = "governance owner",
            Timestamp = TestData.Now,
            ObjectVersion = graph.ObjectVersion
        };
}

internal sealed class InMemoryGraphStore(OpportunityGraph graph) : IOpportunityGraphStore
{
    public OpportunityGraph Graph { get; private set; } = graph;

    public Task<OpportunityGraph?> GetAsync(
        string workspaceId,
        string engagementId,
        CancellationToken cancellationToken) =>
        Task.FromResult<OpportunityGraph?>(
            Graph.WorkspaceId == workspaceId && Graph.Id == engagementId ? Graph : null);

    public Task<IReadOnlyList<OpportunityGraph>> QueryWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OpportunityGraph>>(
            Graph.WorkspaceId == workspaceId ? [Graph] : []);

    public Task<IReadOnlyList<OpportunityGraph>> QueryWorkspaceInWindowAsync(
        string workspaceId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OpportunityGraph>>(
            Graph.WorkspaceId == workspaceId &&
            Graph.Decisions.Any(d => d.Timestamp >= windowStart && d.Timestamp < windowEnd)
                ? [Graph]
                : []);

    public Task CreateAsync(
        OpportunityGraph graphValue,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken)
    {
        Graph = graphValue;
        return Task.CompletedTask;
    }

    public Task ReplaceAsync(
        OpportunityGraph graphValue,
        long expectedVersion,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken)
    {
        if (Graph.ObjectVersion != expectedVersion)
        {
            throw new DomainException("graph.version_conflict", "Version conflict.");
        }

        Graph = graphValue;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        OpportunityGraph graphValue,
        long expectedVersion,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken)
    {
        return Graph.ObjectVersion != expectedVersion
            ? throw new DomainException("graph.version_conflict", "Version conflict.")
            : Task.CompletedTask;
    }
}

internal sealed class SequentialIdentifierFactory : IIdentifierFactory
{
    private int value;

    public string Create() =>
        Interlocked.Increment(ref value).ToString(
            System.Globalization.CultureInfo.InvariantCulture);
}

internal sealed class FakeArtifactNarrativeAgent : IArtifactNarrativeAgent
{
    public Task<ArtifactNarrative> SummarizeAsync(
        OpportunityGraph graph,
        Opportunity opportunity,
        ArtifactType artifactType,
        IArtifactContent content,
        ActorContext actor,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ArtifactNarrative("Test narrative.", "Review before use.", "test-model", TestData.Now));
}

internal sealed class ThrowingArtifactNarrativeAgent : IArtifactNarrativeAgent
{
    public Task<ArtifactNarrative> SummarizeAsync(
        OpportunityGraph graph,
        Opportunity opportunity,
        ArtifactType artifactType,
        IArtifactContent content,
        ActorContext actor,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Simulated Foundry outage.");
}

internal sealed class FixedPolicyEvaluator(PolicyVerdict verdict) : IAgentPolicyEvaluator
{
    public Task<PolicyDecision> EvaluateAsync(
        ActorContext actor,
        string evaluationPoint,
        string? toolName,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PolicyDecision(verdict, "policy-1", "test verdict"));
}

internal sealed class InMemoryAuditSink : IAppendOnlyAuditSink
{
    public List<PolicyAuditRecord> Records { get; } = [];

    public Task AppendAsync(
        PolicyAuditRecord record,
        CancellationToken cancellationToken)
    {
        Records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PolicyAuditRecord>> QueryAsync(
        string workspaceId,
        string correlationId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PolicyAuditRecord>>(
            [.. Records.Where(item =>
                item.WorkspaceId == workspaceId &&
                item.CorrelationId == correlationId)]);
}

internal sealed class InMemoryOperationStore : IDurableOperationStore
{
    private readonly Dictionary<string, DurableOperation> byId = [];
    private readonly Dictionary<string, DurableOperation> byKey = [];

    public int CreateCount { get; private set; }

    public Task<DurableOperation> CreateAsync(
        DurableOperation operation,
        RecommendationWorkItem workItem,
        CancellationToken cancellationToken)
    {
        CreateCount++;
        byId.Add(operation.Id, operation);
        byKey.Add(operation.IdempotencyKey, operation);
        return Task.FromResult(operation);
    }

    public Task<DurableOperation?> GetAsync(
        string workspaceId,
        string operationId,
        CancellationToken cancellationToken) =>
        Task.FromResult(
            byId.TryGetValue(operationId, out var operation) &&
            operation.WorkspaceId == workspaceId
                ? operation
                : null);

    public Task<DurableOperation?> GetByIdempotencyKeyAsync(
        string workspaceId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        Task.FromResult(
            byKey.TryGetValue(idempotencyKey, out var operation) &&
            operation.WorkspaceId == workspaceId
                ? operation
                : null);

    public Task UpdateAsync(
        DurableOperation operation,
        CancellationToken cancellationToken)
    {
        byId[operation.Id] = operation;
        byKey[operation.IdempotencyKey] = operation;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryClaimStore : IEventConsumerClaimStore
{
    private readonly HashSet<string> claims = [];

    public List<ConsumerResult> Results { get; } = [];

    public Task<bool> TryClaimAsync(
        string workspaceId,
        string eventId,
        string consumerName,
        CancellationToken cancellationToken) =>
        Task.FromResult(claims.Add($"{workspaceId}:{eventId}:{consumerName}"));

    public Task CompleteAsync(
        ConsumerResult result,
        CancellationToken cancellationToken)
    {
        Results.Add(result);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(
        string workspaceId,
        string eventId,
        string consumerName,
        CancellationToken cancellationToken)
    {
        claims.Remove($"{workspaceId}:{eventId}:{consumerName}");
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryProjectionStore : IProjectionStore
{
    public int ReviewSaveCount { get; private set; }
    public OpportunityReviewProjection? LastReview { get; private set; }
    public List<ReviewerNotification> Notifications { get; } = [];

    public Task SaveRecommendationAsync(
        string workspaceId,
        OpportunityRecommendation recommendation,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<OpportunityRecommendation?> GetRecommendationAsync(
        string workspaceId,
        string recommendationId,
        CancellationToken cancellationToken) =>
        Task.FromResult<OpportunityRecommendation?>(null);

    public Task SaveArtifactAsync(
        string workspaceId,
        ArtifactEnvelope artifact,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<ArtifactEnvelope?> GetArtifactAsync(
        string workspaceId,
        string artifactId,
        CancellationToken cancellationToken) =>
        Task.FromResult<ArtifactEnvelope?>(null);

    public Task SaveAnalyticsAsync(
        string workspaceId,
        PortfolioAnalyticsProjection projection,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task SaveReviewAsync(
        string workspaceId,
        OpportunityReviewProjection review,
        CancellationToken cancellationToken)
    {
        ReviewSaveCount++;
        LastReview = review;
        return Task.CompletedTask;
    }

    public Task SaveReviewerNotificationAsync(
        string workspaceId,
        ReviewerNotification notification,
        CancellationToken cancellationToken)
    {
        Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task<ReviewerNotificationsPage> QueryReviewerNotificationsAsync(
        string workspaceId,
        int pageSize,
        string? continuationToken,
        CancellationToken cancellationToken)
    {
        var items = Notifications.Where(item => item.WorkspaceId == workspaceId).Take(pageSize).ToList();
        return Task.FromResult(new ReviewerNotificationsPage(items, null));
    }

    public Task DeleteReviewsByEngagementAsync(
        string workspaceId,
        string engagementId,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class InMemoryActivityAuditSink : IActivityAuditSink
{
    public List<AuditRecord> Records { get; } = [];

    public Task AppendAsync(
        AuditRecord record,
        CancellationToken cancellationToken)
    {
        Records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditRecord>> QueryAsync(
        string workspaceId,
        string correlationId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AuditRecord>>(
            [.. Records.Where(item =>
                item.WorkspaceId == workspaceId &&
                item.CorrelationId == correlationId)]);
}
