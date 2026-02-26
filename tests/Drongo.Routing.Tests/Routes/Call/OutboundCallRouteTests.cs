using Drongo.Routing;
using Drongo.Routing.Routes;
using Drongo.Routing.Routes.Call;
using Shouldly;

namespace Drongo.Routing.Tests.Routes.Call;

public class OutboundCallRouteTests
{
    [Fact]
    public void OutboundCallRoute_ImplementsInterface()
    {
        var route = new OutboundCallRoute(
            Protocol: "sip",
            ReceiverAddress: "NxxXXXX@example.com",
            TelcoPattern: "NxxXXXX",
            CallerId: "5551212",
            DestinationNumber: "5551234"
        );
        
        route.ShouldBeAssignableTo<IOutboundCallRoute>();
        route.TelcoPattern.ShouldBe("NxxXXXX");
        route.CallerId.ShouldBe("5551212");
        route.DestinationNumber.ShouldBe("5551234");
    }
}
