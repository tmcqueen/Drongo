using Drongo.Core.Messages;
using Drongo.Core.Prack;
using Xunit;

namespace Drongo.Core.Tests.Prack;

/// <summary>
/// Tests for PRACK request generation per RFC 3262.
/// </summary>
public class PrackRequestGeneratorTests
{
    [Fact]
    public void GeneratePrackRequest_WithRSeqAndCSeq_ReturnsValidPrackRequest()
    {
        // RED: Test generates a valid PRACK request with Rack header
        var generator = new PrackRequestGenerator();
        
        var prack = generator.GeneratePrackRequest(
            dialogId: "dialog-123",
            rseq: 1,
            method: "INVITE",
            cseq: 100,
            targetUri: new SipUri("sip", "callee.example.com"),
            localUri: new SipUri("sip", "caller.example.com"),
            remoteUri: new SipUri("sip", "callee.example.com"),
            routeHeaders: new Dictionary<string, string>()
        );
        
        Assert.NotNull(prack);
        Assert.Equal(SipMethod.Prack, prack.Method);
        Assert.True(prack.HasHeader("Rack"));
        Assert.Contains("1 INVITE 100", prack.Headers["Rack"]);
    }

    [Fact]
    public void GeneratePrackRequest_IncludesCorrectHeaders()
    {
        // RED: Test PRACK request includes required headers
        var generator = new PrackRequestGenerator();
        
        var prack = generator.GeneratePrackRequest(
            dialogId: "dialog-123",
            rseq: 5,
            method: "INVITE",
            cseq: 50,
            targetUri: new SipUri("sip", "b.example.com"),
            localUri: new SipUri("sip", "a.example.com"),
            remoteUri: new SipUri("sip", "b.example.com"),
            routeHeaders: new Dictionary<string, string>(),
            viaHeader: "SIP/2.0/UDP 10.0.0.1:5060;branch=z9hG4bK-abc123"
        );
        
        Assert.True(prack.HasHeader("Via"));
        Assert.True(prack.HasHeader("From"));
        Assert.True(prack.HasHeader("To"));
        Assert.True(prack.HasHeader("Call-ID"));
        Assert.True(prack.HasHeader("CSeq"));
    }

    [Fact]
    public void GeneratePrackRequest_WithRouteHeaders_IncludesRoutes()
    {
        // RED: Test PRACK includes route headers when provided
        var generator = new PrackRequestGenerator();
        var routes = new Dictionary<string, string>
        {
            { "Route", "<sip:proxy.example.com;lr>" }
        };
        
        var prack = generator.GeneratePrackRequest(
            dialogId: "dialog-123",
            rseq: 1,
            method: "INVITE",
            cseq: 100,
            targetUri: new SipUri("sip", "callee.example.com"),
            localUri: new SipUri("sip", "caller.example.com"),
            remoteUri: new SipUri("sip", "callee.example.com"),
            routeHeaders: routes,
            viaHeader: "SIP/2.0/UDP 10.0.0.1:5060;branch=z9hG4bK-abc123"
        );
        
        Assert.True(prack.HasHeader("Route"));
    }

    [Fact]
    public void GeneratePrackRequest_WithDifferentMethod_GeneratesCorrectRack()
    {
        // RED: Test PRACK with different method (not INVITE)
        var generator = new PrackRequestGenerator();
        
        var prack = generator.GeneratePrackRequest(
            dialogId: "dialog-123",
            rseq: 3,
            method: "UPDATE",
            cseq: 200,
            targetUri: new SipUri("sip", "callee.example.com"),
            localUri: new SipUri("sip", "caller.example.com"),
            remoteUri: new SipUri("sip", "callee.example.com"),
            routeHeaders: new Dictionary<string, string>()
        );
        
        var rackHeader = prack.Headers["Rack"];
        Assert.Contains("3 UPDATE 200", rackHeader);
    }

    [Fact]
    public void GeneratePrackRequest_IncrementsCSeq()
    {
        // RED: Test PRACK request increments CSeq from original request
        var generator = new PrackRequestGenerator();
        
        var prack = generator.GeneratePrackRequest(
            dialogId: "dialog-123",
            rseq: 1,
            method: "INVITE",
            cseq: 100,
            targetUri: new SipUri("sip", "callee.example.com"),
            localUri: new SipUri("sip", "caller.example.com"),
            remoteUri: new SipUri("sip", "callee.example.com"),
            routeHeaders: new Dictionary<string, string>()
        );
        
        var cseqHeader = prack.Headers["CSeq"];
        Assert.StartsWith("101", cseqHeader);
    }

    [Fact]
    public void ParsePrackRequest_WithValidRack_ParsesCorrectly()
    {
        // RED: Test parsing a PRACK request to extract RSeq and CSeq info
        var parser = new PrackRequestParser();
        
        var prackRequest = new SipRequest(
            SipMethod.Prack,
            new SipUri("sip", "example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Rack", "5 INVITE 100" },
                { "Call-ID", "call-123" },
                { "From", "<sip:caller@example.com>;tag=abc" },
                { "To", "<sip:callee@example.com>;tag=xyz" },
                { "CSeq", "101 PRACK" }
            }
        );
        
        var rackInfo = parser.ParseRackFromRequest(prackRequest);
        
        Assert.NotNull(rackInfo);
        Assert.Equal(5, rackInfo.RSeq);
        Assert.Equal("INVITE", rackInfo.Method);
        Assert.Equal(100, rackInfo.CSeq);
    }

    [Fact]
    public void ParsePrackRequest_WithoutRackHeader_Throws()
    {
        // RED: Test parsing PRACK without Rack header throws
        var parser = new PrackRequestParser();
        
        var prackRequest = new SipRequest(
            SipMethod.Prack,
            new SipUri("sip", "example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Call-ID", "call-123" },
                { "From", "<sip:caller@example.com>;tag=abc" },
                { "To", "<sip:callee@example.com>;tag=xyz" }
            }
        );
        
        Assert.Throws<InvalidOperationException>(() => parser.ParseRackFromRequest(prackRequest));
    }
}
