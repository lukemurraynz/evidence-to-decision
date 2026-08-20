using OpportunityEngineering.Api.Workers;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Application.Services;
using OpportunityEngineering.Infrastructure.Agents;
using OpportunityEngineering.Infrastructure.Cosmos;
using OpportunityEngineering.Infrastructure.Messaging;

namespace OpportunityEngineering.Api.Hosting;

internal static class DomainServicesExtensions
{
    public static WebApplicationBuilder AddDomainServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ServiceBusRecommendationPublisher>();
        builder.Services.AddSingleton<CosmosOutboxDispatcher>();
        builder.Services.AddSingleton<IRecommendationAgent, FoundryRecommendationAgent>();
        builder.Services.AddSingleton<IDiscoveryCardSuggestionAgent, FoundryDiscoveryCardSuggestionAgent>();
        builder.Services.AddSingleton<IArtifactNarrativeAgent, FoundryArtifactNarrativeAgent>();
        builder.Services.AddSingleton<IEvidenceQualityAgent, FoundryEvidenceQualityAgent>();
        builder.Services.AddSingleton<EvidenceQualityService>();
        builder.Services.AddSingleton<IFrameDraftAgent, FoundryFrameDraftAgent>();
        builder.Services.AddSingleton<IFrameCritiqueAgent, FoundryFrameCritiqueAgent>();
        builder.Services.AddSingleton<FrameDraftService>();
        builder.Services.AddSingleton<IBoardClusterAgent, FoundryBoardClusterAgent>();
        builder.Services.AddSingleton<BoardClusterService>();
        builder.Services.AddSingleton<DiscoveryCardSuggestionService>();

        builder.Services.AddSingleton<GateEvaluator>();
        builder.Services.AddSingleton<ProjectionFactory>();
        builder.Services.AddSingleton<GraphCommandService>();
        builder.Services.AddSingleton<GraphQueryService>();
        builder.Services.AddSingleton<LiveSessionService>();
        builder.Services.AddSingleton<LiveVoteService>();
        builder.Services.AddSingleton<LiveIdeationService>();
        builder.Services.AddSingleton<LivePinService>();
        builder.Services.AddSingleton<LiveBoardService>();
        builder.Services.AddSingleton<RecommendationSubmissionService>();
        builder.Services.AddSingleton<RecommendationExecutionService>();
        builder.Services.AddSingleton<ReplaySafeReviewConsumer>();
        builder.Services.AddHostedService<OutboxWorker>();
        builder.Services.AddSingleton<RecommendationWorker>();
        builder.Services.AddSingleton<IHostedService>(provider =>
            provider.GetRequiredService<RecommendationWorker>());
        builder.Services.AddSingleton<ReviewProjectionWorker>();
        builder.Services.AddSingleton<IHostedService>(provider =>
            provider.GetRequiredService<ReviewProjectionWorker>());

        return builder;
    }
}
