using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

internal static class TestData
{
    public static readonly DateTimeOffset Now =
        new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    public static OpportunityGraph CreateGraph(bool gatesPass = true)
    {
        var graph = OpportunityGraph.Create(
            "engagement-1",
            "workspace-1",
            "1.0",
            "owner-1",
            "governance-owner-1",
            ["Reduce handling time"],
            ["participant-1"]);
        graph = graph.AddWorkflow(new Workflow(
            "workflow-1",
            "request received",
            ["advisor"],
            ["request"],
            ["review"],
            ["approve"],
            ["crm"],
            ["handoff"],
            ["missing data"],
            ["decision"]));
        graph = graph.AddEvidence(Evidence.Capture(
            "evidence-1",
            EvidenceType.CustomerStatement,
            "Current handling is slow.",
            "interview-1",
            Now,
            EvidenceModality.Text,
            0.95m,
            ValidationStatus.Validated,
            "participant-1",
            null,
            null));
        graph = graph.AddProblem(new Problem(
            "problem-1",
            "workflow-1",
            "advisor",
            "respond faster",
            "manual triage",
            "long handling time",
            ["evidence-1"],
            0.9m));
        graph = graph.AddOpportunity(CreateOpportunity(gatesPass));
        return graph;
    }

    public static Opportunity CreateOpportunity(bool gatesPass = true) =>
        new()
        {
            Id = "opportunity-1",
            ProblemId = "problem-1",
            WorkflowId = "workflow-1",
            DesiredOutcome = "Reduce handling time",
            KpiReference = "median handling time",
            Owner = "owner-1",
            ValueProfile = "high",
            ConfidenceProfile = "supported",
            EvidenceReferences = ["evidence-1"],
            TrustProfile = new TrustProfile(
                gatesPass,
                gatesPass,
                gatesPass,
                gatesPass,
                "internal",
                "durable",
                "moderate",
                "moderate"),
            ReadinessProfile = new ReadinessProfile(
                gatesPass,
                gatesPass,
                gatesPass,
                gatesPass,
                gatesPass,
                gatesPass,
                gatesPass,
                gatesPass)
        };

    public static ActorContext Facilitator() =>
        new("actor-1", "workspace-1", new HashSet<ApplicationRole> { ApplicationRole.Facilitator }, "correlation-1");

    public static ActorContext Reviewer() =>
        new("reviewer-1", "workspace-1", new HashSet<ApplicationRole> { ApplicationRole.Reviewer }, "correlation-1");

    public static ActorContext FacilitatorAndReviewer() =>
        new(
            "dual-role-1",
            "workspace-1",
            new HashSet<ApplicationRole> { ApplicationRole.Facilitator, ApplicationRole.Reviewer },
            "correlation-1");
}

internal sealed class FixedTimeProvider : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => TestData.Now;
}
