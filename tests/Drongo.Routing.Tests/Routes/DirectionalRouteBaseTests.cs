using Drongo.Routing;
using Drongo.Routing.Routes;
using Shouldly;

namespace Drongo.Routing.Tests.Routes;

public class DirectionalRouteBaseTests
{
    [Fact]
    public void OutboundRouteBase_ImplementsIOutboundRoute()
    {
        var route = new TestOutboundRoute();
        route.ShouldBeAssignableTo<IOutboundRoute>();
    }

    [Fact]
    public void InboundRouteBase_ImplementsIInboundRoute()
    {
        var route = new TestInboundRoute();
        route.ShouldBeAssignableTo<IInboundRoute>();
    }

    private sealed class TestOutboundRoute : OutboundRouteBase
    {
    }

    private sealed class TestInboundRoute : InboundRouteBase
    {
    }
}
