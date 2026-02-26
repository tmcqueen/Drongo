namespace Drongo.Routing;

public interface IInboundRoute : IRoute
{
    string NormalizedAddress { get; }
    
    string MatchedEndpoint { get; }
}
