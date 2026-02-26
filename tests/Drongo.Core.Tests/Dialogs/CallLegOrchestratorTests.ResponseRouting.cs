using Drongo.Core.SIP.Dialogs;
using Drongo.Core.SIP.Messages;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Dialogs;

/// <summary>
/// Tests for response routing and B2BUA state machine behavior.
/// Covers all RFC3261 response code categories (1xx-6xx), late responses, and symmetric leg updates.
/// TDD: Verifies both legs transition symmetrically and state machine prevents invalid transitions.
/// </summary>
public partial class CallLegOrchestratorTests
{
    #region Basic Routing Tests

    [Fact]
    public void RouteProvisionalResponse_With100TryingFrom_UasReturnsModifiedResponse()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var response = new SipResponse(
            100,
            "Trying",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:caller@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:callee@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        var routedResponse = _orchestrator.RouteProvisionalResponse(callId, response);

        routedResponse.ShouldNotBeNull();
        routedResponse!.StatusCode.ShouldBe(100);
    }

    [Fact]
    public void RouteProvisionalResponse_With180RingingFrom_UasReturnsModifiedResponse()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var response = new SipResponse(
            180,
            "Ringing",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:caller@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:callee@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        var routedResponse = _orchestrator.RouteProvisionalResponse(callId, response);

        routedResponse.ShouldNotBeNull();
        routedResponse!.StatusCode.ShouldBe(180);
        routedResponse.ReasonPhrase.ShouldBe("Ringing");
    }

