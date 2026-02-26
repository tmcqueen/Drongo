namespace Drongo.Routing;

public interface IInboundCallRoute : IInboundRoute
{
    string CallerId { get; }
    
    string CalledNumber { get; }
    
    string? Dnis { get; }
}
