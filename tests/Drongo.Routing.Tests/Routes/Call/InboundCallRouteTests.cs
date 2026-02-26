using Drongo.Routing;
using Drongo.Routing.Routes;
using Drongo.Routing.Routes.Call;
using Shouldly;

namespace Drongo.Routing.Tests.Routes.Call;

public class InboundCallRouteTests
{
    [Fact]
    public void InboundCallRoute_ImplementsInterface()
    {
        var route = new InboundCallRoute(
            Protocol: "sip",
            ReceiverAddress: "5551234@example.com",
            CallerId: "5551212",
            CalledNumber: "5551234",
            Dnis: "5551000"
        );
        
        route.ShouldBeAssignableTo<IInboundCallRoute>();
        route.CallerId.ShouldBe("5551212");
        route.CalledNumber.ShouldBe("5551234");
        route.Dnis.ShouldBe("5551000");
    }

    [Fact]
    public void InboundCallRoute_DnisIsOptional()
    {
        var route = new InboundCallRoute(
            Protocol: "sip",
            ReceiverAddress: "5551234@example.com",
            CallerId: "5551212",
            CalledNumber: "5551234"
        );
        
        route.Dnis.ShouldBeNull();
    }
}
