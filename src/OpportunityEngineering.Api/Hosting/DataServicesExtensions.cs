using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.AI.Projects;
using Microsoft.Azure.Cosmos;
using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Application.Ports;
using OpportunityEngineering.Infrastructure;
using OpportunityEngineering.Infrastructure.Agents;
using OpportunityEngineering.Infrastructure.Cosmos;
using OpportunityEngineering.Infrastructure.Fabric;
using OpportunityEngineering.Infrastructure.Messaging;
using OpportunityEngineering.Infrastructure.Policy;

namespace OpportunityEngineering.Api.Hosting;

internal static class DataServicesExtensions
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    public static WebApplicationBuilder AddPlatformDataServices(this WebApplicationBuilder builder)
    {
        var workspaceSettings = (
            builder.Configuration["WorkspaceAuthorizationJson"] is { Length: > 0 } workspaceJson
                ? JsonSerializer.Deserialize<WorkspaceAuthorizationSettings>(workspaceJson, WebJsonOptions)
                : builder.Configuration
                    .GetRequiredSection("WorkspaceAuthorization")
                    .Get<WorkspaceAuthorizationSettings>())
            ?? throw new InvalidOperationException("Workspace authorization configuration is missing.");
        workspaceSettings.Validate();
        builder.Services.AddSingleton(workspaceSettings);
        builder.Services.AddSingleton<WorkspaceActorResolver>();

        var cosmosEndpoint = builder.Configuration.RequiredConfiguration("Cosmos:AccountEndpoint");
        var cosmosSettings = builder.Configuration
            .GetRequiredSection("Cosmos")
            .Get<CosmosSettings>()
            ?? throw new InvalidOperationException("Cosmos configuration is missing.");
        var serviceBusNamespace = builder.Configuration.RequiredConfiguration("ServiceBus:FullyQualifiedNamespace");
        var serviceBusSettings = builder.Configuration
            .GetRequiredSection("ServiceBus")
            .Get<ServiceBusSettings>()
            ?? throw new InvalidOperationException("Service Bus configuration is missing.");
        var foundryEndpoint = builder.Configuration.RequiredConfiguration("Foundry:ProjectEndpoint");
        var foundrySettings = builder.Configuration
            .GetRequiredSection("Foundry")
            .Get<FoundryAgentSettings>()
            ?? throw new InvalidOperationException("Foundry configuration is missing.");

        var policy = LoadGuardrailPolicy(builder);

        var fabricSettings = (
            builder.Configuration["FabricJson"] is { Length: > 0 } fabricJson
                ? JsonSerializer.Deserialize<FabricGovernanceSettings>(fabricJson, WebJsonOptions)
                : builder.Configuration
                    .GetSection("Fabric")
                    .Get<FabricGovernanceSettings>())
            ?? new FabricGovernanceSettings();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IIdentifierFactory, SystemIdentifierFactory>();
        builder.Services.AddSingleton(policy);
        builder.Services.AddSingleton<IAgentPolicyEvaluator, GuardrailPolicyEvaluator>();
        builder.Services.AddSingleton(cosmosSettings);
        builder.Services.AddSingleton(serviceBusSettings);
        builder.Services.AddSingleton(foundrySettings);
        builder.Services.AddSingleton(fabricSettings);
        builder.Services.AddSingleton<FabricGovernanceGate>();
        builder.Services.AddSingleton(_ => new DefaultAzureCredential());
        builder.Services.AddSingleton(provider =>
            new CosmosClient(
                cosmosEndpoint,
                provider.GetRequiredService<DefaultAzureCredential>(),
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConsistencyLevel = ConsistencyLevel.Session
                }));
        builder.Services.AddSingleton(provider =>
            provider.GetRequiredService<CosmosClient>().GetContainer(
                cosmosSettings.DatabaseName,
                cosmosSettings.ContainerName));
        builder.Services.AddSingleton(provider =>
            new ServiceBusClient(
                serviceBusNamespace,
                provider.GetRequiredService<DefaultAzureCredential>()));
        builder.Services.AddSingleton(provider =>
            new AIProjectClient(
                new Uri(foundryEndpoint),
                provider.GetRequiredService<DefaultAzureCredential>()));

        builder.Services.AddSingleton<IOpportunityGraphStore, CosmosGraphStore>();
        builder.Services.AddSingleton<IDurableOperationStore, CosmosOperationStore>();
        builder.Services.AddSingleton<IProjectionStore, CosmosProjectionStore>();
        builder.Services.AddSingleton<IAppendOnlyAuditSink, CosmosAuditSink>();
        builder.Services.AddSingleton<IActivityAuditSink, CosmosActivityAuditSink>();
        builder.Services.AddSingleton<IEventConsumerClaimStore, CosmosConsumerClaimStore>();
        builder.Services.AddSingleton<ILiveSessionStore, CosmosLiveSessionStore>();
        builder.Services.AddSingleton<ILiveVoteStore, CosmosLiveVoteStore>();
        builder.Services.AddSingleton<ILiveIdeationNoteStore, CosmosLiveIdeationNoteStore>();
        builder.Services.AddSingleton<ILivePinStore, CosmosLivePinStore>();
        builder.Services.AddSingleton<ILiveBoardCardStore, CosmosLiveBoardCardStore>();
        builder.Services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();

        return builder;
    }

    private static GuardrailPolicy LoadGuardrailPolicy(WebApplicationBuilder builder)
    {
        var policyPath = builder.Configuration.RequiredConfiguration("Guardrails:PolicyPath");
        var policy = GuardrailPolicy.Load(
            Path.IsPathRooted(policyPath)
                ? policyPath
                : Path.Combine(builder.Environment.ContentRootPath, policyPath));

        if (builder.Configuration["Guardrails:Mode"] is { Length: > 0 } policyMode)
        {
            if (policyMode is not ("evaluation-only" or "enforce"))
            {
                throw new InvalidOperationException("The configured guardrail mode is invalid.");
            }

            policy = policy with { Mode = policyMode };
        }

        if (builder.Configuration["Guardrails:ApprovedBy"] is { Length: > 0 } approvedBy)
        {
            policy = policy with { ApprovedBy = approvedBy };
        }

        if (builder.Configuration["Guardrails:RollbackReference"] is { Length: > 0 } rollbackReference)
        {
            policy = policy with { RollbackReference = rollbackReference };
        }

        return policy.Mode == "enforce" &&
            (policy.ApprovedBy == "deployment-approval-required" ||
             policy.RollbackReference == "initial-policy")
            ? throw new InvalidOperationException(
                "Enforced guardrails require explicit operator approval and a rollback reference.")
            : policy;
    }
}
