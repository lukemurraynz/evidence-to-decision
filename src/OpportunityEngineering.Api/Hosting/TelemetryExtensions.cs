using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using OpportunityEngineering.Api.Health;
using OpportunityEngineering.Domain;
using OpportunityEngineering.Infrastructure.Agents;

namespace OpportunityEngineering.Api.Hosting;

internal static class TelemetryExtensions
{
    public static WebApplicationBuilder AddPlatformTelemetry(this WebApplicationBuilder builder)
    {
        var openTelemetryBuilder = builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(FoundryRecommendationAgent.ActivitySourceName));
        if (builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] is { Length: > 0 })
        {
            openTelemetryBuilder.UseAzureMonitor();
        }

        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services
            .AddHealthChecks()
            .AddCheck<CosmosHealthCheck>("cosmos", tags: ["ready"])
            .AddCheck<ServiceBusHealthCheck>("service-bus", tags: ["ready"]);

        return builder;
    }

    public static WebApplication UsePlatformExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var (status, title, code) = exception switch
            {
                DomainException domain when domain.Code.StartsWith(
                    "authorization.",
                    StringComparison.Ordinal) =>
                    (StatusCodes.Status403Forbidden, "Access denied", domain.Code),
                DomainException domain when domain.Code.EndsWith(
                    ".not_found",
                    StringComparison.Ordinal) =>
                    (StatusCodes.Status404NotFound, "Resource not found", domain.Code),
                DomainException domain when domain.Code is "graph.version_conflict" =>
                    (StatusCodes.Status412PreconditionFailed, "Version conflict", domain.Code),
                DomainException domain when domain.Code is "operation.idempotency_conflict" =>
                    (StatusCodes.Status409Conflict, "Idempotency conflict", domain.Code),
                DomainException domain =>
                    (StatusCodes.Status400BadRequest, "Request rejected", domain.Code),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Unexpected error",
                    "server.unexpected")
            };
            context.Response.StatusCode = status;
            await Results.Problem(
                statusCode: status,
                title: title,
                detail: exception is DomainException ? exception.Message : "The request could not be completed.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = code,
                    ["correlationId"] = context.TraceIdentifier
                }).ExecuteAsync(context);
        }));

        return app;
    }
}
