namespace Drongo.Routing;

public interface IRoutingEngine
{
    IRoutePlan? Route(IInboundRoute route, string destinationAddress, string? senderAddress = null);
}
