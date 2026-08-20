using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Domain;
using OpportunityEngineering.Infrastructure.Messaging;

namespace OpportunityEngineering.Api.Workers;

public sealed class RecommendationWorker(
    ServiceBusClient client,
    ServiceBusSettings settings,
    ServiceBusRecommendationPublisher publisher,
    RecommendationExecutionService execution,
    IDurableOperationStore operations,
    TimeProvider timeProvider,
    ILogger<RecommendationWorker> logger) : IHostedService, IAsyncDisposable
{
    private static readonly Action<ILogger, string?, Exception?> LogInvalidMessage =
        LoggerMessage.Define<string?>(
            LogLevel.Warning,
            new EventId(2001, nameof(LogInvalidMessage)),
            "Dead-lettered invalid recommendation message {MessageId}.");

    private static readonly Action<ILogger, string, int, Exception?> LogOperationFailure =
        LoggerMessage.Define<string, int>(
            LogLevel.Error,
            new EventId(2002, nameof(LogOperationFailure)),
            "Recommendation operation {OperationId} failed on retry {RetryAttempt}.");

    private static readonly Action<ILogger, string, Exception?> LogProcessorFailure =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2003, nameof(LogProcessorFailure)),
            "Service Bus recommendation processor failed in {ErrorSource}.");

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120),
        TimeSpan.FromSeconds(240)
    ];

    private readonly ServiceBusSessionProcessor processor =
        client.CreateSessionProcessor(
            settings.RecommendationQueue,
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
        RecommendationWorkItem? workItem;
        try
        {
            workItem = arguments.Message.Body.ToObjectFromJson<RecommendationWorkItem>();
        }
        catch (JsonException exception)
        {
            await arguments.DeadLetterMessageAsync(
                arguments.Message,
                "invalid-message",
                "The recommendation work item was not valid JSON.",
                arguments.CancellationToken);
            LogInvalidMessage(logger, arguments.Message.MessageId, exception);
            return;
        }

        if (workItem is null)
        {
            await arguments.DeadLetterMessageAsync(
                arguments.Message,
                "invalid-message",
                "The recommendation work item was empty.",
                arguments.CancellationToken);
            return;
        }

        try
        {
            await execution.ExecuteAsync(workItem, arguments.CancellationToken);
            await arguments.CompleteMessageAsync(
                arguments.Message,
                arguments.CancellationToken);
        }
        catch (OperationCanceledException) when (arguments.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DomainException domainException)
        {
            // Permanent domain failures cannot recover with retries; dead-letter immediately.
            var operation = await operations.GetAsync(
                workItem.WorkspaceId,
                workItem.OperationId,
                arguments.CancellationToken);
            if (operation is not null)
            {
                await operations.UpdateAsync(
                    operation with
                    {
                        Status = OperationStatus.Failed,
                        UpdatedAt = timeProvider.GetUtcNow(),
                        ErrorCode = domainException.Code,
                        ErrorDetail = domainException.Message
                    },
                    arguments.CancellationToken);
            }

            await arguments.DeadLetterMessageAsync(
                arguments.Message,
                "domain-error",
                domainException.Message,
                arguments.CancellationToken);
            LogOperationFailure(logger, workItem.OperationId, 0, domainException);
        }
        catch (Exception exception)
        {
            var retryAttempt = arguments.Message.ApplicationProperties.TryGetValue(
                "retryAttempt",
                out var value)
                ? Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture)
                : 0;
            if (retryAttempt < RetryDelays.Length)
            {
                await publisher.ScheduleRetryAsync(
                    workItem,
                    retryAttempt + 1,
                    RetryDelays[retryAttempt],
                    arguments.CancellationToken);
                await arguments.CompleteMessageAsync(
                    arguments.Message,
                    arguments.CancellationToken);
            }
            else
            {
                var operation = await operations.GetAsync(
                    workItem.WorkspaceId,
                    workItem.OperationId,
                    arguments.CancellationToken);
                if (operation is not null)
                {
                    await operations.UpdateAsync(
                        operation with
                        {
                            Status = OperationStatus.Failed,
                            UpdatedAt = timeProvider.GetUtcNow(),
                            ErrorCode = exception is DomainException domain
                                ? domain.Code
                                : "recommendation.retry_exhausted",
                            ErrorDetail = "Recommendation processing exhausted its retry budget."
                        },
                        arguments.CancellationToken);
                }

                await arguments.DeadLetterMessageAsync(
                    arguments.Message,
                    "retry-exhausted",
                    "Recommendation processing exhausted its retry budget.",
                    arguments.CancellationToken);
            }

            LogOperationFailure(logger, workItem.OperationId, retryAttempt, exception);
        }
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
