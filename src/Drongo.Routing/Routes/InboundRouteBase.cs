using Drongo.Routing;

namespace Drongo.Routing.Routes;

public abstract class InboundRouteBase : RouteBase, IInboundRoute
{
    public string NormalizedAddress { get; init; } = string.Empty;
    public string MatchedEndpoint { get; init; } = string.Empty;
}
