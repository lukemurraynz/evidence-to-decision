using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Application.Services;

/// <summary>Evaluates consequential progression from canonical state without mutating it.</summary>
public sealed class GateEvaluator(TimeProvider timeProvider)
{
    public GateEvaluation Evaluate(
        Opportunity opportunity,
        string evaluator,
        long graphVersion,
        Func<string> createId)
    {
        var blockers = new List<GovernanceBlocker>();
        AddIfMissing(
            opportunity.ReadinessProfile.OwnerDefined && !string.IsNullOrWhiteSpace(opportunity.Owner),
            BlockerCategory.Owner,
            "An accountable owner is required.");
        AddIfMissing(
            opportunity.ReadinessProfile.KpiDefined && !string.IsNullOrWhiteSpace(opportunity.KpiReference),
            BlockerCategory.Kpi,
            "A measurable KPI is required.");
        AddIfMissing(
            opportunity.ReadinessProfile.BaselineDefined,
            BlockerCategory.Baseline,
            "A KPI baseline is required.");
        AddIfMissing(
            opportunity.ReadinessProfile.TargetDefined,
            BlockerCategory.Target,
            "A KPI target is required.");
        AddIfMissing(
            opportunity.TrustProfile.PrivacyApproved,
            BlockerCategory.Privacy,
            "Privacy controls require approval.");
        AddIfMissing(
            opportunity.TrustProfile.SecurityApproved,
            BlockerCategory.Security,
            "Security controls require approval.");
        AddIfMissing(
            opportunity.TrustProfile.GovernanceApproved,
            BlockerCategory.Governance,
            "Governance controls require approval.");
        AddIfMissing(
            opportunity.ReadinessProfile.DataReady,
            BlockerCategory.Data,
            "Required data is not ready.");
        AddIfMissing(
            opportunity.ReadinessProfile.IntegrationReady,
            BlockerCategory.Integration,
            "Required integration is not ready.");
        AddIfMissing(
            opportunity.TrustProfile.HumanOversightDefined,
            BlockerCategory.Oversight,
            "Human oversight is not defined.");

        return new GateEvaluation(
            opportunity.Id,
            blockers.Count == 0 ? GateStatus.Passed : GateStatus.Blocked,
            blockers,
            evaluator,
            timeProvider.GetUtcNow(),
            graphVersion);

        void AddIfMissing(bool satisfied, BlockerCategory category, string rationale)
        {
            if (!satisfied)
            {
                blockers.Add(new GovernanceBlocker(
                    createId(),
                    opportunity.Id,
                    category,
                    rationale,
                    evaluator,
                    timeProvider.GetUtcNow(),
                    $"Resolve the {category.ToString().ToLowerInvariant()} prerequisite and reevaluate.",
                    graphVersion));
            }
        }
    }
}
