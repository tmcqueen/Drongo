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
}
