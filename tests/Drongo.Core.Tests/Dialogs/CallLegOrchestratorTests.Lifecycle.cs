using Drongo.Core.SIP.Dialogs;
using Drongo.Core.SIP.Messages;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Dialogs;

/// <summary>
/// Tests for dialog lifecycle: termination, edge cases, and state transitions.
/// Block 4: Dialog Lifecycle & Cleanup (b4-r1, b4-r2, b4-r3)
/// 
/// Verifies:
/// - b4-r1: Dialog termination via BYE (state transitions to Terminating/Terminated)
/// - b4-r2: Edge cases (duplicate ACK, out-of-order requests)
/// - b4-r3: Comprehensive lifecycle tests
/// </summary>
public partial class CallLegOrchestratorTests
{
    #region b4-r1: Dialog Termination via BYE

    [Fact]
    public void RouteFinalResponse_With200OkToBye_DialogBecomesTerminated()
    {
        var callId = "call-bye-terminate-001";
        var uacTag = "uac-bye-001";
        var uasTag = "uas-bye-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        _orchestrator.RouteFinalResponse(callId, confirmResponse);

        _orchestrator.IsDialogConfirmed(callId).ShouldBeTrue();

        var byeRequest = new SipRequest(
            SipMethod.Bye,
            uasUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });
        _orchestrator.RouteInDialogRequest(callId, byeRequest);

        var byeOkResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });
        var routedResponse = _orchestrator.RouteFinalResponse(callId, byeOkResponse);

        routedResponse.ShouldNotBeNull();
        routedResponse!.StatusCode.ShouldBe(200);

        _orchestrator.TryGetCallLegs(callId, out var uacLeg, out var uasLeg).ShouldBeTrue();
        uacLeg.ShouldNotBeNull();
        uasLeg.ShouldNotBeNull();
        
        uacLeg!.State.ShouldBe(CallLegState.Terminated);
        uasLeg!.State.ShouldBe(CallLegState.Terminated);
    }

    [Fact]
    public void RouteInDialogRequest_WithByeOnConfirmedDialog_TransitionsToTerminating()
    {
        var callId = "call-bye-terminating-001";
        var uacTag = "uac-bye-001";
        var uasTag = "uas-bye-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        _orchestrator.RouteFinalResponse(callId, confirmResponse);

        var byeRequest = new SipRequest(
            SipMethod.Bye,
            uasUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });
        _orchestrator.RouteInDialogRequest(callId, byeRequest);

        _orchestrator.TryGetCallLegs(callId, out var uacLeg, out var uasLeg).ShouldBeTrue();
        uacLeg.ShouldNotBeNull();
        uasLeg.ShouldNotBeNull();

        uacLeg!.State.ShouldBe(CallLegState.Terminating);
        uasLeg!.State.ShouldBe(CallLegState.Terminating);
    }

    #endregion

    #region b4-r2: Edge Cases

    [Fact]
    public void RouteInDialogRequest_WithDuplicateByeOnTerminatingDialog_HandlesGracefully()
    {
        var callId = "call-dup-bye-001";
        var uacTag = "uac-bye-001";
        var uasTag = "uas-bye-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        _orchestrator.RouteFinalResponse(callId, confirmResponse);

        var byeRequest = new SipRequest(
            SipMethod.Bye,
            uasUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });

        _orchestrator.RouteInDialogRequest(callId, byeRequest);

        var duplicateByeRequest = new SipRequest(
            SipMethod.Bye,
            uasUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });

        var result = _orchestrator.RouteInDialogRequest(callId, duplicateByeRequest);

        result.ShouldNotBeNull();
        _orchestrator.TryGetCallLegs(callId, out var uacLeg, out var uasLeg).ShouldBeTrue();
        uacLeg!.State.ShouldBe(CallLegState.Terminating);
        uasLeg!.State.ShouldBe(CallLegState.Terminating);
    }

    [Fact]
    public void RouteInDialogRequest_WithDuplicateAck_HandlesIdempotently()
    {
        var callId = "call-dup-ack-001";
        var uacTag = "uac-ack-001";
        var uasTag = "uas-ack-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        _orchestrator.RouteFinalResponse(callId, confirmResponse);

        var ackRequest = new SipRequest(
            SipMethod.Ack,
            uasUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 ACK"
            });

        var result1 = _orchestrator.RouteInDialogRequest(callId, ackRequest);
        result1.ShouldBeNull();

        var result2 = _orchestrator.RouteInDialogRequest(callId, ackRequest);
        result2.ShouldBeNull();

        _orchestrator.TryGetCallLegs(callId, out var uacLeg, out var uasLeg).ShouldBeTrue();
        uacLeg!.State.ShouldBe(CallLegState.Confirmed);
        uasLeg!.State.ShouldBe(CallLegState.Confirmed);
    }

    #endregion

    #region b4-r3: Lifecycle Integration Tests

    [Fact]
    public void FullDialogLifecycle_InviteThroughByeAcknowledgement_TransitionsCorrectly()
    {
        var callId = "call-lifecycle-001";
        var uacTag = "uac-001";
        var uasTag = "uas-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        _orchestrator.TryGetCallLegs(callId, out var leg1, out var leg2).ShouldBeTrue();
        leg1!.State.ShouldBe(CallLegState.Initial);
        leg2!.State.ShouldBe(CallLegState.Initial);

        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        _orchestrator.RouteFinalResponse(callId, confirmResponse);

        _orchestrator.TryGetCallLegs(callId, out leg1, out leg2).ShouldBeTrue();
        leg1!.State.ShouldBe(CallLegState.Confirmed);
        leg2!.State.ShouldBe(CallLegState.Confirmed);

        var byeRequest = new SipRequest(
            SipMethod.Bye,
            uasUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });
        _orchestrator.RouteInDialogRequest(callId, byeRequest);

        _orchestrator.TryGetCallLegs(callId, out leg1, out leg2).ShouldBeTrue();
        leg1!.State.ShouldBe(CallLegState.Terminating);
        leg2!.State.ShouldBe(CallLegState.Terminating);

        var byeOkResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });
        _orchestrator.RouteFinalResponse(callId, byeOkResponse);

        _orchestrator.TryGetCallLegs(callId, out leg1, out leg2).ShouldBeTrue();
        leg1!.State.ShouldBe(CallLegState.Terminated);
        leg2!.State.ShouldBe(CallLegState.Terminated);
    }

    #endregion
}
