namespace Drongo.Routing;

public interface IInboundInteraction : IInteraction
{
    IInboundRoute? InboundRoute { get; }
    
    IInboundRoutePlan? InboundPlan { get; }
}
