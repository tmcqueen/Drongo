namespace Drongo.Routing;

public interface IOutboundRoutePlan : IRoutePlan
{
    string NormalizedAddress { get; }
    
    string AssignedHost { get; }
}
