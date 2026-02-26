namespace Drongo.Routing.Routes.Call;

public sealed class OutboundCallRoutePlan(
    string NormalizedCallerId,
    string NormalizedDestination,
    string? AssignedEndpoint) : OutboundRoutePlanBase, IOutboundCallRoutePlan
{
    public string NormalizedCallerId { get; init; } = NormalizedCallerId;
    public string NormalizedDestination { get; init; } = NormalizedDestination;
    public string? AssignedEndpoint { get; init; } = AssignedEndpoint;
}
