using OpportunityEngineering.Application.Contracts;

namespace OpportunityEngineering.Infrastructure.Fabric;

public sealed record FabricGovernanceSettings
{
    public bool Requested { get; init; }
    public bool CapacityReady { get; init; }
    public bool TenantAiSettingsReady { get; init; }
    public bool WorkspaceIdentityReady { get; init; }
    public bool OneLakeSecurityReady { get; init; }
    public bool AuditReady { get; init; }
    public bool DirectLakeUsed { get; init; }
    public string QueryModeObservation { get; init; } = "not-validated";
    public IReadOnlyList<string> ApprovedDatasets { get; init; } = [];
}

public sealed class FabricGovernanceGate(FabricGovernanceSettings settings)
{
    public FabricReadinessReport Evaluate()
    {
        var blockers = new List<string>();
        AddBlocker(!settings.Requested, "Fabric enablement was not requested.");
        AddBlocker(!settings.CapacityReady, "Fabric capacity readiness is not validated.");
        AddBlocker(
            !settings.TenantAiSettingsReady,
            "Required Fabric tenant AI settings are not validated.");
        AddBlocker(
            !settings.WorkspaceIdentityReady,
            "Fabric workspace identity posture is not validated.");
        AddBlocker(
            !settings.OneLakeSecurityReady,
            "OneLake workspace security boundaries are not validated.");
        AddBlocker(!settings.AuditReady, "Fabric query auditing is not validated.");
        AddBlocker(
            settings.ApprovedDatasets.Count == 0,
            "No derived analytics datasets are approved.");
        AddBlocker(
            settings.DirectLakeUsed &&
            settings.QueryModeObservation is not ("direct-lake" or "fallback-observed"),
            "Direct Lake query-mode fallback visibility is not validated.");

        return new FabricReadinessReport(
            settings.Requested,
            blockers.Count == 0,
            settings.CapacityReady,
            settings.TenantAiSettingsReady,
            settings.WorkspaceIdentityReady,
            settings.OneLakeSecurityReady,
            settings.AuditReady,
            settings.DirectLakeUsed,
            settings.QueryModeObservation,
            settings.ApprovedDatasets,
            blockers);

        void AddBlocker(bool condition, string blocker)
        {
            if (condition)
            {
                blockers.Add(blocker);
            }
        }
    }
}
