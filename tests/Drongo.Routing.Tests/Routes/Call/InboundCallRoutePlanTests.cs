using Drongo.Routing;
using Drongo.Routing.Routes;
using Drongo.Routing.Routes.Call;
using Shouldly;

namespace Drongo.Routing.Tests.Routes.Call;

public class InboundCallRoutePlanTests
{
    [Fact]
    public void InboundCallRoutePlan_ImplementsInterface()
    {
        var plan = new InboundCallRoutePlan(
            NormalizedCallerId: "5551212",
            NormalizedCalledNumber: "5551234",
            AssignedEndpoint: "sip:endpoint@example.com"
        );
        
        plan.ShouldBeAssignableTo<IInboundCallRoutePlan>();
        plan.NormalizedCallerId.ShouldBe("5551212");
        plan.NormalizedCalledNumber.ShouldBe("5551234");
        plan.AssignedEndpoint.ShouldBe("sip:endpoint@example.com");
    }
}
