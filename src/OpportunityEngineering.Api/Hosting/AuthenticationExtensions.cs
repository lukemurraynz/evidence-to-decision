using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OpportunityEngineering.Api.Authorization;

namespace OpportunityEngineering.Api.Hosting;

internal static class AuthenticationExtensions
{
    public static WebApplicationBuilder AddPlatformAuthentication(this WebApplicationBuilder builder)
    {
        var participantSigningKey = builder.Configuration.RequiredConfiguration("Participant:SigningKey");
        var participantTokenSettings = new ParticipantTokenSettings { SigningKey = participantSigningKey };
        builder.Services.AddSingleton(participantTokenSettings);
        builder.Services.AddSingleton<ParticipantTokenIssuer>();
        builder.Services.AddSingleton<ParticipantContextResolver>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetRequiredSection("AzureAd"))
            .Services
            .AddAuthentication()
            .AddJwtBearer(ParticipantTokenIssuer.Scheme, options =>
            {
                // Without this, ASP.NET Core's default JWT claim mapping silently renames
                // well-known short claim names ("sub" -> ClaimTypes.NameIdentifier, "name" ->
                // ClaimTypes.Name) before they reach Context.User, breaking any code, like
                // ParticipantContextResolver, that reads the original short names.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    IssuerSigningKey = ParticipantTokenIssuer.SigningKey(participantTokenSettings),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                };
            });
        builder.Services.AddAuthorization();

        return builder;
    }
}
