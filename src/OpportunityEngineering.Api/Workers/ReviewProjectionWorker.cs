using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Infrastructure.Messaging;

namespace OpportunityEngineering.Api.Workers;

public sealed class ReviewProjectionWorker(
    ServiceBusClient client,
    ServiceBusSettings settings,
    ReplaySafeReviewConsumer consumer,
    ILogger<ReviewProjectionWorker> logger) : IHostedService, IAsyncDisposable
{
    private static readonly Action<ILogger, string?, Exception?> LogInvalidMessage =
        LoggerMessage.Define<string?>(
            LogLevel.Warning,
            new EventId(3001, nameof(LogInvalidMessage)),
            "Dead-lettered invalid graph event {MessageId}.");

    private static readonly Action<ILogger, string, Exception?> LogProcessorFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3002, nameof(LogProcessorFailure)),
            "Service Bus review projection processor failed in {ErrorSource}.");

    private readonly ServiceBusSessionProcessor processor =
        client.CreateSessionProcessor(
            settings.GraphEventsTopic,
            settings.ReviewSubscription,
            new ServiceBusSessionProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentSessions = 10,
                MaxConcurrentCallsPerSession = 1
            });

    public Task StartAsync(CancellationToken cancellationToken)
    {
        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;
        return processor.StartProcessingAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        processor.StopProcessingAsync(cancellationToken);

    // DisposeAsync tears down the processor's own event subscriptions along with
    // everything else; unsubscribing here first is redundant, and the SDK's processor
    // throws if a handler is removed while the processor never fully finished starting
    // (e.g. the background connection attempt failed before startup completed).
    public ValueTask DisposeAsync() => processor.DisposeAsync();

    private async Task ProcessMessageAsync(ProcessSessionMessageEventArgs arguments)
    {
        GraphChangedEvent? graphEvent;
        try
        {
            graphEvent = arguments.Message.Body.ToObjectFromJson<GraphChangedEvent>();
        }
        catch (JsonException exception)
        {
            await arguments.DeadLetterMessageAsync(
                arguments.Message,
                "invalid-message",
                "The graph event was not valid JSON.",
                arguments.CancellationToken);
            LogInvalidMessage(logger, arguments.Message.MessageId, exception);
            return;
        }

        if (graphEvent is null ||
            !string.Equals(
                graphEvent.WorkspaceId,
                arguments.Message.SessionId,
                StringComparison.Ordinal))
        {
            await arguments.DeadLetterMessageAsync(
                arguments.Message,
                "invalid-workspace-session",
                "The event workspace did not match its ordered session.",
                arguments.CancellationToken);
            return;
        }

        await consumer.ConsumeAsync(graphEvent, arguments.CancellationToken);
        await arguments.CompleteMessageAsync(
            arguments.Message,
            arguments.CancellationToken);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs arguments)
    {
        LogProcessorFailure(
            logger,
            arguments.ErrorSource.ToString(),
            arguments.Exception);
        return Task.CompletedTask;
    }
}
