using Drongo.Routing;

namespace Drongo.Routing.Routes;

public abstract class OutboundRouteBase : RouteBase, IOutboundRoute
{
    public string MatchedPattern { get; init; } = string.Empty;
    public string? Transformer { get; init; }
}
