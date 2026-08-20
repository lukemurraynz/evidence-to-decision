using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Cosmos;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Infrastructure.Cosmos;

public sealed class CosmosGraphStore(Container container) : IOpportunityGraphStore
{
    public async Task<OpportunityGraph?> GetAsync(
        string workspaceId,
        string engagementId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<GraphDocument>(
                GraphId(engagementId),
                new PartitionKey(workspaceId),
                cancellationToken: cancellationToken);
            return response.Resource.payload;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<OpportunityGraph>> QueryWorkspaceAsync(
        string workspaceId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT VALUE c.payload FROM c WHERE c.documentType = @type")
            .WithParameter("@type", DocumentTypes.Graph);
        var iterator = container.GetItemQueryIterator<OpportunityGraph>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(workspaceId)
            });
        var results = new List<OpportunityGraph>();
        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }

    public async Task<IReadOnlyList<OpportunityGraph>> QueryWorkspaceInWindowAsync(
        string workspaceId,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken)
    {
        // Push the decision timestamp window predicate to Cosmos to reduce RU consumption and
        // avoid materializing graphs with no decisions in the requested window.
        var query = new QueryDefinition(
            """
            SELECT VALUE c.payload FROM c
            WHERE c.documentType = @type
            AND EXISTS(
                SELECT 1 FROM decision IN c.payload.decisions
                WHERE decision.timestamp >= @windowStart
                  AND decision.timestamp < @windowEnd
            )
            """)
            .WithParameter("@type", DocumentTypes.Graph)
            .WithParameter("@windowStart", windowStart)
            .WithParameter("@windowEnd", windowEnd);
        var iterator = container.GetItemQueryIterator<OpportunityGraph>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(workspaceId)
            });
        var results = new List<OpportunityGraph>();
        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }

    public async Task CreateAsync(
        OpportunityGraph graph,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken)
    {
        var batch = container.CreateTransactionalBatch(new PartitionKey(graph.WorkspaceId))
            .CreateItem(new GraphDocument(
                GraphId(graph.Id),
                graph.WorkspaceId,
                DocumentTypes.Graph,
                graph))
            .CreateItem(new EventDocument(
                EventId(graphEvent.EventId),
                graph.WorkspaceId,
                DocumentTypes.Event,
                graphEvent))
            .CreateItem(new DomainAuditDocument(
                DomainAuditId(auditRecord.Id),
                graph.WorkspaceId,
                DocumentTypes.DomainAudit,
                auditRecord));

        using var response = await batch.ExecuteAsync(cancellationToken);
        EnsureBatchSucceeded(response, "graph.create_failed");
    }

    public async Task ReplaceAsync(
        OpportunityGraph graph,
        long expectedVersion,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken)
    {
        ItemResponse<GraphDocument> current;
        try
        {
            current = await container.ReadItemAsync<GraphDocument>(
                GraphId(graph.Id),
                new PartitionKey(graph.WorkspaceId),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            throw new DomainException("engagement.not_found", "Engagement was not found.");
        }

        if (current.Resource.payload.ObjectVersion != expectedVersion)
        {
            throw VersionConflict();
        }

        var batch = container.CreateTransactionalBatch(new PartitionKey(graph.WorkspaceId))
            .ReplaceItem(
                GraphId(graph.Id),
                new GraphDocument(
                    GraphId(graph.Id),
                    graph.WorkspaceId,
                    DocumentTypes.Graph,
                    graph),
                new TransactionalBatchItemRequestOptions { IfMatchEtag = current.ETag })
            .CreateItem(new EventDocument(
                EventId(graphEvent.EventId),
                graph.WorkspaceId,
                DocumentTypes.Event,
                graphEvent))
            .CreateItem(new DomainAuditDocument(
                DomainAuditId(auditRecord.Id),
                graph.WorkspaceId,
                DocumentTypes.DomainAudit,
                auditRecord));

        using var response = await batch.ExecuteAsync(cancellationToken);
        if (response.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.Conflict)
        {
            throw VersionConflict();
        }

        EnsureBatchSucceeded(response, "graph.replace_failed");
    }

    public async Task DeleteAsync(
        OpportunityGraph graph,
        long expectedVersion,
        GraphChangedEvent graphEvent,
        AuditRecord auditRecord,
        CancellationToken cancellationToken)
    {
        var current = await container.ReadItemAsync<GraphDocument>(
            GraphId(graph.Id),
            new PartitionKey(graph.WorkspaceId),
            cancellationToken: cancellationToken);
        if (current.Resource.payload.ObjectVersion != expectedVersion)
        {
            throw VersionConflict();
        }

        var batch = container.CreateTransactionalBatch(new PartitionKey(graph.WorkspaceId))
            .DeleteItem(
                GraphId(graph.Id),
                new TransactionalBatchItemRequestOptions { IfMatchEtag = current.ETag })
            .CreateItem(new EventDocument(
                EventId(graphEvent.EventId),
                graph.WorkspaceId,
                DocumentTypes.Event,
                graphEvent))
            .CreateItem(new DomainAuditDocument(
                DomainAuditId(auditRecord.Id),
                graph.WorkspaceId,
                DocumentTypes.DomainAudit,
                auditRecord));
        using var response = await batch.ExecuteAsync(cancellationToken);
        if (response.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.Conflict)
        {
            throw VersionConflict();
        }

        EnsureBatchSucceeded(response, "graph.delete_failed");
    }

    private static string GraphId(string id) => $"graph:{id}";
    private static string EventId(string id) => $"event:{id}";
    private static string DomainAuditId(string id) => $"domain-audit:{id}";

    private static DomainException VersionConflict() =>
        new("graph.version_conflict", "The canonical graph changed. Reread it before retrying.");

    private static void EnsureBatchSucceeded(
        TransactionalBatchResponse response,
        string errorCode)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new DomainException(
                errorCode,
                $"The durable transaction failed with status {(int)response.StatusCode}.");
        }
    }
}

