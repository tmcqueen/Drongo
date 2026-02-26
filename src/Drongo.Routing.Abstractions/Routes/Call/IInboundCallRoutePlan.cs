namespace Drongo.Routing;

public interface IInboundCallRoutePlan : IInboundRoutePlan
{
    string NormalizedCallerId { get; }
    
    string NormalizedCalledNumber { get; }
    
    string AssignedEndpoint { get; }
}
