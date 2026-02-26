using Drongo.Routing;

namespace Drongo.Routing.Routes;

public abstract class RoutePlanBase : IRoutePlan
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public RouteClassification Classification { get; init; } = new(string.Empty, string.Empty);
    public bool IsAuthorized { get; init; }
    public string? DestinationEndpoint { get; init; }
    public string? TransformedAddress { get; init; }
    public string? DestinationHost { get; init; }
}
