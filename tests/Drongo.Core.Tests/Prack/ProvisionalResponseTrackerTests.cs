using Drongo.Core.Prack;
using Xunit;

namespace Drongo.Core.Tests.Prack;

/// <summary>
/// Tests for provisional response tracking per RFC 3262.
/// </summary>
public class ProvisionalResponseTrackerTests
{
    [Fact]
    public void TrackProvisionalResponse_AfterInitialProvisionalResponse_ReturnsRSeq()
    {
        // RED: Test tracks first provisional response and returns initial RSeq
        var tracker = new ProvisionalResponseTracker();
        
        var rseq = tracker.TrackProvisionalResponse("dialog-123");
        
        Assert.Equal(1, rseq);
    }

    [Fact]
    public void TrackProvisionalResponse_MultipleResponses_IncrementsRSeq()
    {
        // RED: Test that multiple provisional responses increment RSeq
        var tracker = new ProvisionalResponseTracker();
        
        tracker.TrackProvisionalResponse("dialog-123");
        var rseq2 = tracker.TrackProvisionalResponse("dialog-123");
        
        Assert.Equal(2, rseq2);
    }

    [Fact]
    public void TrackProvisionalResponse_DifferentDialogs_TracksIndependently()
    {
        // RED: Test that different dialogs track RSeq independently
        var tracker = new ProvisionalResponseTracker();
        
        var rseq1 = tracker.TrackProvisionalResponse("dialog-A");
        var rseq2 = tracker.TrackProvisionalResponse("dialog-B");
        
        Assert.Equal(1, rseq1);
        Assert.Equal(1, rseq2);
    }

    [Fact]
    public void AcknowledgeProvisionalResponse_WithValidRSeq_MarksAsAcknowledged()
    {
        // RED: Test acknowledging a provisional response by RSeq
        var tracker = new ProvisionalResponseTracker();
        
        var rseq = tracker.TrackProvisionalResponse("dialog-123");
        var acknowledged = tracker.AcknowledgeProvisionalResponse("dialog-123", rseq);
        
        Assert.True(acknowledged);
    }

    [Fact]
    public void AcknowledgeProvisionalResponse_WithInvalidRSeq_ReturnsFalse()
    {
        // RED: Test acknowledging with wrong RSeq returns false
        var tracker = new ProvisionalResponseTracker();
        
        tracker.TrackProvisionalResponse("dialog-123");
        var acknowledged = tracker.AcknowledgeProvisionalResponse("dialog-123", 999);
        
        Assert.False(acknowledged);
    }

    [Fact]
    public void AcknowledgeProvisionalResponse_AlreadyAcknowledged_ReturnsFalse()
    {
        // RED: Test that double acknowledgment returns false
        var tracker = new ProvisionalResponseTracker();
        
        var rseq = tracker.TrackProvisionalResponse("dialog-123");
        tracker.AcknowledgeProvisionalResponse("dialog-123", rseq);
        var secondAck = tracker.AcknowledgeProvisionalResponse("dialog-123", rseq);
        
        Assert.False(secondAck);
    }

    [Fact]
    public void IsProvisionalResponseRequired_For100Trying_ReturnsFalse()
    {
        // RED: Test that 100 Trying does NOT require PRACK per RFC 3262
        var tracker = new ProvisionalResponseTracker();
        
        var requiresPrack = tracker.IsProvisionalResponseReliable(100);
        
        Assert.False(requiresPrack);
    }

    [Fact]
    public void IsProvisionalResponseRequired_For180Ringing_ReturnsTrue()
    {
        // RED: Test that 180 Ringing requires PRACK
        var tracker = new ProvisionalResponseTracker();
        
        var requiresPrack = tracker.IsProvisionalResponseReliable(180);
        
        Assert.True(requiresPrack);
    }

    [Fact]
    public void IsProvisionalResponseRequired_For181CallBeingForwarded_ReturnsTrue()
    {
        // RED: Test that 181 requires PRACK
        var tracker = new ProvisionalResponseTracker();
        
        var requiresPrack = tracker.IsProvisionalResponseReliable(181);
        
        Assert.True(requiresPrack);
    }

    [Fact]
    public void GetUnacknowledgedResponses_WithNoPrack_ReturnsEmpty()
    {
        // RED: Test getting unacknowledged responses returns empty list
        var tracker = new ProvisionalResponseTracker();
        
        var unacked = tracker.GetUnacknowledgedResponses("dialog-123");
        
        Assert.Empty(unacked);
    }

    [Fact]
    public void GetUnacknowledgedResponses_AfterMultipleProvisionals_ReturnsUnacked()
    {
        // RED: Test getting list of unacknowledged provisional responses
        var tracker = new ProvisionalResponseTracker();
        
        tracker.TrackProvisionalResponse("dialog-123");
        tracker.TrackProvisionalResponse("dialog-123");
        var unacked = tracker.GetUnacknowledgedResponses("dialog-123");
        
        Assert.Equal(2, unacked.Count);
    }

    [Fact]
    public void GetUnacknowledgedResponses_AfterAcknowledgingOne_ReturnsOneRemaining()
    {
        // RED: Test that acknowledging one response leaves others unacked
        var tracker = new ProvisionalResponseTracker();
        
        var rseq1 = tracker.TrackProvisionalResponse("dialog-123");
        tracker.TrackProvisionalResponse("dialog-123");
        tracker.AcknowledgeProvisionalResponse("dialog-123", rseq1);
        
        var unacked = tracker.GetUnacknowledgedResponses("dialog-123");
        
        Assert.Single(unacked);
    }

    [Fact]
    public void ClearDialog_DialogExists_RemovesTracking()
    {
        // RED: Test clearing dialog tracking
        var tracker = new ProvisionalResponseTracker();
        
        tracker.TrackProvisionalResponse("dialog-123");
        tracker.ClearDialog("dialog-123");
        var unacked = tracker.GetUnacknowledgedResponses("dialog-123");
        
        Assert.Empty(unacked);
    }
}