public sealed class CosmosOperationStore(Container container) : IDurableOperationStore
{
    public async Task<DurableOperation> CreateAsync(
        DurableOperation operation,
        RecommendationWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var document = new OperationDocument(
            OperationId(operation.Id),
            operation.WorkspaceId,
            DocumentTypes.Operation,
            operation,
            workItem);
        var batch = container.CreateTransactionalBatch(new PartitionKey(operation.WorkspaceId))
            .CreateItem(document)
            .CreateItem(new IdempotencyKeyDocument(
                IdempotencyKeyId(operation.IdempotencyKey),
                operation.WorkspaceId,
                DocumentTypes.IdempotencyKey,
                operation.Id,
                operation.RequestHash));
        using var response = await batch.ExecuteAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var existing = await GetByIdempotencyKeyAsync(
                    operation.WorkspaceId,
                    operation.IdempotencyKey,
                    cancellationToken) ?? throw new DomainException(
                        "operation.conflict",
                        "The operation already exists.");
                return !string.Equals(existing.RequestHash, operation.RequestHash, StringComparison.Ordinal)
                    ? throw new DomainException(
                        "operation.idempotency_conflict",
                        "A different request was already submitted with the same idempotency key.")
                    : existing;
            }

            throw new DomainException(
                "operation.create_failed",
                $"The durable operation transaction failed with status {(int)response.StatusCode}.");
        }

        return operation;
    }

    public async Task<DurableOperation?> GetAsync(
        string workspaceId,
        string operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<OperationDocument>(
                OperationId(operationId),
                new PartitionKey(workspaceId),
                cancellationToken: cancellationToken);
            return response.Resource.payload;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<DurableOperation?> GetByIdempotencyKeyAsync(
        string workspaceId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var marker = await container.ReadItemAsync<IdempotencyKeyDocument>(
                IdempotencyKeyId(idempotencyKey),
                new PartitionKey(workspaceId),
                cancellationToken: cancellationToken);
            return await GetAsync(workspaceId, marker.Resource.operationId, cancellationToken);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task UpdateAsync(
        DurableOperation operation,
        CancellationToken cancellationToken)
    {
        var existing = await container.ReadItemAsync<OperationDocument>(
            OperationId(operation.Id),
            new PartitionKey(operation.WorkspaceId),
            cancellationToken: cancellationToken);
        await container.ReplaceItemAsync(
            existing.Resource with { payload = operation },
            existing.Resource.id,
            new PartitionKey(operation.WorkspaceId),
            new ItemRequestOptions { IfMatchEtag = existing.ETag },
            cancellationToken);
    }

    private static string OperationId(string id) => $"operation:{id}";

    private static string IdempotencyKeyId(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"idempotency:{Convert.ToHexString(hash)}";
    }
}

public sealed class CosmosProjectionStore(Container container) : IProjectionStore
{
    public Task SaveRecommendationAsync(
        string workspaceId,
        OpportunityRecommendation recommendation,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            workspaceId,
            $"recommendation:{recommendation.RecommendationId}",
            DocumentTypes.Recommendation,
            recommendation,
            cancellationToken);

    public Task<OpportunityRecommendation?> GetRecommendationAsync(
        string workspaceId,
        string recommendationId,
        CancellationToken cancellationToken) =>
        ReadAsync<OpportunityRecommendation>(
            workspaceId,
            $"recommendation:{recommendationId}",
            cancellationToken);

    public Task SaveArtifactAsync(
        string workspaceId,
        ArtifactEnvelope artifact,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            workspaceId,
            $"artifact:{artifact.ArtifactId}",
            DocumentTypes.Artifact,
            artifact,
            cancellationToken);

    public Task<ArtifactEnvelope?> GetArtifactAsync(
        string workspaceId,
        string artifactId,
        CancellationToken cancellationToken) =>
        ReadAsync<ArtifactEnvelope>(
            workspaceId,
            $"artifact:{artifactId}",
            cancellationToken);

    public Task SaveAnalyticsAsync(
        string workspaceId,
        PortfolioAnalyticsProjection projection,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            workspaceId,
            $"analytics:{projection.Id}",
            DocumentTypes.Analytics,
            projection,
            cancellationToken);

    public Task SaveReviewAsync(
        string workspaceId,
        OpportunityReviewProjection review,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            workspaceId,
            $"review:{review.OpportunityId}",
            DocumentTypes.Review,
            review,
            cancellationToken);

    public Task SaveReviewerNotificationAsync(
        string workspaceId,
        ReviewerNotification notification,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            workspaceId,
            $"reviewer-notification:{notification.NotificationId}",
            DocumentTypes.ReviewerNotification,
            notification,
            cancellationToken);

    public async Task<ReviewerNotificationsPage> QueryReviewerNotificationsAsync(
        string workspaceId,
        int pageSize,
        string? continuationToken,
        CancellationToken cancellationToken)
    {
        var clampedSize = Math.Clamp(pageSize, 1, 100);
        var query = new QueryDefinition(
            "SELECT VALUE c.payload FROM c WHERE c.documentType = @type ORDER BY c.payload.createdAt DESC")
            .WithParameter("@type", DocumentTypes.ReviewerNotification);
        var iterator = container.GetItemQueryIterator<ReviewerNotification>(
            query,
            continuationToken: continuationToken,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(workspaceId),
                MaxItemCount = clampedSize
            });
        var notifications = new List<ReviewerNotification>(clampedSize);
        string? nextToken = null;
        if (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            notifications.AddRange(page.Take(clampedSize));
            nextToken = page.ContinuationToken;
        }

        return new ReviewerNotificationsPage(notifications, nextToken);
    }

    public async Task DeleteReviewsByEngagementAsync(
        string workspaceId,
        string engagementId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            """
            SELECT c.id FROM c
            WHERE (c.documentType = @reviewType OR c.documentType = @notificationType)
            AND c.payload.engagementId = @engagementId
            """)
            .WithParameter("@reviewType", DocumentTypes.Review)
            .WithParameter("@notificationType", DocumentTypes.ReviewerNotification)
            .WithParameter("@engagementId", engagementId);
        var iterator = container.GetItemQueryIterator<dynamic>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(workspaceId) });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            foreach (var item in page)
            {
                string id = item.id;
                try
                {
                    await container.DeleteItemAsync<object>(
                        id,
                        new PartitionKey(workspaceId),
                        cancellationToken: cancellationToken);
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                }
            }
        }
    }

    private async Task UpsertAsync<T>(
        string workspaceId,
        string id,
        string documentType,
        T payload,
        CancellationToken cancellationToken)
    {
        await container.UpsertItemAsync(
            new ProjectionDocument<T>(id, workspaceId, documentType, payload),
            new PartitionKey(workspaceId),
            cancellationToken: cancellationToken);
    }

    private async Task<T?> ReadAsync<T>(
        string workspaceId,
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<ProjectionDocument<T>>(
                id,
                new PartitionKey(workspaceId),
                cancellationToken: cancellationToken);
            return response.Resource.payload;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
    }
}

