namespace Drongo.Routing;

public interface IInboundRoutePlan : IRoutePlan
{
    string MatchedEndpoint { get; }
    
    string? TransformedEndpoint { get; }
}
