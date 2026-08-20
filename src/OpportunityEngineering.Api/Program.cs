using OpportunityEngineering.Api.Endpoints;
using OpportunityEngineering.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddPlatformTelemetry()
    .AddPlatformAuthentication()
    .AddPlatformRateLimiting()
    .AddPlatformRealtime()
    .AddPlatformCors()
    .AddPlatformDataServices()
    .AddDomainServices();

var app = builder.Build();

app.UsePlatformExceptionHandling();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapParticipantEndpoints();

var api = app.MapGroup("/api/v1/workspaces/{workspaceId}")
    .RequireAuthorization();

api.MapEngagementEndpoints();
api.MapLiveSessionEndpoints();
api.MapEvidenceEndpoints();
api.MapOpportunityEndpoints();

app.Run();

public partial class Program;
