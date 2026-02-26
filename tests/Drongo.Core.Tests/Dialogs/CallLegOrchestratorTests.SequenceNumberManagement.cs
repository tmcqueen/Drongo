using Drongo.Core.SIP.Dialogs;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Dialogs;

/// <summary>
/// Tests for CallLeg sequence number (CSeq) management per RFC3261 Section 12.
/// Verifies local sequence (sent by this leg) and remote sequence (received from peer)
/// track correctly and follow RFC3261 rules (local starts at 1, remote at 0, only increases).
/// </summary>
public partial class CallLegOrchestratorTests
{
    #region Sequence Number Management Tests

    [Fact]
    public void CallLeg_LocalSequenceNumber_InitialValue_IsOne()
    {
        var (uacLeg, _) = CreateLegPair();
        uacLeg.LocalSequenceNumber.ShouldBe(1L);
    }

    [Fact]
    public void CallLeg_RemoteSequenceNumber_InitialValue_IsZero()
    {
        var (uacLeg, _) = CreateLegPair();
        uacLeg.RemoteSequenceNumber.ShouldBe(0L);
    }

    [Fact]
    public void GetNextSequenceNumber_FirstCall_ReturnsTwo()
    {
        var (uacLeg, _) = CreateLegPair();
        var nextSeq = uacLeg.GetNextSequenceNumber();
        nextSeq.ShouldBe(2L);
    }

    [Fact]
    public void GetNextSequenceNumber_MultipleCallsIncrement()
    {
        var (uacLeg, _) = CreateLegPair();
        var seq1 = uacLeg.GetNextSequenceNumber();  // Returns 2, increments to 2
        var seq2 = uacLeg.GetNextSequenceNumber();  // Returns 3, increments to 3
        var seq3 = uacLeg.GetNextSequenceNumber();  // Returns 4, increments to 4
        seq1.ShouldBe(2L);
        seq2.ShouldBe(3L);
        seq3.ShouldBe(4L);
        uacLeg.LocalSequenceNumber.ShouldBe(4L);
    }

    [Fact]
    public void UpdateRemoteSequenceNumber_WithHigherValue_Updates()
    {
        var (uacLeg, _) = CreateLegPair();
        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(5);
        uacLeg.RemoteSequenceNumber.ShouldBe(5L);
    }

    [Fact]
    public void UpdateRemoteSequenceNumber_WithLowerValue_DoesNotUpdate()
    {
        var (uacLeg, _) = CreateLegPair();
        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(10);
        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(5);
        uacLeg.RemoteSequenceNumber.ShouldBe(10L);
    }

    [Fact]
    public void UpdateRemoteSequenceNumber_WithEqualValue_DoesNotUpdate()
    {
        var (uacLeg, _) = CreateLegPair();
        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(7);
        uacLeg.RemoteSequenceNumber.ShouldBe(7L);

        // Try to update with same value (retransmission detection)
        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(7);

        // Should remain 7 (no update on equal)
        uacLeg.RemoteSequenceNumber.ShouldBe(7L);
    }

    [Fact]
    public void GetNextSequenceNumber_WithLargeInitialValue_IncrementsCorrectly()
    {
        var (uacLeg, _) = CreateLegPair();

        // Simulate sending many requests by incrementing many times
        for (long i = 0; i < 1000; i++)
        {
            uacLeg.GetNextSequenceNumber();
        }

        uacLeg.LocalSequenceNumber.ShouldBe(1001L);

        // Next call should return 1002
        var nextSeq = uacLeg.GetNextSequenceNumber();
        nextSeq.ShouldBe(1002L);
    }

    [Fact]
    public void UpdateRemoteSequenceNumber_WithZero_DoesNotUpdate()
    {
        var (uacLeg, _) = CreateLegPair();

        // Initial is 0, trying to update with 0 should not change (equal condition)
        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(0);

        uacLeg.RemoteSequenceNumber.ShouldBe(0L);
    }

    [Fact]
    public void UpdateRemoteSequenceNumber_MultipleIncreasingValues()
    {
        var (uacLeg, _) = CreateLegPair();

        // Simulate receiving multiple requests with increasing CSeq
        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(1);
        uacLeg.RemoteSequenceNumber.ShouldBe(1L);

        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(3);
        uacLeg.RemoteSequenceNumber.ShouldBe(3L);

        ((CallLeg)uacLeg).UpdateRemoteSequenceNumber(5);
        uacLeg.RemoteSequenceNumber.ShouldBe(5L);
    }

    #endregion
}
