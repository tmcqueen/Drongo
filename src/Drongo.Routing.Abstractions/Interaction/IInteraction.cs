namespace Drongo.Routing;

public interface IInteraction
{
    string Id { get; }
    
    DateTimeOffset CreatedAt { get; }
    
    IRoute? Route { get; }
    
    IRoutePlan? Plan { get; }
}
