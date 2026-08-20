using OpportunityEngineering.Infrastructure.Cosmos;

namespace OpportunityEngineering.Api.Workers;

public sealed class OutboxWorker(
    CosmosOutboxDispatcher dispatcher,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogDispatchFailure =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1001, nameof(LogDispatchFailure)),
            "Outbox dispatch failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dispatched = await dispatcher.DispatchAsync(50, stoppingToken);
                if (dispatched == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogDispatchFailure(logger, exception);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
