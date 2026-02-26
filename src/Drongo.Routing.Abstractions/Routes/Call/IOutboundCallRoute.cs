namespace Drongo.Routing;

public interface IOutboundCallRoute : IOutboundRoute
{
    string CallerId { get; }
    
    string DestinationNumber { get; }
}
