using OpportunityEngineering.Infrastructure.Fabric;

namespace OpportunityEngineering.ApiTests;

[TestClass]
public sealed class FabricGovernanceTests
{
    [TestMethod]
    public void EvaluateBlocksEnablementWhenPrerequisitesAreMissing()
    {
        var gate = new FabricGovernanceGate(new FabricGovernanceSettings
        {
            Requested = true,
            CapacityReady = true,
            TenantAiSettingsReady = false,
            WorkspaceIdentityReady = true,
            OneLakeSecurityReady = true,
            AuditReady = true,
            ApprovedDatasets = ["portfolio-projection"]
        });

        var report = gate.Evaluate();

        Assert.IsFalse(report.Enabled);
        Assert.IsTrue(report.Blockers.Any(item =>
            item.Contains("tenant", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void EvaluateRequiresDirectLakeFallbackVisibility()
    {
        var gate = new FabricGovernanceGate(new FabricGovernanceSettings
        {
            Requested = true,
            CapacityReady = true,
            TenantAiSettingsReady = true,
            WorkspaceIdentityReady = true,
            OneLakeSecurityReady = true,
            AuditReady = true,
            DirectLakeUsed = true,
            QueryModeObservation = "not-validated",
            ApprovedDatasets = ["portfolio-projection"]
        });

        var report = gate.Evaluate();

        Assert.IsFalse(report.Enabled);
        Assert.IsTrue(report.Blockers.Any(item =>
            item.Contains("Direct Lake", StringComparison.Ordinal)));
    }
}
