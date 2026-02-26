using Drongo.Core.Dialogs;
using Drongo.Core.Messages;
using Drongo.Core.Prack;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Drongo.Core.Tests.Prack;

/// <summary>
/// Tests for PRACK integration with CallLegOrchestrator per RFC 3262.
/// </summary>
public class PrackIntegrationTests
{
    [Fact]
    public void RouteInDialogRequest_WithPrackRequest_ForwardsPrack()
    {
        // RED: Test that PRACK requests are forwarded in-dialog
        var logger = NullLogger<CallLeg>.Instance;
        var orchestrator = new CallLegOrchestrator(logger);

        var (uacLeg, uasLeg) = orchestrator.CreateCallLegPair(
            callId: "call-123",
            uacTag: "uac-tag",
            uasTag: "uas-tag",
            uacUri: new SipUri("sip", "caller.example.com"),
            uasUri: new SipUri("sip", "callee.example.com"),
            isSecure: false
        );

        var prackRequest = new SipRequest(
            SipMethod.Prack,
            new SipUri("sip", "callee.example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Call-ID", "call-123" },
                { "From", "<sip:caller@example.com>;tag=uac-tag" },
                { "To", "<sip:callee@example.com>;tag=uas-tag" },
                { "Rack", "1 INVITE 100" }
            }
        );

        var result = orchestrator.RouteInDialogRequest("call-123", prackRequest);

        Assert.NotNull(result);
    }

    [Fact]
    public void RouteInDialogRequest_WithPrackRequest_ReturnsNullForAck()
    {
        // RED: Test that ACK is not forwarded (consumed locally)
        var logger = NullLogger<CallLeg>.Instance;
        var orchestrator = new CallLegOrchestrator(logger);

        var (uacLeg, uasLeg) = orchestrator.CreateCallLegPair(
            callId: "call-123",
            uacTag: "uac-tag",
            uasTag: "uas-tag",
            uacUri: new SipUri("sip", "caller.example.com"),
            uasUri: new SipUri("sip", "callee.example.com"),
            isSecure: false
        );

        var ackRequest = new SipRequest(
            SipMethod.Ack,
            new SipUri("sip", "callee.example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Call-ID", "call-123" },
                { "From", "<sip:caller@example.com>;tag=uac-tag" },
                { "To", "<sip:callee@example.com>;tag=uas-tag" }
            }
        );

        var result = orchestrator.RouteInDialogRequest("call-123", ackRequest);

        Assert.Null(result);
    }

    [Fact]
    public void PrackProvider_TracksProvisionalResponse()
    {
        // RED: Test that PrackProvider tracks provisional responses
        var provider = new PrackProvider();

        var rseq1 = provider.TrackProvisionalResponse("call-123", 180);
        var rseq2 = provider.TrackProvisionalResponse("call-123", 180);

        Assert.Equal(1, rseq1);
        Assert.Equal(2, rseq2);
    }

    [Fact]
    public void PrackProvider_DoesNotTrack100Trying()
    {
        // RED: Test that 100 Trying does not require PRACK tracking
        var provider = new PrackProvider();

        var rseq = provider.TrackProvisionalResponse("call-123", 100);

        Assert.Equal(0, rseq);
    }

    [Fact]
    public void PrackProvider_AcknowledgesProvisionalResponse()
    {
        // RED: Test acknowledging a provisional response
        var provider = new PrackProvider();

        provider.TrackProvisionalResponse("call-123", 180);
        var acknowledged = provider.AcknowledgeProvisionalResponse("call-123", 1);

        Assert.True(acknowledged);
    }

    [Fact]
    public void PrackProvider_RejectsInvalidAcknowledgment()
    {
        // RED: Test rejecting acknowledgment for unknown RSeq
        var provider = new PrackProvider();

        provider.TrackProvisionalResponse("call-123", 180);
        var acknowledged = provider.AcknowledgeProvisionalResponse("call-123", 999);

        Assert.False(acknowledged);
    }

    [Fact]
    public void PrackProvider_GeneratesPrackRequest()
    {
        // RED: Test PRACK request generation from provider
        var provider = new PrackProvider();

        provider.TrackProvisionalResponse("call-123", 180);

        var prackRequest = provider.GeneratePrackRequest(
            dialogId: "call-123",
            targetUri: new SipUri("sip", "callee.example.com"),
            localUri: new SipUri("sip", "caller.example.com"),
            remoteUri: new SipUri("sip", "callee.example.com")
        );

        Assert.NotNull(prackRequest);
        Assert.Equal(SipMethod.Prack, prackRequest.Method);
        Assert.True(prackRequest.HasHeader("Rack"));
    }
}
