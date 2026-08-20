namespace OpportunityEngineering.Application.Contracts;

// Deliberately just two roles: a Facilitator runs the workshop and has full control over
// their own engagement (including deleting it), a Reviewer approves decisions. There is no
// separate Admin tier. A facilitator already needs full authority over the engagements they
// run, and a distinct "admin who isn't a facilitator" persona has no use in this app.
public enum ApplicationRole
{
    Facilitator,
    Reviewer
}

public sealed record ActorContext(
    string ActorId,
    string WorkspaceId,
    IReadOnlySet<ApplicationRole> Roles,
    string CorrelationId)
{
    public bool Has(ApplicationRole role) => Roles.Contains(role);
}
