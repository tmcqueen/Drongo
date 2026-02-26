using Drongo.Routing;

namespace Drongo.Routing.Routes;

public abstract class OutboundRoutePlanBase : RoutePlanBase, IOutboundRoutePlan
{
    public string NormalizedAddress { get; init; } = string.Empty;
    public string AssignedHost { get; init; } = string.Empty;
}
