using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace OpportunityEngineering.Api.Endpoints;

internal static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready")
            }).AllowAnonymous();

        return app;
    }
}
