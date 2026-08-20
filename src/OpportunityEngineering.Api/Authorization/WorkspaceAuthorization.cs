using System.Security.Claims;
using OpportunityEngineering.Application.Contracts;
using OpportunityEngineering.Domain;

namespace OpportunityEngineering.Api.Authorization;

public sealed record WorkspaceRoleMapping
{
    public required IReadOnlyList<string> FacilitatorGroupObjectIds { get; init; }
    public required IReadOnlyList<string> ReviewerGroupObjectIds { get; init; }
}

public sealed record WorkspaceAuthorizationSettings
{
    public required IReadOnlyDictionary<string, WorkspaceRoleMapping> Workspaces { get; init; }

    public void Validate()
    {
        if (Workspaces.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one workspace authorization mapping is required.");
        }

        foreach (var (workspaceId, mapping) in Workspaces)
        {
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                throw new InvalidOperationException("Workspace IDs cannot be empty.");
            }

            var groupIds = mapping.FacilitatorGroupObjectIds
                .Concat(mapping.ReviewerGroupObjectIds)
                .ToArray();
            if (groupIds.Length == 0 ||
                groupIds.Any(item => !Guid.TryParse(item, out _)) ||
                groupIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != groupIds.Length)
            {
                throw new InvalidOperationException(
                    $"Workspace '{workspaceId}' has invalid or overlapping group object IDs.");
            }
        }
    }
}

public sealed class WorkspaceActorResolver(WorkspaceAuthorizationSettings settings)
{
    public ActorContext Resolve(
        ClaimsPrincipal principal,
        string workspaceId,
        string correlationId)
    {
        var actorId = principal.FindFirstValue("oid")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw Denied();
        if (!settings.Workspaces.TryGetValue(workspaceId, out var mapping))
        {
            throw Denied();
        }

        var groups = principal.FindAll("groups")
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roles = ResolveRoles(mapping, groups);
        return new ActorContext(actorId, workspaceId, roles, correlationId);
    }

    // A principal can legitimately belong to more than one role's group at once (a
    // facilitator who also reviews), so every matched role is granted rather than
    // collapsing to a single winner. Each capability check downstream (e.g.
    // GraphCommandService.RequireFacilitatorMutation) tests for the specific role it
    // needs via ActorContext.Has, so granting the full set never over-authorizes a
    // single action; it only lets a dual-role actor use either capability.
    private static HashSet<ApplicationRole> ResolveRoles(
        WorkspaceRoleMapping mapping,
        HashSet<string> groups)
    {
        var roles = new HashSet<ApplicationRole>();
        if (mapping.ReviewerGroupObjectIds.Any(groups.Contains))
        {
            roles.Add(ApplicationRole.Reviewer);
        }
        if (mapping.FacilitatorGroupObjectIds.Any(groups.Contains))
        {
            roles.Add(ApplicationRole.Facilitator);
        }
        return roles.Count > 0 ? roles : throw Denied();
    }

    private static DomainException Denied() =>
        new(
            "authorization.workspace_access_denied",
            "The authenticated user is not authorized for this workspace.");
}
