using System.Security.Claims;
using OpportunityEngineering.Api.Authorization;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.ApiTests;

[TestClass]
public sealed class WorkspaceAuthorizationTests
{
    private const string FacilitatorGroup = "11111111-1111-1111-1111-111111111111";
    private const string ReviewerGroup = "22222222-2222-2222-2222-222222222222";

    [TestMethod]
    public void ResolveAllowsMappedWorkspaceGroup()
    {
        var resolver = CreateResolver();
        var principal = CreatePrincipal(FacilitatorGroup);

        var actor = resolver.Resolve(principal, "workspace-1", "correlation-1");

        Assert.AreEqual("actor-1", actor.ActorId);
        Assert.AreEqual("workspace-1", actor.WorkspaceId);
        CollectionAssert.AreEquivalent(
            new[] { ApplicationRole.Facilitator },
            actor.Roles.ToArray());
    }

    [TestMethod]
    public void ResolveGrantsUnionOfRolesWhenMultipleGroupsMatch()
    {
        var resolver = CreateResolver();
        var principal = CreatePrincipal(FacilitatorGroup, ReviewerGroup);

        var actor = resolver.Resolve(principal, "workspace-1", "correlation-1");

        CollectionAssert.AreEquivalent(
            new[] { ApplicationRole.Facilitator, ApplicationRole.Reviewer },
            actor.Roles.ToArray());
        Assert.IsTrue(actor.Has(ApplicationRole.Facilitator));
        Assert.IsTrue(actor.Has(ApplicationRole.Reviewer));
    }

    [TestMethod]
    public void ResolveDeniesGroupMappedOnlyToAnotherWorkspace()
    {
        var resolver = CreateResolver();
        var principal = CreatePrincipal("44444444-4444-4444-4444-444444444444");

        var exception = Assert.ThrowsExactly<DomainException>(() =>
            resolver.Resolve(principal, "workspace-1", "correlation-1"));

        Assert.AreEqual("authorization.workspace_access_denied", exception.Code);
    }

    [TestMethod]
    public void ValidateRejectsOverlappingRoleMappings()
    {
        var settings = new WorkspaceAuthorizationSettings
        {
            Workspaces = new Dictionary<string, WorkspaceRoleMapping>
            {
                ["workspace-1"] = new()
                {
                    FacilitatorGroupObjectIds = [FacilitatorGroup],
                    ReviewerGroupObjectIds = [FacilitatorGroup]
                }
            }
        };

        _ = Assert.ThrowsExactly<InvalidOperationException>(settings.Validate);
    }

    private static WorkspaceActorResolver CreateResolver()
    {
        var settings = new WorkspaceAuthorizationSettings
        {
            Workspaces = new Dictionary<string, WorkspaceRoleMapping>
            {
                ["workspace-1"] = new()
                {
                    FacilitatorGroupObjectIds = [FacilitatorGroup],
                    ReviewerGroupObjectIds = [ReviewerGroup]
                }
            }
        };
        settings.Validate();
        return new WorkspaceActorResolver(settings);
    }

    private static ClaimsPrincipal CreatePrincipal(params string[] groups)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim("oid", "actor-1"),
                .. groups.Select(group => new Claim("groups", group))
            ],
            "test");
        return new ClaimsPrincipal(identity);
    }
}
