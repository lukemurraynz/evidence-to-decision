using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;

namespace OpportunityEngineering.Infrastructure.Messaging;

public sealed record ServiceBusSettings(
    string GraphEventsTopic,
    string RecommendationQueue,
    string ReviewSubscription);

public sealed class ServiceBusEventPublisher(
    ServiceBusClient client,
    ServiceBusSettings settings) : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusSender sender =
        client.CreateSender(settings.GraphEventsTopic);

    public async Task PublishAsync(
        GraphChangedEvent graphEvent,
        CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(graphEvent))
        {
            MessageId = graphEvent.EventId,
            SessionId = graphEvent.WorkspaceId,
            CorrelationId = graphEvent.CorrelationId,
            ContentType = "application/json",
            Subject = graphEvent.EventType
        };
        message.ApplicationProperties["schemaVersion"] = graphEvent.SchemaVersion;
        message.ApplicationProperties["canonicalGraphVersion"] =
            graphEvent.CanonicalGraphVersion;

        await sender.SendMessageAsync(message, cancellationToken);
    }

    public ValueTask DisposeAsync() => sender.DisposeAsync();
}

public sealed class ServiceBusRecommendationPublisher(
    ServiceBusClient client,
    ServiceBusSettings settings) : IAsyncDisposable
{
    private readonly ServiceBusSender sender =
        client.CreateSender(settings.RecommendationQueue);

    public async Task PublishAsync(
        RecommendationWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var message = CreateMessage(workItem, 0);

        await sender.SendMessageAsync(message, cancellationToken);
    }

    public async Task ScheduleRetryAsync(
        RecommendationWorkItem workItem,
        int retryAttempt,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        var message = CreateMessage(workItem, retryAttempt);
        message.MessageId = $"{workItem.OperationId}:retry:{retryAttempt}";
        await sender.ScheduleMessageAsync(
            message,
            DateTimeOffset.UtcNow.Add(delay),
            cancellationToken);
    }

    public ValueTask DisposeAsync() => sender.DisposeAsync();

    private static ServiceBusMessage CreateMessage(
        RecommendationWorkItem workItem,
        int retryAttempt)
    {
        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(workItem))
        {
            MessageId = workItem.OperationId,
            SessionId = workItem.WorkspaceId,
            CorrelationId = workItem.CorrelationId,
            ContentType = "application/json",
            Subject = "recommendation.requested"
        };
        message.ApplicationProperties["retryAttempt"] = retryAttempt;
        return message;
    }
}
