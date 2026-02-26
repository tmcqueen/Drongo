namespace Drongo.Routing;

public interface IOutboundCallRoutePlan : IOutboundRoutePlan
{
    string NormalizedCallerId { get; }
    
    string NormalizedDestination { get; }
    
    string? AssignedEndpoint { get; }
}