public sealed class CosmosAuditSink(Container container) : IAppendOnlyAuditSink
{
    public async Task AppendAsync(
        PolicyAuditRecord record,
        CancellationToken cancellationToken)
    {
        await container.CreateItemAsync(
            new PolicyAuditDocument(
                $"policy-audit:{record.Id}",
                record.WorkspaceId,
                DocumentTypes.PolicyAudit,
                record),
            new PartitionKey(record.WorkspaceId),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<PolicyAuditRecord>> QueryAsync(
        string workspaceId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            """
            SELECT VALUE c.payload
            FROM c
            WHERE c.documentType = @type
              AND c.payload.correlationId = @correlationId
            ORDER BY c.payload.occurredAt
            """)
            .WithParameter("@type", DocumentTypes.PolicyAudit)
            .WithParameter("@correlationId", correlationId);
        var iterator = container.GetItemQueryIterator<PolicyAuditRecord>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(workspaceId)
            });
        var results = new List<PolicyAuditRecord>();
        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }
}

public sealed class CosmosActivityAuditSink(Container container) : IActivityAuditSink
{
    public async Task AppendAsync(
        AuditRecord record,
        CancellationToken cancellationToken)
    {
        await container.CreateItemAsync(
            new DomainAuditDocument(
                $"domain-audit:{record.Id}",
                record.WorkspaceId,
                DocumentTypes.DomainAudit,
                record),
            new PartitionKey(record.WorkspaceId),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRecord>> QueryAsync(
        string workspaceId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            """
            SELECT VALUE c.payload
            FROM c
            WHERE c.documentType = @type
              AND c.payload.correlationId = @correlationId
            ORDER BY c.payload.occurredAt
            """)
            .WithParameter("@type", DocumentTypes.DomainAudit)
            .WithParameter("@correlationId", correlationId);
        var iterator = container.GetItemQueryIterator<AuditRecord>(
            query,
            requestOptions: new QueryRequestOptions
            {
                PartitionKey = new PartitionKey(workspaceId)
            });
        var results = new List<AuditRecord>();
        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }
}

public sealed class CosmosConsumerClaimStore(Container container) : IEventConsumerClaimStore
{
    // Processing claims expire after 10 minutes so a crash cannot leave a permanent lock.
    // Completed claims retain ttl = -1 (no expiry) for deduplication.
    private const int ProcessingLeaseSeconds = 600;

