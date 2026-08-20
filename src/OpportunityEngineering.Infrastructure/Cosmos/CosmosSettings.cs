namespace OpportunityEngineering.Infrastructure.Cosmos;

public sealed record CosmosSettings(
    string DatabaseName,
    string ContainerName);
