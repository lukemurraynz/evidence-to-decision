using Azure.Identity;
using OpportunityEngineering.Api.Hubs;

namespace OpportunityEngineering.Api.Hosting;

internal static class RealtimeExtensions
{
    public static WebApplicationBuilder AddPlatformRealtime(this WebApplicationBuilder builder)
    {
        var signalREndpoint = builder.Configuration.RequiredConfiguration("SignalR:Endpoint");
        builder.Services.AddSignalR().AddAzureSignalR(options =>
        {
            // Azure SignalR Service forwards only a minimal built-in claim set to the hub context
            // by default. The custom "wid"/"eid"/"sid"/"name"/"scope" claims on the Participant
            // token need to be explicitly opted in here to survive the trip through the service.
            options.ClaimsProvider = context => context.User?.Claims ?? [];
            options.Endpoints =
            [
                new Microsoft.Azure.SignalR.ServiceEndpoint(new Uri(signalREndpoint), new DefaultAzureCredential())
            ];
        });
        builder.Services.AddSingleton<LivePresenceTracker>();
        builder.Services.AddSingleton<FacilitatorConnectionTracker>();

        return builder;
    }
}