    public async Task<bool> TryClaimAsync(
        string workspaceId,
        string eventId,
        string consumerName,
        CancellationToken cancellationToken)
    {
        var document = new ConsumerClaimDocument(
            ClaimId(eventId, consumerName),
            workspaceId,
            DocumentTypes.ConsumerClaim,
            eventId,
            consumerName,
            "processing",
            null,
            ttl: ProcessingLeaseSeconds);
        try
        {
            await container.CreateItemAsync(
                document,
                new PartitionKey(workspaceId),
                cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            return false;
        }
    }

    public async Task CompleteAsync(
        ConsumerResult result,
        CancellationToken cancellationToken)
    {
        var id = ClaimId(result.EventId, result.ConsumerName);
        var existing = await container.ReadItemAsync<ConsumerClaimDocument>(
            id,
            new PartitionKey(result.WorkspaceId),
            cancellationToken: cancellationToken);
        await container.ReplaceItemAsync(
            existing.Resource with { status = "completed", result = result, ttl = -1 },
            id,
            new PartitionKey(result.WorkspaceId),
            new ItemRequestOptions { IfMatchEtag = existing.ETag },
            cancellationToken);
    }

    public async Task ReleaseAsync(
        string workspaceId,
        string eventId,
        string consumerName,
        CancellationToken cancellationToken)
    {
        try
        {
            await container.DeleteItemAsync<ConsumerClaimDocument>(
                ClaimId(eventId, consumerName),
                new PartitionKey(workspaceId),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    private static string ClaimId(string eventId, string consumerName) =>
        $"claim:{consumerName}:{eventId}";
}

public sealed class CosmosLiveSessionStore(Container container, TimeProvider timeProvider) : ILiveSessionStore
{
    // Live sessions are ephemeral workshop artifacts; 24h covers a session that runs late
    // without leaving abandoned test sessions around indefinitely.
    private const int SessionTtlSeconds = 86_400;

    public async Task<LiveSession> CreateAsync(
        LiveSession session,
        CancellationToken cancellationToken)
    {
        await container.CreateItemAsync(
            new LiveSessionDocument(
                SessionId(session.Id),
                session.WorkspaceId,
                DocumentTypes.LiveSession,
                session,
                ttl: SessionTtlSeconds),
            new PartitionKey(session.WorkspaceId),
            cancellationToken: cancellationToken);
        return session;
    }

    public async Task<LiveSession> UpdateStatusAsync(
        LiveSession session,
        CancellationToken cancellationToken)
    {
        await container.UpsertItemAsync(
            new LiveSessionDocument(
                SessionId(session.Id),
                session.WorkspaceId,
                DocumentTypes.LiveSession,
                session,
                ttl: SessionTtlSeconds),
            new PartitionKey(session.WorkspaceId),
            cancellationToken: cancellationToken);
        return session;
    }

    public async Task<LiveSession?> GetByJoinCodeAsync(
        string joinCode,
        CancellationToken cancellationToken)
    {
        // A participant knows only the join code, not the workspace. This is the one
        // cross-partition query in the collaboration store, kept cheap by the small,
        // short-lived live-session document set.
        // The Cosmos SDK's default serializer preserves C# PascalCase property names as-is
        // in the stored JSON (no camelCase conversion), so the query must match that exactly.
        var query = new QueryDefinition(
            "SELECT VALUE c.payload FROM c WHERE c.documentType = @type AND c.payload.JoinCode = @joinCode")
            .WithParameter("@type", DocumentTypes.LiveSession)
            .WithParameter("@joinCode", joinCode);
        var iterator = container.GetItemQueryIterator<LiveSession>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            var match = page.FirstOrDefault();
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    public async Task<LiveSession?> GetAsync(
        string workspaceId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<LiveSessionDocument>(
                SessionId(sessionId),
                new PartitionKey(workspaceId),
                cancellationToken: cancellationToken);
            return response.Resource.payload;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<LiveSession?> GetActiveByStepAsync(
        string workspaceId,
        string engagementId,
        string journeyStepId,
        CancellationToken cancellationToken)
    {
        // Status is set once at creation and never updated (there's no "close" mutation), so a
        // long-finished session from an earlier round still matches Status = "active". The
        // real signal that a session is actually current is that it hasn't expired yet. Order
        // by CreatedAt so if more than one somehow still qualifies, the newest wins.
        var query = new QueryDefinition(
            "SELECT VALUE c.payload FROM c WHERE c.documentType = @type AND c.payload.EngagementId = @engagementId "
            + "AND c.payload.JourneyStepId = @journeyStepId AND c.payload.Status = @status "
            + "AND c.payload.ExpiresAt > @now ORDER BY c.payload.CreatedAt DESC")
            .WithParameter("@type", DocumentTypes.LiveSession)
            .WithParameter("@engagementId", engagementId)
            .WithParameter("@journeyStepId", journeyStepId)
            .WithParameter("@status", "active")
            .WithParameter("@now", timeProvider.GetUtcNow());
        var iterator = container.GetItemQueryIterator<LiveSession>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(workspaceId) });
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            var match = page.FirstOrDefault();
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string SessionId(string id) => $"live-session:{id}";
}

public sealed class CosmosLiveVoteStore(Container container) : ILiveVoteStore
{
    // Matches the LiveSession lease: votes never outlive the session they belong to.
    private const int VoteTtlSeconds = 86_400;

    public async Task CastAsync(LiveVote vote, CancellationToken cancellationToken)
    {
        // Upserted (not appended) keyed by participant+card+step, so re-casting a vote for
        // the same card/step overwrites rather than double-counts, while a participant can
        // still cast independent votes for different cards or steps.
        await container.UpsertItemAsync(
            new LiveVoteDocument(
                VoteId(vote.JoinSessionId, vote.ParticipantId, vote.DiscoveryCardId, vote.JourneyStepId),
                vote.WorkspaceId,
                DocumentTypes.LiveVote,
                vote,
                ttl: VoteTtlSeconds),
            new PartitionKey(vote.WorkspaceId),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LiveVote>> QueryTallyAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT VALUE c.payload FROM c WHERE c.documentType = @type AND c.payload.JoinSessionId = @joinSessionId")
            .WithParameter("@type", DocumentTypes.LiveVote)
            .WithParameter("@joinSessionId", joinSessionId);
        var iterator = container.GetItemQueryIterator<LiveVote>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(workspaceId) });
        var results = new List<LiveVote>();
        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }

    private static string VoteId(
        string joinSessionId,
        string participantId,
        string discoveryCardId,
        string journeyStepId) =>
        $"live-vote:{joinSessionId}:{participantId}:{discoveryCardId}:{journeyStepId}";
}

public sealed class CosmosLiveIdeationNoteStore(Container container) : ILiveIdeationNoteStore
{
    // Matches the LiveSession lease: notes never outlive the session they belong to.
    private const int NoteTtlSeconds = 86_400;

    public async Task<LiveIdeationNote> SubmitAsync(LiveIdeationNote note, CancellationToken cancellationToken)
    {
        // Appended, not upserted keyed by participant: unlike a vote, a participant submits
        // many distinct notes over a round, so each note gets its own id in the key.
        await container.CreateItemAsync(
            new LiveIdeationNoteDocument(
                NoteId(note.JoinSessionId, note.Id),
                note.WorkspaceId,
                DocumentTypes.LiveIdeationNote,
                note,
                ttl: NoteTtlSeconds),
            new PartitionKey(note.WorkspaceId),
            cancellationToken: cancellationToken);
        return note;
    }

    public async Task<IReadOnlyList<LiveIdeationNote>> QueryBySessionAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT VALUE c.payload FROM c WHERE c.documentType = @type AND c.payload.JoinSessionId = @joinSessionId")
            .WithParameter("@type", DocumentTypes.LiveIdeationNote)
            .WithParameter("@joinSessionId", joinSessionId);
        var iterator = container.GetItemQueryIterator<LiveIdeationNote>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(workspaceId) });
        var results = new List<LiveIdeationNote>();
        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }

    public async Task<LiveIdeationNote?> GetAsync(
        string workspaceId,
        string joinSessionId,
        string noteId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await container.ReadItemAsync<LiveIdeationNoteDocument>(
                NoteId(joinSessionId, noteId),
                new PartitionKey(workspaceId),
                cancellationToken: cancellationToken);
            return response.Resource.payload;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private static string NoteId(string joinSessionId, string noteId) =>
        $"live-ideation-note:{joinSessionId}:{noteId}";
}

public sealed class CosmosLivePinStore(Container container) : ILivePinStore
{
    // Matches the LiveSession lease: pins never outlive the session they belong to.
    private const int PinTtlSeconds = 86_400;

    public async Task<bool> ToggleAsync(LivePin pin, CancellationToken cancellationToken)
    {
        var id = PinId(pin.JoinSessionId, pin.ParticipantId, pin.DiscoveryCardId, pin.JourneyStepId);
        var partitionKey = new PartitionKey(pin.WorkspaceId);
        try
        {
            // Already pinned; a second toggle removes it. Unlike a vote's blind upsert, this
            // needs a read-then-write; a personal, low-frequency toggle isn't worth optimizing
            // into a single round trip.
            await container.DeleteItemAsync<LivePinDocument>(id, partitionKey, cancellationToken: cancellationToken);
            return false;
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            await container.CreateItemAsync(
                new LivePinDocument(id, pin.WorkspaceId, DocumentTypes.LivePin, pin, ttl: PinTtlSeconds),
                partitionKey,
                cancellationToken: cancellationToken);
            return true;
        }
    }

    public async Task<IReadOnlyList<LivePin>> QueryTallyAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT VALUE c.payload FROM c WHERE c.documentType = @type AND c.payload.JoinSessionId = @joinSessionId")
            .WithParameter("@type", DocumentTypes.LivePin)
            .WithParameter("@joinSessionId", joinSessionId);
        var iterator = container.GetItemQueryIterator<LivePin>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(workspaceId) });
        var results = new List<LivePin>();
        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }

