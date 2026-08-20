using OpportunityEngineering.Application.Ports;

namespace OpportunityEngineering.Infrastructure;

public sealed class SystemIdentifierFactory : IIdentifierFactory
{
    public string Create() => Guid.CreateVersion7().ToString("D");
}
