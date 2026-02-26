namespace Drongo.Routing;

public interface IRoutePlan
{
    string Id { get; }

    RouteClassification Classification { get; }

    bool IsAuthorized { get; }

    string? DestinationEndpoint { get; }

    string? TransformedAddress { get; }

    string? DestinationHost { get; }
}
