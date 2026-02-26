using Drongo.Routing;
using Drongo.Routing.Routes;
using Shouldly;

namespace Drongo.Routing.Tests.Routes;

public class RoutePlanBaseTests
{
    [Fact]
    public void OutboundRoutePlanBase_ImplementsIOutboundRoutePlan()
    {
        var plan = new TestOutboundRoutePlan();
        plan.ShouldBeAssignableTo<IOutboundRoutePlan>();
    }

    [Fact]
    public void InboundRoutePlanBase_ImplementsIInboundRoutePlan()
    {
        var plan = new TestInboundRoutePlan();
        plan.ShouldBeAssignableTo<IInboundRoutePlan>();
    }

    private sealed class TestOutboundRoutePlan : OutboundRoutePlanBase
    {
    }

    private sealed class TestInboundRoutePlan : InboundRoutePlanBase
    {
    }
}
