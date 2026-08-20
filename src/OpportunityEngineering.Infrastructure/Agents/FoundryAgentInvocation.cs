using System.Diagnostics;
using System.Text.Json;
using Azure;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Infrastructure.Agents;

/// <summary>
/// Shared transient-failure retry and token-usage telemetry for every Foundry agent call.
/// Safe to retry blindly here: these agents are read-only advisory calls with no side effects
/// on canonical state, unlike a mutating tool call. Conservative attempt count because each
/// retry re-sends the full prompt and burns tokens/quota.
/// </summary>
internal static class FoundryAgentInvocation
{
    private const int MaxAttempts = 3;

    public static async Task<T> RunAsync<T>(
        ChatClientAgent agent,
        string request,
        JsonSerializerOptions serializerOptions,
        Activity? activity,
        Func<DomainException> onEmptyResult,
        CancellationToken cancellationToken,
        ChatClientAgentRunOptions? options = null)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var response = await agent.RunAsync<T>(
                    request,
                    serializerOptions: serializerOptions,
                    options: options,
                    cancellationToken: cancellationToken);
                RecordUsage(activity, response.Usage);
                return response.Result ?? throw onEmptyResult();
            }
            catch (RequestFailedException exception) when (
                attempt < MaxAttempts && IsTransient(exception.Status))
            {
                await Task.Delay(BackoffDelay(attempt), cancellationToken);
            }
        }
    }

    private static bool IsTransient(int status) =>
        status is 429 or 500 or 502 or 503 or 504;

    private static TimeSpan BackoffDelay(int attempt) =>
        TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250));

    private static void RecordUsage(Activity? activity, UsageDetails? usage)
    {
        if (usage is null || activity is null)
        {
            return;
        }

        activity.SetTag("gen_ai.usage.input_tokens", usage.InputTokenCount);
        activity.SetTag("gen_ai.usage.output_tokens", usage.OutputTokenCount);
        activity.SetTag("gen_ai.usage.total_tokens", usage.TotalTokenCount);
    }
}
