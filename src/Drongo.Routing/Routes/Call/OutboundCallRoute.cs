namespace Drongo.Routing.Routes.Call;

public sealed class OutboundCallRoute : OutboundRouteBase, IOutboundCallRoute
{
    public OutboundCallRoute(
        string Protocol,
        string ReceiverAddress,
        string? TelcoPattern,
        string CallerId,
        string DestinationNumber)
    {
        this.Protocol = Protocol;
        this.ReceiverAddress = ReceiverAddress;
        this.TelcoPattern = TelcoPattern;
        this.CallerId = CallerId;
        this.DestinationNumber = DestinationNumber;
    }

    public string? TelcoPattern { get; init; }
    public string CallerId { get; init; }
    public string DestinationNumber { get; init; }
}
