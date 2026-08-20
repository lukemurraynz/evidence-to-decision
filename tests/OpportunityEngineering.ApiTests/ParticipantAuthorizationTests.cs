using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.ApiTests;

// The previous version of this suite hand-configured its own JwtSecurityTokenHandler with
// InboundClaimTypeMap manually cleared, asserting against a copy of what the pipeline was
// assumed to do: the exact class of gap that let a MapInboundClaims regression ship to
// production undetected (a real incident: CastVote failed with claims resolving to long
// ClaimTypes URIs instead of "sub"/"name" until that config was fixed). This suite instead
// boots the real app (WebApplicationFactory<Program>, same harness as EndpointWiringTests)
// and validates against the actually-configured JwtBearerOptions for the Participant scheme,
// so a future regression on that scheme fails this test instead of silently passing a
// hand-maintained guess.
[TestClass]
[DoNotParallelize]
public sealed class ParticipantAuthorizationTests
{
    [TestMethod]
    public async Task IssuedTokenResolvesToTheMintedParticipantContextThroughTheRealPipeline()
    {
        var principal = await IssueAndValidateAsync(CreateSession());
        var resolver = new ParticipantContextResolver();

        var participant = resolver.Resolve(principal, "workspace-1", "engagement-1", "correlation-1");

        Assert.AreEqual("participant-1", participant.ParticipantId);
        Assert.AreEqual("workspace-1", participant.WorkspaceId);
        Assert.AreEqual("engagement-1", participant.EngagementId);
        Assert.AreEqual("session-1", participant.JoinSessionId);
        Assert.AreEqual("Riley", participant.DisplayName);
    }

    [TestMethod]
    public async Task ResolveRejectsATokenReplayedAgainstADifferentEngagement()
    {
        var principal = await IssueAndValidateAsync(CreateSession());
        var resolver = new ParticipantContextResolver();

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            resolver.Resolve(principal, "workspace-1", "engagement-2", "correlation-1"));

        Assert.AreEqual("authorization.participant_access_denied", exception.Code);
    }

    [TestMethod]
    public async Task ResolveRejectsATokenReplayedAgainstADifferentWorkspace()
    {
        var principal = await IssueAndValidateAsync(CreateSession());
        var resolver = new ParticipantContextResolver();

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            resolver.Resolve(principal, "workspace-2", "engagement-1", "correlation-1"));

        Assert.AreEqual("authorization.participant_access_denied", exception.Code);
    }

    private static LiveSession CreateSession()
    {
        var now = DateTimeOffset.UtcNow;
        return new LiveSession(
            Id: "session-1",
            WorkspaceId: "workspace-1",
            EngagementId: "engagement-1",
            JourneyStepId: "step-1",
            JoinCode: "ABC123",
            CreatedBy: "facilitator-1",
            CreatedAt: now,
            ExpiresAt: now.AddHours(1),
            Status: "active");
    }

    /// <summary>Issues a token exactly as the join endpoint would, then validates it against a
    /// real app host's actual configured JwtBearerOptions for the Participant scheme, not a
    /// hand-maintained guess of what those options should be.</summary>
    private static async Task<ClaimsPrincipal> IssueAndValidateAsync(LiveSession session)
    {
        foreach (var (key, value) in EndpointWiringTests.TestConfiguration)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
        Environment.SetEnvironmentVariable(
            "Guardrails__PolicyPath",
            Path.Combine(AppContext.BaseDirectory, "guardrails", "policy.json"));
        try
        {
            // Clears the default Windows EventLog provider: a second WebApplicationFactory<Program>
            // booted in the same process (this one, after EndpointWiringTests') hits its disposed
            // static EventLog handle the moment JsonWebTokenHandler logs its benign "audience not
            // validated" warning, which JsonWebTokenHandler swallows as a validation failure.
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.ClearProviders()));
            var signingKey = EndpointWiringTests.TestConfiguration["Participant__SigningKey"];
            var issuer = new ParticipantTokenIssuer(
                new ParticipantTokenSettings { SigningKey = signingKey },
                TimeProvider.System);
            var token = issuer.Issue(session, "participant-1", "Riley");

            var tokenValidationParameters = factory.Services
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(ParticipantTokenIssuer.Scheme)
                .TokenValidationParameters;
            var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, tokenValidationParameters);
            Assert.IsTrue(result.IsValid, result.Exception?.ToString());

            return new ClaimsPrincipal(new ClaimsIdentity(result.ClaimsIdentity));
        }
        finally
        {
            foreach (var key in EndpointWiringTests.TestConfiguration.Keys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
            Environment.SetEnvironmentVariable("Guardrails__PolicyPath", null);
        }
    }
}
