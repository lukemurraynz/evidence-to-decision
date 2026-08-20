using System.Text.Json;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Infrastructure.Policy;

public sealed record GuardrailPolicy
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public required string Version { get; init; }
    public required DateTimeOffset EffectiveAt { get; init; }
    public required string Mode { get; init; }
    public required string ApprovedBy { get; init; }
    public required string RollbackReference { get; init; }
    public required IReadOnlyList<string> AllowedEvaluationPoints { get; init; }
    public required IReadOnlyList<string> AllowedTools { get; init; }

    public static GuardrailPolicy Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new InvalidOperationException("The guardrail policy bundle is missing.");
        }

        GuardrailPolicy? policy;
        try
        {
            policy = JsonSerializer.Deserialize<GuardrailPolicy>(
                File.ReadAllText(path),
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The guardrail policy bundle is invalid.",
                exception);
        }

        return policy is null ||
            string.IsNullOrWhiteSpace(policy.Version) ||
            string.IsNullOrWhiteSpace(policy.ApprovedBy) ||
            string.IsNullOrWhiteSpace(policy.RollbackReference) ||
            policy.AllowedEvaluationPoints.Count == 0 ||
            policy.Mode is not ("evaluation-only" or "enforce")
                ? throw new InvalidOperationException(
                    "The guardrail policy bundle is empty or incomplete.")
                : policy;
    }
}

public sealed class GuardrailPolicyEvaluator(GuardrailPolicy policy) : IAgentPolicyEvaluator
{
    public Task<PolicyDecision> EvaluateAsync(
        ActorContext actor,
        string evaluationPoint,
        string? toolName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Three sequential guards; chaining them into ternaries would nest three levels deep,
        // which reads worse than the guard clauses it would replace.
#pragma warning disable IDE0046
        if (string.IsNullOrWhiteSpace(actor.ActorId) ||
            string.IsNullOrWhiteSpace(actor.WorkspaceId) ||
            string.IsNullOrWhiteSpace(actor.CorrelationId))
        {
            return Task.FromResult(Deny("The authenticated policy context is incomplete."));
        }

        if (!policy.AllowedEvaluationPoints.Contains(
                evaluationPoint,
                StringComparer.Ordinal))
        {
            return Task.FromResult(PolicyViolation(
                $"Evaluation point '{evaluationPoint}' is not permitted."));
        }

        if (toolName is not null && !policy.AllowedTools.Contains(toolName, StringComparer.Ordinal))
        {
            return Task.FromResult(PolicyViolation($"Tool '{toolName}' is not permitted."));
        }
#pragma warning restore IDE0046

        return Task.FromResult(
            new PolicyDecision(PolicyVerdict.Allow, policy.Version, "Policy permitted the action."));
    }

    private PolicyDecision PolicyViolation(string reason) =>
        policy.Mode == "evaluation-only"
            ? new PolicyDecision(PolicyVerdict.Warn, policy.Version, reason)
            : Deny(reason);

    private PolicyDecision Deny(string reason) =>
        new(PolicyVerdict.Deny, policy.Version, reason);
}
