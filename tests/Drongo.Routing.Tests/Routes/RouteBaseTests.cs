using Drongo.Routing;
using Drongo.Routing.Routes;
using Shouldly;

namespace Drongo.Routing.Tests.Routes;

public class RouteBaseTests
{
    [Fact]
    public void RouteBase_ImplementsIRoute()
    {
        var route = new TestRoute();
        route.ShouldBeAssignableTo<IRoute>();
    }
    
    private sealed class TestRoute : RouteBase
    {
    }
}
