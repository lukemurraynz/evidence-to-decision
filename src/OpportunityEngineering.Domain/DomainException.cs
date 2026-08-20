namespace OpportunityEngineering.Domain;

/// <summary>Represents a rejected domain operation whose invariant must be shown to the caller.</summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
