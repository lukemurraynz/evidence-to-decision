using Microsoft.Azure.Cosmos;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Infrastructure.Messaging;

namespace OpportunityEngineering.Infrastructure.Cosmos;

public sealed class CosmosOutboxDispatcher(
    Container container,
    IEventPublisher eventPublisher,
    ServiceBusRecommendationPublisher recommendationPublisher)
{
    public async Task<int> DispatchAsync(
        int maximumItems,
        CancellationToken cancellationToken)
    {
        if (maximumItems <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumItems),
                "The outbox batch size must be positive.");
        }

        var dispatched = await DispatchEventsAsync(maximumItems, cancellationToken);
        if (dispatched < maximumItems)
        {
            dispatched += await DispatchRecommendationsAsync(
                maximumItems - dispatched,
                cancellationToken);
        }

        return dispatched;
    }

    private async Task<int> DispatchEventsAsync(
        int maximumItems,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            """
            SELECT TOP @maximumItems *
            FROM c
            WHERE c.documentType = @type
              AND c.published = false
            ORDER BY c.payload.occurredAt
            """)
            .WithParameter("@maximumItems", maximumItems)
            .WithParameter("@type", DocumentTypes.Event);
        var iterator = container.GetItemQueryIterator<EventDocument>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = maximumItems
            });
        var dispatched = 0;
        while (iterator.HasMoreResults && dispatched < maximumItems)
        {
            foreach (var document in await iterator.ReadNextAsync(cancellationToken))
            {
                await eventPublisher.PublishAsync(document.payload, cancellationToken);
                await MarkPublishedAsync(
                    document.id,
                    document.workspaceId,
                    cancellationToken);
                dispatched++;
            }
        }

        return dispatched;
    }

    private async Task<int> DispatchRecommendationsAsync(
        int maximumItems,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            """
            SELECT TOP @maximumItems *
            FROM c
            WHERE c.documentType = @type
              AND c.published = false
            ORDER BY c.payload.createdAt
            """)
            .WithParameter("@maximumItems", maximumItems)
            .WithParameter("@type", DocumentTypes.Operation);
        var iterator = container.GetItemQueryIterator<OperationDocument>(
            query,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = maximumItems
            });
        var dispatched = 0;
        while (iterator.HasMoreResults && dispatched < maximumItems)
        {
            foreach (var document in await iterator.ReadNextAsync(cancellationToken))
            {
                await recommendationPublisher.PublishAsync(
                    document.workItem,
                    cancellationToken);
                await MarkPublishedAsync(
                    document.id,
                    document.workspaceId,
                    cancellationToken);
                dispatched++;
            }
        }

        return dispatched;
    }

    private async Task MarkPublishedAsync(
        string id,
        string workspaceId,
        CancellationToken cancellationToken)
    {
        await container.PatchItemAsync<object>(
            id,
            new PartitionKey(workspaceId),
            [PatchOperation.Set("/published", true)],
            cancellationToken: cancellationToken);
    }
}
