using Drongo.Routing;

namespace Drongo.Routing.Routes;

public abstract class InboundRoutePlanBase : RoutePlanBase, IInboundRoutePlan
{
    public string MatchedEndpoint { get; init; } = string.Empty;
    public string? TransformedEndpoint { get; init; }
}
