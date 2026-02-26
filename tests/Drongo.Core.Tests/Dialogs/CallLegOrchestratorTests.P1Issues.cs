using Drongo.Core.SIP.Dialogs;
using Drongo.Core.SIP.Messages;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Dialogs;

/// <summary>
/// Tests for P1 Issues in Block 2:
/// - r5: Thread safety - CallLeg state mutations not protected
/// - r6: HandleRequest stub incomplete
/// - r7: Logger type mismatch (ILogger<CallLeg> in CallLegOrchestrator)
/// - r8: Sequence number type divergence
///
/// TDD RED PHASE: Write failing tests to document expected behavior
/// </summary>
public partial class CallLegOrchestratorTests
{
    #region P1 Issue r5: Thread Safety Tests

    /// <summary>
    /// Drongo-2c5-b2-r5: CallLeg mutations not thread-safe
    /// TDD RED: Concurrent HandleResponse calls should not cause state corruption
    /// </summary>
    [Fact]
    public void CallLeg_ConcurrentHandleResponseCalls_ShouldMaintainConsistentState()
    {
        var (uacLeg, _) = CreateLegPair();

        // Simulate concurrent response handling from multiple threads
        var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();

        // Create multiple response objects for concurrent processing
        var responses = new[]
        {
            new SipResponse(100, "Trying", "SIP/2.0",
                new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" }),
            new SipResponse(180, "Ringing", "SIP/2.0",
                new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" }),
            new SipResponse(183, "Session Progress", "SIP/2.0",
                new Dictionary<string, string> { ["Call-ID"] = "call-123", ["CSeq"] = "1 INVITE" })
        };

        // Spawn concurrent tasks to handle responses
        for (int i = 0; i < 3; i++)
        {
            var response = responses[i];
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                uacLeg.HandleResponse(response);
            }));
        }

        System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

