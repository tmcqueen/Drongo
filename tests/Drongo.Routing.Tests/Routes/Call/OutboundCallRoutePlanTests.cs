using Drongo.Routing;
using Drongo.Routing.Routes;
using Drongo.Routing.Routes.Call;
using Shouldly;

namespace Drongo.Routing.Tests.Routes.Call;

public class OutboundCallRoutePlanTests
{
    [Fact]
    public void OutboundCallRoutePlan_ImplementsInterface()
    {
        var plan = new OutboundCallRoutePlan(
            NormalizedCallerId: "5551212",
            NormalizedDestination: "5551234",
            AssignedEndpoint: "sip:endpoint@example.com"
        );
        
        plan.ShouldBeAssignableTo<IOutboundCallRoutePlan>();
        plan.NormalizedCallerId.ShouldBe("5551212");
        plan.NormalizedDestination.ShouldBe("5551234");
        plan.AssignedEndpoint.ShouldBe("sip:endpoint@example.com");
    }
}
