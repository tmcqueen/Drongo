using Drongo.Core.Dialogs;
using Drongo.Core.Messages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Dialogs;

/// <summary>
/// Tests for Block 3: In-Dialog Request Handling
/// RFC3261 Sections 12.1-14: ACK, BYE, re-INVITE, and 481 responses
///
/// In a B2BUA architecture (Back-to-Back User Agent):
/// - ACK acknowledges the 2xx response received by the UAC
/// - Does NOT get forwarded to the UAS (UAS sends its own ACK)
/// - Dialog remains in Confirmed state
/// </summary>
public partial class CallLegOrchestratorTests_InDialog
{
    private readonly ILogger<CallLeg> _logger;

    public CallLegOrchestratorTests_InDialog()
    {
        _logger = Substitute.For<ILogger<CallLeg>>();
    }

    /// <summary>
    /// Drongo-2c5-b3-r1: Handle ACK requests (RFC3261 Section 12.1)
    ///
    /// Per RFC3261 Section 12.1, ACK must:
    /// 1. Match dialog by From tag, To tag, and Call-ID
    /// 2. Be handled in Confirmed state (after receiving 2xx response to INVITE)
    /// 3. Remain in Confirmed state after ACK receipt (does not get routed to other leg)
    /// </summary>
    [Fact]
    public void RouteInDialogRequest_AckForConfirmedDialog_ReturnsNull()
    {
        // Arrange
        var orchestrator = new CallLegOrchestrator(_logger);
        var callId = "call-123";
        var uacTag = "uac-tag-456";
        var uasTag = "uas-tag-789";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        // Create dialog leg pair
        orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        // Move to Confirmed state by routing 2xx response
        var finalResponse = new SipResponse(
            200,
            "OK",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:caller@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:callee@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        orchestrator.RouteFinalResponse(callId, finalResponse);

        // Act - Create ACK for confirmed dialog
        var ackRequest = new SipRequest(
            SipMethod.Ack,
            uacUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = $"Alice <sip:caller@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:callee@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 ACK"
            });

        // Assert - ACK for in-dialog request should return null (not forwarded to UAS per RFC3261)
        // Per RFC3261 Section 12.1: ACK acknowledges 2xx response but is NOT forwarded to other leg
        var result = orchestrator.RouteInDialogRequest(callId, ackRequest);
        result.ShouldBeNull();  // ACK does not get forwarded to other leg
    }

    [Fact]
    public void GetCallLegs_WithValidCallId_ReturnsLegPair()
    {
        // Arrange
        var orchestrator = new CallLegOrchestrator(_logger);
        var callId = "call-456";
        var uacTag = "uac-789";
        var uasTag = "uas-012";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        // Act - Retrieve the legs
        var legsFound = orchestrator.TryGetCallLegs(callId, out var uacLeg, out var uasLeg);

        // Assert
        legsFound.ShouldBeTrue();
        uacLeg.ShouldNotBeNull();
        uasLeg.ShouldNotBeNull();
        uacLeg!.LocalTag.ShouldBe(uacTag);
        uasLeg!.LocalTag.ShouldBe(uasTag);
    }

    [Fact]
    public void IsDialogConfirmed_AfterFinalResponse_ReturnsTrue()
    {
        // Arrange
        var orchestrator = new CallLegOrchestrator(_logger);
        var callId = "call-789";
        var uacTag = "uac-111";
        var uasTag = "uas-222";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        // Route a 2xx response to confirm the dialog
        var confirmResponse = new SipResponse(
            200,
            "OK",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        orchestrator.RouteFinalResponse(callId, confirmResponse);

        // Act
        var confirmed = orchestrator.IsDialogConfirmed(callId);

        // Assert
        confirmed.ShouldBeTrue();
    }

    [Fact]
    public void RouteInDialogRequest_AckForNonExistentDialog_ReturnsNull()
    {
        // Arrange
        var orchestrator = new CallLegOrchestrator(_logger);
        var nonExistentCallId = "nonexistent-call-999";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);

        var ackRequest = new SipRequest(
            SipMethod.Ack,
            uacUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = "Alice <sip:alice@example.com>;tag=uac-123",
                ["To"] = "Bob <sip:bob@example.com>;tag=uas-456",
                ["Call-ID"] = nonExistentCallId,
                ["CSeq"] = "1 ACK"
            });

        // Act - Route ACK for non-existent dialog
        var result = orchestrator.RouteInDialogRequest(nonExistentCallId, ackRequest);