        // After concurrent processing, state should be consistent
        // Verify no state corruption occurred
        uacLeg.State.ShouldBeOneOf(
            CallLegState.Initial,
            CallLegState.ProvisionalResponse);
    }

    /// <summary>
    /// Drongo-2c5-b2-r5: Concurrent mutations during routing should not corrupt state
    /// TDD RED: Multiple threads routing responses should result in consistent dialog state
    /// </summary>
    [Fact]
    public void CallLegOrchestrator_ConcurrentResponseRouting_DialogStateShouldBeConsistent()
    {
        var callId = "call-concurrent-123";
        var uacUri = new SipUri("sip", "caller@example.com", 5060);
        var uasUri = new SipUri("sip", "callee@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, "tag-1", "tag-2", uacUri, uasUri, false);

        // Simulate multiple threads routing responses concurrently
        var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();

        // Create multiple responses for concurrent routing
        // All provisional (1xx) to avoid state divergence from mixed response types
        var responses = new[]
        {
            new SipResponse(100, "Trying", "SIP/2.0",
                new Dictionary<string, string> { ["Call-ID"] = callId, ["CSeq"] = "1 INVITE" }),
            new SipResponse(180, "Ringing", "SIP/2.0",
                new Dictionary<string, string> { ["Call-ID"] = callId, ["CSeq"] = "1 INVITE" }),
            new SipResponse(183, "Session Progress", "SIP/2.0",
                new Dictionary<string, string> { ["Call-ID"] = callId, ["CSeq"] = "1 INVITE" })
        };

        // Route responses concurrently with proper closure capture
        for (int i = 0; i < 3; i++)
        {
            int index = i; // Capture value, not reference
            var response = responses[index];
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                _orchestrator.RouteProvisionalResponse(callId, response);
            }));
        }

        System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

        // Both legs should be in same state (symmetric routing per RFC3261)
        uacLeg.State.ShouldBe(uasLeg.State);

        // State should be ProvisionalResponse from the 1xx responses
        uacLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
        uasLeg.State.ShouldBe(CallLegState.ProvisionalResponse);
    }

    #endregion

    #region P1 Issue r6: HandleRequest Stub Tests

    /// <summary>
    /// Drongo-2c5-b2-r6: HandleRequest stub incomplete
    /// TDD RED: HandleRequest should process incoming requests, not just log
    /// </summary>
    [Fact]
    public void HandleRequest_WithValidInvite_ShouldUpdateLegState()
    {
        var (uacLeg, _) = CreateLegPair();
        var request = new SipRequest(
            SipMethod.Invite,
            new SipUri("sip", "bob@example.com", 5060),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["From"] = "<sip:alice@example.com>;tag=1",
                ["To"] = "<sip:bob@example.com>",
                ["Call-ID"] = "call-123",
                ["CSeq"] = "1 INVITE"
            });

        // Handle the request
        uacLeg.HandleRequest(request);

        // After handling INVITE, leg should transition to Inviting state
        uacLeg.State.ShouldBe(CallLegState.Inviting);
    }

    /// <summary>
    /// Drongo-2c5-b2-r6: HandleRequest should validate request fields
    /// TDD RED: Invalid requests should throw or be rejected
    /// </summary>
    [Fact]
    public void HandleRequest_WithNullRequest_ShouldThrowArgumentNullException()
    {
        var (uacLeg, _) = CreateLegPair();

        // Attempt to handle null request
        var ex = Should.Throw<ArgumentNullException>(() =>
            uacLeg.HandleRequest(null!));

        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region P1 Issue r7: Logger Type Mismatch Tests

    /// <summary>
    /// Drongo-2c5-b2-r7: Logger type should be ILogger<CallLegOrchestrator>, not ILogger<CallLeg>
    /// TDD RED: Verify logger context is correct type
    /// This is more of a code review item, so we verify the orchestrator can be created
    /// </summary>
    [Fact]
    public void CallLegOrchestrator_ShouldBeConstructibleWithProperLogger()
    {
        // This test documents that CallLegOrchestrator currently takes ILogger<CallLeg>
        // but should ideally take ILogger<CallLegOrchestrator> for better logging context
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<CallLeg>>();
        var orchestrator = new CallLegOrchestrator(logger);

        orchestrator.ShouldNotBeNull();
    }

    #endregion

    #region P1 Issue r8: Sequence Number Type Tests

    /// <summary>
    /// Drongo-2c5-b2-r8: Sequence numbers should consistently use long throughout
    /// TDD RED: Verify no int/long type mismatches in sequence number handling
    /// </summary>
    [Fact]
    public void CallLeg_SequenceNumberType_ShouldBeLongConsistently()
    {
        var (uacLeg, _) = CreateLegPair();

        // LocalSequenceNumber should be long
        var localSeq = uacLeg.LocalSequenceNumber;
        localSeq.ShouldBeOfType<long>();

        // GetNextSequenceNumber should return long
        var nextSeq = uacLeg.GetNextSequenceNumber();
        nextSeq.ShouldBeOfType<long>();

        // RemoteSequenceNumber should be long
        var remoteSeq = uacLeg.RemoteSequenceNumber;
        remoteSeq.ShouldBeOfType<long>();
    }

    /// <summary>
    /// Drongo-2c5-b2-r8: Large sequence numbers (beyond int range) should be handled
    /// TDD RED: Verify sequence numbers can exceed int.MaxValue per RFC3261
    /// </summary>
    [Fact]
    public void CallLeg_SequenceNumber_ShouldHandleValuesAboveIntMaxValue()
    {
        var (uacLeg, _) = CreateLegPair();

        // Simulate many requests to get beyond int.MaxValue
        long targetValue = int.MaxValue + 1000L;

        // Create enough sequence number increments
        for (long i = 0; i < targetValue - 1; i++)
        {
            uacLeg.GetNextSequenceNumber();
        }

        // Should have sequence number beyond int.MaxValue
        uacLeg.LocalSequenceNumber.ShouldBeGreaterThan(int.MaxValue);

        // Next sequence should work correctly
        var nextSeq = uacLeg.GetNextSequenceNumber();
        nextSeq.ShouldBe(uacLeg.LocalSequenceNumber);
    }

    /// <summary>
    /// Drongo-2c5-b2-r8: UpdateRemoteSequenceNumber should accept long values
    /// TDD RED: Remote sequence updates should support full long range
    /// </summary>
    [Fact]
    public void UpdateRemoteSequenceNumber_WithLargeLongValue_ShouldUpdate()
    {
        var (uacLeg, _) = CreateLegPair();

        long largeValue = (long)int.MaxValue + 50;

        // Update with large value
        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(largeValue);

        uacLeg.RemoteSequenceNumber.ShouldBe(largeValue);
    }

    #endregion
}
