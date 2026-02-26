namespace Drongo.Routing;

public interface IOutboundInteraction : IInteraction
{
    IOutboundRoute? OutboundRoute { get; }
    
    IOutboundRoutePlan? OutboundPlan { get; }
}
