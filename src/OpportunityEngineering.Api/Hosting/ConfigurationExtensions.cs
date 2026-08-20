namespace OpportunityEngineering.Api.Hosting;

internal static class ConfigurationExtensions
{
    public static string RequiredConfiguration(this IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Required configuration '{key}' is missing.");
}
