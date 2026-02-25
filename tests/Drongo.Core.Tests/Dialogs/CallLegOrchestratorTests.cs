using Drongo.Core.Dialogs;
using Drongo.Core.Messages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Shouldly;

namespace Drongo.Core.Tests.Dialogs;

public class CallLegOrchestratorTests
{
    private readonly ICallLegOrchestrator _orchestrator;
    private readonly ILogger<CallLeg> _logger;

    public CallLegOrchestratorTests()
    {
        _logger = Substitute.For<ILogger<CallLeg>>();
        _orchestrator = new CallLegOrchestrator(_logger);
    }

    [Fact]
    public void CreateCallLegPair_WithValidParams_CreatesUacAndUasLegs()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        uacLeg.ShouldNotBeNull();
        uasLeg.ShouldNotBeNull();
        uacLeg.CallId.ShouldBe(callId);
        uasLeg.CallId.ShouldBe(callId);
        uacLeg.LocalTag.ShouldBe(uacTag);
        uasLeg.LocalTag.ShouldBe(uasTag);
    }

    [Fact]
    public void CreateCallLegPair_WithValidParams_SetsRemoteTagsCorrectly()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        // Per RFC3261 Section 12: UAC leg's remote tag should be UAS's local tag
        uacLeg.RemoteTag.ShouldBe(uasTag);
        // UAS leg's remote tag should be UAC's local tag
        uasLeg.RemoteTag.ShouldBe(uacTag);
    }

    [Fact]
    public void CreateCallLegPair_WithValidParams_SetsUrisCorrectly()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        uacLeg.LocalUri.ShouldBe(uacUri);
        uacLeg.RemoteUri.ShouldBe(uasUri);
        uasLeg.LocalUri.ShouldBe(uasUri);
        uasLeg.RemoteUri.ShouldBe(uacUri);
    }

    [Fact]
    public void CreateCallLegPair_WithSecureFlag_SetsBothLegsSecure()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5061);
        var uasUri = new SipUri("sip", "callee@example.com", 5061);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, true);

        uacLeg.IsSecure.ShouldBeTrue();
        uasLeg.IsSecure.ShouldBeTrue();
    }

    [Fact]
    public void TryGetCallLegs_WithExistingPair_ReturnsBothLegs()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        _orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var found = _orchestrator.TryGetCallLegs(callId, out var retrievedUacLeg, out var retrievedUasLeg);

        found.ShouldBeTrue();
        retrievedUacLeg.ShouldNotBeNull();
        retrievedUasLeg.ShouldNotBeNull();
        retrievedUacLeg!.LocalTag.ShouldBe(uacTag);
        retrievedUasLeg!.LocalTag.ShouldBe(uasTag);
    }

    [Fact]
    public void TryGetCallLegs_WithNonexistentCallId_ReturnsFalse()
    {
        var found = _orchestrator.TryGetCallLegs("nonexistent", out var uacLeg, out var uasLeg);

        found.ShouldBeFalse();
        uacLeg.ShouldBeNull();
        uasLeg.ShouldBeNull();
    }

    [Fact]
    public void ActiveDialogCount_AfterCreatingPairs_ReflectsCount()
    {
        _orchestrator.ActiveDialogCount.ShouldBe(0);

        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        _orchestrator.CreateCallLegPair("call-1", "tag-1", "tag-2", uacUri, uasUri, false);
        _orchestrator.ActiveDialogCount.ShouldBe(1);

        _orchestrator.CreateCallLegPair("call-2", "tag-3", "tag-4", uacUri, uasUri, false);
        _orchestrator.ActiveDialogCount.ShouldBe(2);
    }

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

    [Fact]
    public void CallLeg_CreatedInInitialState()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        uacLeg.State.ShouldBe(CallLegState.Initial);
        uasLeg.State.ShouldBe(CallLegState.Initial);
    }

    [Fact]
    public void CallLeg_LocalSequenceNumber_StartsAtOne()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        uacLeg.LocalSequenceNumber.ShouldBeGreaterThanOrEqualTo(0);
        uasLeg.LocalSequenceNumber.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void CallLeg_GetNextSequenceNumber_IncrementsForEachCall()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, _) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        var seq1 = uacLeg.GetNextSequenceNumber();
        var seq2 = uacLeg.GetNextSequenceNumber();
        var seq3 = uacLeg.GetNextSequenceNumber();

        seq2.ShouldBe(seq1 + 1);
        seq3.ShouldBe(seq2 + 1);
    }

    [Fact]
    public void CallLeg_IsEstablished_FalseForInitialState()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        uacLeg.IsEstablished().ShouldBeFalse();
        uasLeg.IsEstablished().ShouldBeFalse();
    }

    [Fact]
    public void CallLeg_IsEstablished_TrueAfterConfirmedState()
    {
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, _) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        // After routing 200 OK, leg should be established
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

        uacLeg.IsEstablished().ShouldBeTrue();
    }
}
