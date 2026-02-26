using Drongo.Routing;
using Drongo.Routing.Routes;
using Drongo.Routing.Telco;
using Shouldly;

namespace Drongo.Routing.Tests;

public class RoutingEngineTests
{
    [Fact]
    public void RoutingEngine_ImplementsIRoutingEngine()
    {
        var engine = new RoutingEngine();
        engine.ShouldBeAssignableTo<IRoutingEngine>();
    }

    [Fact]
    public void Route_ReturnsRoutePlan_WhenDestinationMatches()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "1XXXXXXXXXX",
            SenderAddress = "*"
        };

        var result = engine.Route(route, "+12025551234");

        result.ShouldNotBeNull();
        result.Classification.ShouldNotBeNull();
    }

    [Fact]
    public void Route_ReturnsNull_WhenNoRouteMatches()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "44XXXXXXXXXX",
            SenderAddress = "*"
        };

        var result = engine.Route(route, "+12025551234");

        result.ShouldBeNull();
    }

    [Fact]
    public void Route_SetsClassification_Local()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "1202XXXXXXX",
            SenderAddress = "*"
        };

        var result = engine.Route(route, "12025551234");

        result.ShouldNotBeNull();
        result.Classification.Code.ShouldBe("LOCAL");
    }

    [Fact]
    public void Route_SetsClassification_TollFree()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "1800XXXXXXX",
            SenderAddress = "*"
        };

        var result = engine.Route(route, "18005551234");

        result.ShouldNotBeNull();
        result.Classification.Code.ShouldBe("TOLLFREE");
    }

    [Fact]
    public void Route_SetsClassification_International()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "44XXXXXXXXXX",
            SenderAddress = "*"
        };

        var result = engine.Route(route, "+442071838371");

        result.ShouldNotBeNull();
        result.Classification.Code.ShouldBe("INTERNATIONAL");
    }

    [Fact]
    public void Route_SetsAuthorization_True_WhenSenderAllowed()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "*",
            SenderAddress = "1202*"
        };

        var result = engine.Route(route, "+12025551234", "+12025559876");

        result.ShouldNotBeNull();
        result.IsAuthorized.ShouldBeTrue();
    }

    [Fact]
    public void Route_SetsAuthorization_False_WhenSenderNotAllowed()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "*",
            SenderAddress = "1202*"
        };

        var result = engine.Route(route, "+12025551234", "+13015559876");

        result.ShouldNotBeNull();
        result.IsAuthorized.ShouldBeFalse();
    }

    [Fact]
    public void Route_NormalizesAddress()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "1XXXXXXXXXX",
            SenderAddress = "*"
        };

        var result = engine.Route(route, "(202) 555-1234");

        result.ShouldNotBeNull();
        result.TransformedAddress.ShouldBe("+12025551234");
    }

    [Fact]
    public void Route_AssignsDestinationHost()
    {
        var engine = new RoutingEngine();
        var route = new TestInboundRoute
        {
            OrganizationId = "org1",
            Protocol = "SIP",
            ReceiverAddress = "1XXXXXXXXXX",
            SenderAddress = "*",
            Location = "endpoint1"
        };

        var result = engine.Route(route, "+12025551234");

        result.ShouldNotBeNull();
        result.DestinationHost.ShouldBe("endpoint1");
    }

    private sealed class TestInboundRoute : InboundRouteBase
    {
    }
}
