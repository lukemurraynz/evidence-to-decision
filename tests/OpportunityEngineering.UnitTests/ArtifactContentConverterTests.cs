using Newtonsoft.Json;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.UnitTests;

[TestClass]
public sealed class ArtifactContentConverterTests
{
    [TestMethod]
    public void RoundTripsEachArtifactContentThroughNewtonsoft()
    {
        var decision = new DecisionRecord
        {
            Id = "decision-1",
            OpportunityId = "opportunity-1",
            PreviousState = EngagementLifecycle.Discovery,
            NewState = EngagementLifecycle.Validation,
            DecisionClass = DecisionClass.Validate,
            Rationale = "rationale",
            Owner = "owner",
            ApprovalPoint = "approval-point",
            EscalationPath = "escalation-path",
            Timestamp = DateTimeOffset.UtcNow,
        };
        IReadOnlyList<IArtifactContent> samples =
        [
            new DecisionRecordContent("owner", "outcome", decision, []),
            new ExecutiveSummaryContent(
                "problem",
                "outcome",
                "value",
                "confidence",
                EngagementLifecycle.Discovery,
                decision),
            new ExperimentDefinitionContent("hypothesis", "criteria", "outcome", [], [], "owner"),
        ];

        foreach (var sample in samples)
        {
            var json = JsonConvert.SerializeObject(sample);
            var roundTripped = JsonConvert.DeserializeObject<IArtifactContent>(json);

            Assert.IsNotNull(roundTripped);
            Assert.AreEqual(sample.GetType(), roundTripped.GetType());
        }
    }
}
