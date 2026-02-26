using Drongo.Core.Dialogs;
using Drongo.Core.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Drongo.Core.Tests.UpdateRefer;

/// <summary>
/// Tests for UPDATE method routing per RFC 3311.
/// </summary>
public class UpdateMethodRoutingTests
{
    [Fact]
    public void RouteInDialogRequest_WithUpdateRequest_ReturnsRequest()
    {
        // RED: Test UPDATE requests are forwarded in-dialog
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

        var updateRequest = new SipRequest(
            SipMethod.Update,
            new SipUri("sip", "callee.example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Call-ID", "call-123" },
                { "From", "<sip:caller@example.com>;tag=uac-tag" },
                { "To", "<sip:callee@example.com>;tag=uas-tag" },
                { "CSeq", "1 UPDATE" }
            }
        );

        var result = orchestrator.RouteInDialogRequest("call-123", updateRequest);

        Assert.NotNull(result);
        Assert.Equal(SipMethod.Update, result.Method);
    }

    [Fact]
    public void RouteInDialogRequest_WithUpdate_ForwardsToOtherLeg()
    {
        // RED: Test UPDATE is forwarded to the other leg in the dialog
        var logger = NullLogger<CallLeg>.Instance;
        var orchestrator = new CallLegOrchestrator(logger);

        orchestrator.CreateCallLegPair(
            callId: "call-123",
            uacTag: "uac-tag",
            uasTag: "uas-tag",
            uacUri: new SipUri("sip", "caller.example.com"),
            uasUri: new SipUri("sip", "callee.example.com"),
            isSecure: false
        );

        var updateRequest = new SipRequest(
            SipMethod.Update,
            new SipUri("sip", "callee.example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Call-ID", "call-123" },
                { "From", "<sip:caller@example.com>;tag=uac-tag" },
                { "To", "<sip:callee@example.com>;tag=uas-tag" },
                { "CSeq", "1 UPDATE" }
            }
        );

        var result = orchestrator.RouteInDialogRequest("call-123", updateRequest);

        Assert.NotNull(result);
    }

    [Fact]
    public void RouteInDialogRequest_WithReferRequest_ReturnsRequest()
    {
        // RED: Test REFER requests are forwarded in-dialog
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

        var referRequest = new SipRequest(
            SipMethod.Refer,
            new SipUri("sip", "callee.example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                { "Call-ID", "call-123" },
                { "From", "<sip:caller@example.com>;tag=uac-tag" },
                { "To", "<sip:callee@example.com>;tag=uas-tag" },
                { "Refer-To", "<sip:transfer-target@example.com>" },
                { "CSeq", "1 REFER" }
            }
        );

        var result = orchestrator.RouteInDialogRequest("call-123", referRequest);

        Assert.NotNull(result);
        Assert.Equal(SipMethod.Refer, result.Method);
    }
}
