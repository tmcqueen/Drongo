namespace Drongo.Routing;

public interface IOutboundRoute : IRoute
{
    string MatchedPattern { get; }
    
    string? Transformer { get; }
}
