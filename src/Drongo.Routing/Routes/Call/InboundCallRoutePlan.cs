namespace Drongo.Routing.Routes.Call;

public sealed class InboundCallRoutePlan(
    string NormalizedCallerId,
    string NormalizedCalledNumber,
    string AssignedEndpoint) : InboundRoutePlanBase, IInboundCallRoutePlan
{
    public string NormalizedCallerId { get; init; } = NormalizedCallerId;
    public string NormalizedCalledNumber { get; init; } = NormalizedCalledNumber;
    public string AssignedEndpoint { get; init; } = AssignedEndpoint;
}
