namespace Drongo.Routing.Routes.Call;

public sealed class InboundCallRoute : InboundRouteBase, IInboundCallRoute
{
    public InboundCallRoute(
        string Protocol,
        string ReceiverAddress,
        string CallerId,
        string CalledNumber,
        string? Dnis = null)
    {
        this.Protocol = Protocol;
        this.ReceiverAddress = ReceiverAddress;
        this.CallerId = CallerId;
        this.CalledNumber = CalledNumber;
        this.Dnis = Dnis;
    }

    public string CallerId { get; init; }
    public string CalledNumber { get; init; }
    public string? Dnis { get; init; }
}
