using System.Threading.RateLimiting;

namespace OpportunityEngineering.Api.Hosting;

internal static class RateLimitingExtensions
{
    public static WebApplicationBuilder AddPlatformRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Anonymous, unauthenticated join-code redemption is the one endpoint an attacker could
            // script to brute-force a live join code. 10/min per source IP is generous for a
            // participant fixing a typo, but throttles automated guessing.
            options.AddPolicy("join-code", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        return builder;
    }
}