    private static string PinId(
        string joinSessionId,
        string participantId,
        string discoveryCardId,
        string journeyStepId) =>
        $"live-pin:{joinSessionId}:{participantId}:{discoveryCardId}:{journeyStepId}";
}

public sealed class CosmosLiveBoardCardStore(Container container) : ILiveBoardCardStore
{
    // Matches the LiveSession lease: placements never outlive the session they belong to.
    private const int BoardCardTtlSeconds = 86_400;

    public Task<LiveBoardCard> PlaceAsync(LiveBoardCard card, CancellationToken cancellationToken) =>
        UpsertAsync(card, cancellationToken);

    public Task<LiveBoardCard> MoveAsync(LiveBoardCard card, CancellationToken cancellationToken) =>
        UpsertAsync(card, cancellationToken);

    public async Task RemoveAsync(string workspaceId, string placementId, CancellationToken cancellationToken)
    {
        try
        {
            await container.DeleteItemAsync<LiveBoardCardDocument>(
                CardId(placementId), new PartitionKey(workspaceId), cancellationToken: cancellationToken);
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            // Already gone (removed by someone else, or expired via TTL). Removal is
            // idempotent, matching LiveSessionService.CloseAsync's re-save-is-fine stance.
        }
    }

    // Blind upsert keyed by the placement's own Id (not owner+card, unlike a vote or pin):
    // duplicates of the same card must coexist as separate placements, so there's no natural
    // owner+card key to overwrite. Last-write-wins on the rare case of two people moving the
    // same placement at once, matching this store family's ephemeral-state convention.
    private async Task<LiveBoardCard> UpsertAsync(LiveBoardCard card, CancellationToken cancellationToken)
    {
        await container.UpsertItemAsync(
            new LiveBoardCardDocument(
                CardId(card.Id),
                card.WorkspaceId,
                DocumentTypes.LiveBoardCard,
                card,
                ttl: BoardCardTtlSeconds),
            new PartitionKey(card.WorkspaceId),
            cancellationToken: cancellationToken);
        return card;
    }

    public async Task<IReadOnlyList<LiveBoardCard>> QueryBySessionAsync(
        string workspaceId,
        string joinSessionId,
        CancellationToken cancellationToken)
    {
        var query = new QueryDefinition(
            "SELECT VALUE c.payload FROM c WHERE c.documentType = @type AND c.payload.JoinSessionId = @joinSessionId")
            .WithParameter("@type", DocumentTypes.LiveBoardCard)
            .WithParameter("@joinSessionId", joinSessionId);
        var iterator = container.GetItemQueryIterator<LiveBoardCard>(
            query,
            requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(workspaceId) });
        var results = new List<LiveBoardCard>();
        while (iterator.HasMoreResults)
        {
            results.AddRange(await iterator.ReadNextAsync(cancellationToken));
        }

        return results;
    }

    private static string CardId(string id) => $"live-board-card:{id}";
}

