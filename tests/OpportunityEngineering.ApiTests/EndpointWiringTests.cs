using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace OpportunityEngineering.ApiTests;

// ASP.NET Core minimal APIs build endpoint metadata for every registered route lazily, the first
// time anything touches the shared EndpointDataSource (e.g. AuthorizationMiddleware's policy
// cache). A route with an invalid parameter binding (for example, a body parameter inferred on a
// verb that disallows inferred bodies) throws only at that point, not at build time and not when
// unit tests exercise services in isolation. Nothing in this suite issued a single HTTP request
// through the real pipeline until this file, which is why a broken DELETE endpoint shipped to
// production undetected. This test needs no more than one request to any route to catch that
// whole class of defect.
// Mutates process-wide environment variables to configure the app, so it must never run
// concurrently with anything else that does the same (see CollaborationHubTests).
[TestClass]
[DoNotParallelize]
public sealed class EndpointWiringTests
{
    // Shared with ParticipantAuthorizationTests, which needs the same real app host to pull
    // the actual configured JwtBearerOptions rather than a hand-maintained guess of them.
    internal static readonly IReadOnlyDictionary<string, string> TestConfiguration = new Dictionary<string, string>
    {
        ["EntraID__Instance"] = "https://login.microsoftonline.com/",
        ["EntraID__TenantID"] = "00000000-0000-0000-0000-000000000000",
        ["EntraID__ClientID"] = "00000000-0000-0000-0000-000000000001",
        ["WorkspaceAuthorizationJson"] = """
            {"workspaces":{"workspace-1":{
                "FacilitatorGroupObjectIds":["00000000-0000-0000-0000-0000000000f1"],
                "ReviewerGroupObjectIds":["00000000-0000-0000-0000-0000000000f2"]}}}
            """,
        ["Cosmos__AccountEndpoint"] = "https://cosmos-test.documents.azure.com:443/",
        ["Cosmos__DatabaseName"] = "test-db",
        ["Cosmos__ContainerName"] = "test-container",
        ["ServiceBus__FullyQualifiedNamespace"] = "sb-test.servicebus.windows.net",
        ["ServiceBus__GraphEventsTopic"] = "graph-events",
        ["ServiceBus__RecommendationQueue"] = "recommendations",
        ["ServiceBus__ReviewSubscription"] = "review-projection",
        ["Foundry__ProjectEndpoint"] = "https://foundry-test.services.ai.azure.com/api/projects/test",
        ["Foundry__ModelDeploymentName"] = "gpt-4o",
        ["Foundry__ModelIdentity"] = "gpt-4o",
        ["Guardrails__Mode"] = "evaluation-only",
        ["Participant__SigningKey"] = "test-signing-key-at-least-32-bytes-long-for-hmac-sha256",
        ["SignalR__Endpoint"] = "https://sigr-test.service.signalr.net",
    };

    [TestMethod]
    public async Task EndpointRouteTableBuildsWithoutError()
    {
        foreach (var (key, value) in TestConfiguration)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
        Environment.SetEnvironmentVariable(
            "Guardrails__PolicyPath",
            Path.Combine(AppContext.BaseDirectory, "guardrails", "policy.json"));
        try
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.ClearProviders()));
            using var client = factory.CreateClient();

            // /health/live is anonymous, but authorization middleware still resolves the full
            // endpoint data source for every request regardless of which route was hit.
            var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

            Assert.AreNotEqual(
                System.Net.HttpStatusCode.InternalServerError,
                response.StatusCode,
                await response.Content.ReadAsStringAsync());
        }
        finally
        {
            foreach (var key in TestConfiguration.Keys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
            Environment.SetEnvironmentVariable("Guardrails__PolicyPath", null);
        }
    }

    [TestMethod]
    public async Task JoinCodeRedemptionIsRateLimitedPerSourceAfterTenRequestsInAWindow()
    {
        foreach (var (key, value) in TestConfiguration)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
        Environment.SetEnvironmentVariable(
            "Guardrails__PolicyPath",
            Path.Combine(AppContext.BaseDirectory, "guardrails", "policy.json"));
        try
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.ClearProviders()));
            using var client = factory.CreateClient();

            HttpResponseMessage? lastResponse = null;
            for (var attempt = 0; attempt < 11; attempt++)
            {
                lastResponse = await client.PostAsJsonAsync(
                    "/api/v1/join/NOTAREALCODE",
                    new { DisplayName = "Riley" });
            }

            Assert.AreEqual(System.Net.HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
        }
        finally
        {
            foreach (var key in TestConfiguration.Keys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
            Environment.SetEnvironmentVariable("Guardrails__PolicyPath", null);
        }
    }
}
