using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class OpportunityGraphTests
{
    [TestMethod]
    public void CaptureRejectsValidatedLowConfidenceTranscript()
    {
        var exception = Assert.ThrowsExactly<DomainException>(() =>
            Evidence.Capture(
                "evidence-1",
                EvidenceType.CustomerStatement,
                "Transcript text",
                "recording-1",
                TestData.Now,
                EvidenceModality.Transcript,
                0.79m,
                ValidationStatus.Validated,
                "participant-1",
                null,
                "asset-1"));

        Assert.AreEqual("evidence.human_correction_required", exception.Code);
    }

    [TestMethod]
    public void UpdateDetailsReplacesObjectivesAndParticipantsAndIncrementsVersion()
    {
        var graph = TestData.CreateGraph();

        var updated = graph.UpdateDetails(["New objective"], ["New participant"]);

        Assert.HasCount(1, updated.Objectives);
        Assert.AreEqual("New objective", updated.Objectives[0]);
        Assert.HasCount(1, updated.Participants);
        Assert.AreEqual("New participant", updated.Participants[0]);
        Assert.AreEqual(graph.ObjectVersion + 1, updated.ObjectVersion);
    }

    [TestMethod]
    public void CorrectEvidencePreservesOriginalAndAppendsRevision()
    {
        var graph = TestData.CreateGraph();

        var corrected = graph.CorrectEvidence(
            "evidence-1",
            "Corrected wording.",
            "actor-1",
            TestData.Now,
            "Participant correction.");
        var evidence = corrected.Evidence.Single();

        Assert.AreEqual("Current handling is slow.", evidence.Statement);
        Assert.AreEqual("Corrected wording.", evidence.EffectiveStatement);
        Assert.HasCount(1, evidence.Revisions);
        Assert.AreEqual(graph.ObjectVersion + 1, corrected.ObjectVersion);
    }

    [TestMethod]
    public void AddEvidenceConflictPreservesBothClaims()
    {
        var graph = TestData.CreateGraph();
        graph = graph.AddEvidence(Evidence.Capture(
            "evidence-2",
            EvidenceType.CustomerStatement,
            "Current handling is fast.",
            "interview-2",
            TestData.Now,
            EvidenceModality.Text,
            0.8m,
            ValidationStatus.NeedsCorrection,
            "participant-2",
            null,
            null));

        var updated = graph.AddEvidenceConflict(new EvidenceConflict(
            "conflict-1",
            "evidence-1",
            "evidence-2",
            "handling time",
            "Validate against measurement",
            TestData.Now,
            "actor-1"));

        Assert.HasCount(2, updated.Evidence);
        Assert.HasCount(1, updated.EvidenceConflicts);
    }

    [TestMethod]
    public void AddIdeationNoteAppendsTheNoteAndIncrementsVersion()
    {
        var graph = TestData.CreateGraph();

        var updated = graph.AddIdeationNote(new IdeationNote("note-1", "Skip the re-keying step entirely.", "Riley", TestData.Now));

        Assert.HasCount(1, updated.IdeationNotes);
        Assert.AreEqual("Skip the re-keying step entirely.", updated.IdeationNotes[0].Text);
        Assert.AreEqual(graph.ObjectVersion + 1, updated.ObjectVersion);
    }

    [TestMethod]
    public void AddIdeationNoteRejectsADuplicateId()
    {
        var graph = TestData.CreateGraph().AddIdeationNote(
            new IdeationNote("note-1", "First idea.", "Riley", TestData.Now));

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            graph.AddIdeationNote(new IdeationNote("note-1", "Second idea.", "Sam", TestData.Now)));

        Assert.AreEqual("ideation note.duplicate", exception.Code);
    }

    [TestMethod]
    public void GateEvaluatorRecordsMissingPrerequisitesAsBlockers()
    {
        var evaluator = new GateEvaluator(new FixedTimeProvider());

        var result = evaluator.Evaluate(
            TestData.CreateOpportunity(gatesPass: false),
            "reviewer-1",
            4,
            () => Guid.CreateVersion7().ToString());

        Assert.AreEqual(GateStatus.Blocked, result.Status);
        Assert.IsTrue(result.Blockers.Any(item => item.Category == BlockerCategory.Privacy));
        Assert.IsTrue(result.Blockers.Any(item => item.Category == BlockerCategory.Owner));
        Assert.IsTrue(result.Blockers.All(item => item.CanonicalGraphVersion == 4));
    }

    [TestMethod]
    public void ApplyStalenessMarksArtifactStaleAfterGraphChange()
    {
        var artifact = new OpportunityEngineering.Application.Contracts.ArtifactEnvelope(
            "artifact-1",
            ArtifactType.PilotBrief,
            "engagement-1",
            "opportunity-1",
            5,
            "1.0",
            [],
            [],
            TestData.Now,
            "actor-1",
            StalenessStatus.Current,
            new ArchitectureHandoffContent(
                "problem",
                "workflow",
                ["advisor"],
                "outcome",
                "kpi",
                "baseline",
                "target",
                [],
                TestData.CreateOpportunity().TrustProfile,
                [],
                [],
                "owner-1",
                new DecisionRecord
                {
                    Id = "decision-1",
                    OpportunityId = "opportunity-1",
                    PreviousState = EngagementLifecycle.Discovery,
                    NewState = EngagementLifecycle.Validation,
                    DecisionClass = DecisionClass.Validate,
                    Rationale = "Validate.",
                    Owner = "reviewer-1",
                    ApprovalPoint = "review",
                    EscalationPath = "governance",
                    Timestamp = TestData.Now,
                    ObjectVersion = 5
                }));

        var result = ProjectionFactory.ApplyStaleness(artifact, 6);

        Assert.AreEqual(StalenessStatus.Stale, result.Staleness);
    }
}
