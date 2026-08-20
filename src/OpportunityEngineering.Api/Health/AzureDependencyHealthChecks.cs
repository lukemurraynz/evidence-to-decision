using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpportunityEngineering.Infrastructure.Messaging;

namespace OpportunityEngineering.Api.Health;

public sealed class CosmosHealthCheck(Container container) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await container.ReadContainerAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "Cosmos DB is unavailable.",
                exception);
        }
    }
}

public sealed class ServiceBusHealthCheck(
    ServiceBusClient client,
    ServiceBusSettings settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var sender = client.CreateSender(settings.RecommendationQueue);
            using var batch = await sender.CreateMessageBatchAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "Service Bus is unavailable.",
                exception);
        }
    }
}
