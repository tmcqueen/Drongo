namespace Drongo.Routing;

public interface IOutboundCallRoute : IOutboundRoute
{
    string? TelcoPattern { get; }
    
    string CallerId { get; }
    
    string DestinationNumber { get; }
}
