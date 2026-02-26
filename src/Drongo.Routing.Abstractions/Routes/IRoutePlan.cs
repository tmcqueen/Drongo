namespace Drongo.Routing;

public interface IRoutePlan
{
    string Id { get; }
    
    string RouteId { get; }
    
    RouteClassification Classification { get; }
    
    bool IsAuthorized { get; }
    
    string? DestinationEndpoint { get; }
    
    string? TransformedAddress { get; }
    
    string? DestinationHost { get; }
}

public record RouteClassification(string Name, string Code);