        // Assert - ACK for non-existent dialog returns null (silently handled per RFC3261)
        result.ShouldBeNull();
    }

    [Fact]
    public void RouteInDialogRequest_WithValidArguments_DoesNotThrow()
    {
        // Arrange
        var orchestrator = new CallLegOrchestrator(_logger);
        var callId = "call-valid-123";
        var uacTag = "uac-abc";
        var uasTag = "uas-def";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        var ackRequest = new SipRequest(
            SipMethod.Ack,
            uacUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 ACK"
            });

        // Act & Assert - Should not throw
        Should.NotThrow(() =>
        {
            orchestrator.RouteInDialogRequest(callId, ackRequest);
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Comprehensive Tests for BYE Requests (RFC3261 Section 12.2)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RouteInDialogRequest_ByeForConfirmedDialog_ForwardsToOtherLeg()
    {
        // Drongo-2c5-b3-r2: Handle BYE requests
        // Per RFC3261 Section 12.2: BYE requests should be forwarded to the other leg
        // and result in dialog termination

        // Arrange
        var orchestrator = new CallLegOrchestrator(_logger);
        var callId = "call-bye-001";
        var uacTag = "uac-bye-001";
        var uasTag = "uas-bye-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        // Establish confirmed dialog
        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        orchestrator.RouteFinalResponse(callId, confirmResponse);

        // Act - Route BYE request
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

        // BYE should be forwarded (not null like ACK)
        // TODO: When r2 is implemented, this should return a non-null request
        var result = orchestrator.RouteInDialogRequest(callId, byeRequest);

        // For now, implementation returns null (placeholder)
        // After r2 implementation, this should return the forwarded BYE request
        result.ShouldNotBeNull();
        result!.Method.ShouldBe(SipMethod.Bye);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Comprehensive Tests for re-INVITE (RFC3261 Section 14)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RouteInDialogRequest_ReInviteForConfirmedDialog_ForwardsToOtherLeg()
    {
        // Drongo-2c5-b3-r3: Handle re-INVITE requests
        // Per RFC3261 Section 14: re-INVITE allows mid-dialog offer/answer exchange

        // Arrange
        var orchestrator = new CallLegOrchestrator(_logger);
        var callId = "call-reinvite-001";
        var uacTag = "uac-reinv-001";
        var uasTag = "uas-reinv-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);

        // Establish confirmed dialog
        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        orchestrator.RouteFinalResponse(callId, confirmResponse);

        // Act - Route re-INVITE request (INVITE with CSeq > 1)
        var reinviteRequest = new SipRequest(
            SipMethod.Invite,
            uasUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 INVITE"  // Higher sequence number = re-INVITE
            });

        // re-INVITE should be forwarded (not null)
        // TODO: When r3 is implemented, this should return a non-null request
        var result = orchestrator.RouteInDialogRequest(callId, reinviteRequest);

        // For now, implementation returns null (placeholder)
        // After r3 implementation, this should return the forwarded re-INVITE
        result.ShouldNotBeNull();
        result!.Method.ShouldBe(SipMethod.Invite);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Comprehensive Tests for 481 Responses (RFC3261 Section 12.2.2)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RouteInDialogRequest_AnyRequestForNonExistentDialog_ReturnsNull()
    {
        // Drongo-2c5-b3-r4: Implement 481 responses
        // Per RFC3261 Section 12.2.2: Requests that don't match a dialog
        // should receive 481 "Call Leg Does Not Exist" response

        // Arrange
        var orchestrator = new CallLegOrchestrator(_logger);
        var nonExistentCallId = "nonexistent-481-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);

        // Act - Try to route any request for non-existent dialog
        var byeRequest = new SipRequest(
            SipMethod.Bye,
            uacUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = "Alice <sip:alice@example.com>;tag=uac-123",
                ["To"] = "Bob <sip:bob@example.com>;tag=uas-456",
                ["Call-ID"] = nonExistentCallId,
                ["CSeq"] = "1 BYE"
            });

        var result = orchestrator.RouteInDialogRequest(nonExistentCallId, byeRequest);

        // Assert - Request returns null (should trigger 481 response at higher level)
        result.ShouldBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Edge Case Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void RouteInDialogRequest_WithNullCallId_ThrowsArgumentException()
    {
        // Edge case: null Call-ID should throw
        var orchestrator = new CallLegOrchestrator(_logger);
        var uacUri = new SipUri("sip", "alice@example.com", 5060);

        var ackRequest = new SipRequest(SipMethod.Ack, uacUri, "SIP/2.0",
            new Dictionary<string, string>());

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
        {
            orchestrator.RouteInDialogRequest(null!, ackRequest);
        });
    }

    [Fact]
    public void RouteInDialogRequest_WithNullRequest_ThrowsArgumentNullException()
    {
        // Edge case: null request should throw
        var orchestrator = new CallLegOrchestrator(_logger);
        var callId = "call-edge-001";

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
        {
            orchestrator.RouteInDialogRequest(callId, null!);
        });
    }

    [Fact]
    public void RouteInDialogRequest_MultipleConsecutiveRequests_HandledCorrectly()
    {
        // Test handling multiple in-dialog requests in sequence
        // Simulates: ACK -> re-INVITE -> BYE

        var orchestrator = new CallLegOrchestrator(_logger);
        var callId = "call-sequence-001";
        var uacTag = "uac-seq";
        var uasTag = "uas-seq";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        // Setup confirmed dialog
        orchestrator.CreateCallLegPair(callId, uacTag, uasTag, uacUri, uasUri, false);
        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });
        orchestrator.RouteFinalResponse(callId, confirmResponse);

        // Route ACK
        var ackRequest = new SipRequest(SipMethod.Ack, uacUri, "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 ACK"
            });
        var ackResult = orchestrator.RouteInDialogRequest(callId, ackRequest);
        ackResult.ShouldBeNull(); // ACK is not forwarded

        // Route re-INVITE
        var reinviteRequest = new SipRequest(SipMethod.Invite, uasUri, "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 INVITE"
            });
        var reinviteResult = orchestrator.RouteInDialogRequest(callId, reinviteRequest);
        // TODO: Should forward re-INVITE after r3 implementation

        // Route BYE
        var byeRequest = new SipRequest(SipMethod.Bye, uasUri, "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });
        var byeResult = orchestrator.RouteInDialogRequest(callId, byeRequest);
        // TODO: Should forward BYE after r2 implementation
    }
}
