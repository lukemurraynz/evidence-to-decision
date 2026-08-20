namespace OpportunityEngineering.Application.Contracts;

public sealed record FabricReadinessReport(
    bool Requested,
    bool Enabled,
    bool CapacityReady,
    bool TenantAiSettingsReady,
    bool WorkspaceIdentityReady,
    bool OneLakeSecurityReady,
    bool AuditReady,
    bool DirectLakeUsed,
    string QueryModeObservation,
    IReadOnlyList<string> ApprovedDatasets,
    IReadOnlyList<string> Blockers);