    [Fact]
    public void RouteFinalResponse_With200OkFrom_UasConfirmsDialog()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var response = new SipResponse(
            200,
            "OK",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:caller@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:callee@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE",
                ["Contact"] = "<sip:callee@192.0.2.2:5060>"
            });

        var routedResponse = _orchestrator.RouteFinalResponse(callId, response);

        routedResponse.ShouldNotBeNull();
        routedResponse!.StatusCode.ShouldBe(200);
    }

    [Fact]
    public void RouteFinalResponse_With200Ok_DialogBecomesConfirmed()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        _orchestrator.IsDialogConfirmed(callId).ShouldBeFalse();

        var response = new SipResponse(
            200,
            "OK",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:caller@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:callee@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE",
                ["Contact"] = "<sip:callee@192.0.2.2:5060>"
            });

        _orchestrator.RouteFinalResponse(callId, response);

        _orchestrator.IsDialogConfirmed(callId).ShouldBeTrue();
    }

    [Fact]
    public void RouteErrorResponse_With486BusyHereFrom_UasReturnsModifiedResponse()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var response = new SipResponse(
            486,
            "Busy Here",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:caller@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:callee@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        var routedResponse = _orchestrator.RouteErrorResponse(callId, response);

        routedResponse.ShouldNotBeNull();
        routedResponse!.StatusCode.ShouldBe(486);
        routedResponse.ReasonPhrase.ShouldBe("Busy Here");
    }

    [Fact]
    public void RouteErrorResponse_With486BusyHere_DialogNotConfirmed()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var response = new SipResponse(
            486,
            "Busy Here",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:caller@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:callee@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        _orchestrator.RouteErrorResponse(callId, response);

        _orchestrator.IsDialogConfirmed(callId).ShouldBeFalse();
    }

    #endregion

    #region State Machine Tests

    /// <summary>
    /// Drongo-2c5-b2-r2: CallLeg allows backward and invalid state transitions
    /// TDD: Write failing tests first, then implement fix
    /// </summary>
    [Fact]
    public void TransitionToState_FromConfirmedToInitial_ThrowsInvalidOperationException()
    {
        var callId = "call-123";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, _) = _orchestrator.CreateCallLegPair(
            callId, "tag-1", "tag-2", uacUri, uasUri, false);

        // Manually transition leg to Confirmed (simulating normal flow)
        var confirmResponse = new SipResponse(
            200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = "Alice <sip:caller@example.com>;tag=tag-1",
                ["To"] = "Bob <sip:callee@example.com>;tag=tag-2",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        uacLeg.HandleResponse(confirmResponse);
        uacLeg.State.ShouldBe(CallLegState.Confirmed);

        // Attempting backward transition should throw
        Should.Throw<InvalidOperationException>(() =>
            ((CallLeg)uacLeg).TransitionToState(CallLegState.Initial));
    }

    [Fact]
    public void HandleResponse_WithLateProvisionalAfterConfirmed_StateRemainsConfirmed()
    {
        var callId = "call-123";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, _) = _orchestrator.CreateCallLegPair(
            callId, "tag-1", "tag-2", uacUri, uasUri, false);

        // Transition to Confirmed
        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = "Alice <sip:caller@example.com>;tag=tag-1",
                ["To"] = "Bob <sip:callee@example.com>;tag=tag-2",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        uacLeg.HandleResponse(confirmResponse);
        uacLeg.State.ShouldBe(CallLegState.Confirmed);

        // Late provisional (183) should NOT downgrade state
        var lateProvisional = new SipResponse(183, "Session Progress", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = "Alice <sip:caller@example.com>;tag=tag-1",
                ["To"] = "Bob <sip:callee@example.com>;tag=tag-2",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        uacLeg.HandleResponse(lateProvisional);

        uacLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    /// <summary>
    /// Drongo-2c5-b2-r4: Asymmetric leg state updates when routing responses
    /// TDD: Verify both legs are updated symmetrically during routing
    /// </summary>
    [Fact]
    public void RouteProvisionalResponse_UpdatesBothLegs()
    {
        var callId = "call-123";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, "tag-1", "tag-2", uacUri, uasUri, false);

        var provisional = new SipResponse(183, "Session Progress", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        _orchestrator.RouteProvisionalResponse(callId, provisional);

        // Both legs should be updated to ProvisionalResponse state
        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
        uasLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
    }

    [Fact]
    public void RouteFinalResponse_UpdatesBothLegsSymmetrically()
    {
        var callId = "call-123";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, "tag-1", "tag-2", uacUri, uasUri, false);

        // Send provisional response first to move to ProvisionalResponse state
        var provisional = new SipResponse(183, "Session Progress", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        _orchestrator.RouteProvisionalResponse(callId, provisional);

        // Now send final response
        var final = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        _orchestrator.RouteFinalResponse(callId, final);

        // Both legs should be in Confirmed state using same method (HandleResponse)
        uacLeg.State.ShouldBe(CallLegState.Confirmed);
        uasLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    [Fact]
    public void RouteErrorResponse_UpdatesBothLegs()
    {
        var callId = "call-123";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, "tag-1", "tag-2", uacUri, uasUri, false);

        var errorResponse = new SipResponse(486, "Busy Here", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        _orchestrator.RouteErrorResponse(callId, errorResponse);

        // Both legs should be updated to Failed state
        uacLeg.State.ShouldBe(CallLegState.Failed);
        uasLeg.State.ShouldBe(CallLegState.Failed);
    }

    #endregion

    #region RFC3261 1xx Provisional Response Tests

    [Fact]
    public void HandleResponse_With100Trying_TransitionsToProvisionalResponse()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(100, "Trying", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
    }

    [Fact]
    public void HandleResponse_With180Ringing_TransitionsToProvisionalResponse()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(180, "Ringing", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
    }

    [Fact]
    public void HandleResponse_With181CallIsBeingForwarded_TransitionsToProvisionalResponse()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(181, "Call Is Being Forwarded", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
    }

    [Fact]
    public void HandleResponse_With183SessionProgress_TransitionsToProvisionalResponse()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(183, "Session Progress", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
    }

    [Fact]
    public void HandleResponse_Multiple1xxResponses_StateRemainsProvisionalResponse()
    {
        var (uacLeg, _) = CreateLegPair();
        var response180 = new SipResponse(180, "Ringing", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        var response181 = new SipResponse(181, "Call Is Being Forwarded", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response180);
        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);

        // Second 1xx should not change state
        uacLeg.HandleResponse(response181);
        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
    }

    #endregion

    #region RFC3261 2xx Final Success Response Tests

    [Fact]
    public void HandleResponse_With200OK_TransitionsToConfirmed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    [Fact]
    public void HandleResponse_With2xxFromProvisionalState_TransitionsToConfirmed()
    {
        var (uacLeg, _) = CreateLegPair();
        var provisional = new SipResponse(183, "Session Progress", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        var final = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(provisional);
        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);

        uacLeg.HandleResponse(final);
        uacLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    #endregion

    #region RFC3261 3xx Redirection Response Tests

    [Fact]
    public void HandleResponse_With300MultipleChoices_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(300, "Multiple Choices", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    [Fact]
    public void HandleResponse_With301MovedPermanently_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(301, "Moved Permanently", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    [Fact]
    public void HandleResponse_With380AlternativeService_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(380, "Alternative Service", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    #endregion

    #region RFC3261 4xx Client Error Response Tests

    [Fact]
    public void HandleResponse_With400BadRequest_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(400, "Bad Request", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    [Fact]
    public void HandleResponse_With403Forbidden_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(403, "Forbidden", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    [Fact]
    public void HandleResponse_With404NotFound_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(404, "Not Found", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    [Fact]
    public void HandleResponse_With486BusyHere_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(486, "Busy Here", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    #endregion

    #region RFC3261 5xx Server Error Response Tests

    [Fact]
    public void HandleResponse_With500ServerInternalError_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(500, "Server Internal Error", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    [Fact]
    public void HandleResponse_With503ServiceUnavailable_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(503, "Service Unavailable", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    #endregion

    #region RFC3261 6xx Global Failure Response Tests

    [Fact]
    public void HandleResponse_With600BusyEverywhere_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(600, "Busy Everywhere", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    [Fact]
    public void HandleResponse_With603Decline_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();
        var response = new SipResponse(603, "Decline", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });

        uacLeg.HandleResponse(response);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    #endregion

    #region Late Response Handling Tests

    [Fact]
    public void HandleResponse_LateProvisionalAfter2xxConfirmation_StateRemainsConfirmed()
    {
        var (uacLeg, _) = CreateLegPair();

        // Confirm dialog with 2xx
        var confirm = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(confirm);
        uacLeg.State.ShouldBe(CallLegState.Confirmed);

        // Late 1xx should not downgrade state
        var late1xx = new SipResponse(183, "Session Progress", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(late1xx);

        uacLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    [Fact]
    public void HandleResponse_Late3xxAfter2xxConfirmation_StateRemainsConfirmed()
    {
        var (uacLeg, _) = CreateLegPair();

        // Confirm dialog
        var confirm = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(confirm);
        uacLeg.State.ShouldBe(CallLegState.Confirmed);

        // Late 3xx after confirmation should not change state
        var late3xx = new SipResponse(301, "Moved Permanently", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(late3xx);

        uacLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    [Fact]
    public void HandleResponse_Late4xxAfter2xxConfirmation_StateRemainsConfirmed()
    {
        var (uacLeg, _) = CreateLegPair();

        // Confirm dialog
        var confirm = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(confirm);
        uacLeg.State.ShouldBe(CallLegState.Confirmed);

        // Late 4xx after confirmation should not change state
        var late4xx = new SipResponse(486, "Busy Here", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(late4xx);

        uacLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    [Fact]
    public void HandleResponse_Late5xxAfter2xxConfirmation_StateRemainsConfirmed()
    {
        var (uacLeg, _) = CreateLegPair();

        // Confirm dialog
        var confirm = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(confirm);
        uacLeg.State.ShouldBe(CallLegState.Confirmed);

        // Late 5xx should not change state
        var late5xx = new SipResponse(500, "Server Internal Error", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(late5xx);

        uacLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    [Fact]
    public void HandleResponse_Multiple2xxResponses_StateRemainsConfirmedAfterFirst()
    {
        var (uacLeg, _) = CreateLegPair();

        // First 2xx confirms dialog
        var confirm1 = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(confirm1);
        uacLeg.State.ShouldBe(CallLegState.Confirmed);

        // Second 2xx (duplicate/retransmission) should not change state
        var confirm2 = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(confirm2);

        uacLeg.State.ShouldBe(CallLegState.Confirmed);
    }

    [Fact]
    public void HandleResponse_3xxDuringProvisionalState_TransitionsToFailed()
    {
        var (uacLeg, _) = CreateLegPair();

        // Move to provisional
        var provisional = new SipResponse(180, "Ringing", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(provisional);
        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);

        // 3xx response while in provisional should transition to Failed
        var error3xx = new SipResponse(301, "Moved Permanently", "SIP/2.0",
            new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" });
        uacLeg.HandleResponse(error3xx);

        uacLeg.State.ShouldBe(CallLegState.Failed);
    }

    #endregion
}
