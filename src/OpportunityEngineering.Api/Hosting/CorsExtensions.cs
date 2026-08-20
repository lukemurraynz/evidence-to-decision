namespace OpportunityEngineering.Api.Hosting;

internal static class CorsExtensions
{
    public static WebApplicationBuilder AddPlatformCors(this WebApplicationBuilder builder)
    {
        var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("frontend", policy =>
            {
                if (corsAllowedOrigins.Length > 0)
                {
                    policy.WithOrigins(corsAllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithExposedHeaders("ETag", "Retry-After", "x-correlation-id");
                }
            });
        });

        return builder;
    }
}
