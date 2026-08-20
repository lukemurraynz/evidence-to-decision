using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;
using OpportunityEngineering.Infrastructure.Policy;

namespace OpportunityEngineering.ApiTests;

[TestClass]
public sealed class GuardrailPolicyTests
{
    [TestMethod]
    public void LoadFailsClosedWhenBundleIsMissing()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
            GuardrailPolicy.Load(Path.Combine(
                Path.GetTempPath(),
                Guid.CreateVersion7().ToString())));
    }

    [TestMethod]
    public async Task EvaluateDeniesUnknownActionInEnforceMode()
    {
        var evaluator = new GuardrailPolicyEvaluator(new GuardrailPolicy
        {
            Version = "1.0",
            EffectiveAt = DateTimeOffset.Parse(
                "2026-08-16T00:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture),
            Mode = "enforce",
            ApprovedBy = "operator-1",
            RollbackReference = "policy-previous",
            AllowedEvaluationPoints = ["recommendation.submit"],
            AllowedTools = ["foundry-recommendation"]
        });

        var result = await evaluator.EvaluateAsync(
            new ActorContext(
                "actor-1",
                "workspace-1",
                new HashSet<ApplicationRole> { ApplicationRole.Facilitator },
                "correlation-1"),
            "unknown.action",
            null,
            CancellationToken.None);

        Assert.AreEqual(PolicyVerdict.Deny, result.Verdict);
        Assert.IsFalse(result.Permitted);
    }

    [TestMethod]
    public async Task EvaluateWarnsDuringEvaluationOnlyPromotionStage()
    {
        var evaluator = new GuardrailPolicyEvaluator(new GuardrailPolicy
        {
            Version = "1.0-evaluation",
            EffectiveAt = DateTimeOffset.Parse(
                "2026-08-16T00:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture),
            Mode = "evaluation-only",
            ApprovedBy = "operator-1",
            RollbackReference = "policy-previous",
            AllowedEvaluationPoints = ["recommendation.submit"],
            AllowedTools = ["foundry-recommendation"]
        });

        var result = await evaluator.EvaluateAsync(
            new ActorContext(
                "actor-1",
                "workspace-1",
                new HashSet<ApplicationRole> { ApplicationRole.Facilitator },
                "correlation-1"),
            "unknown.action",
            null,
            CancellationToken.None);

        Assert.AreEqual(PolicyVerdict.Warn, result.Verdict);
        Assert.IsTrue(result.Permitted);
    }
}
