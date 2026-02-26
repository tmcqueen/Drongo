using Drongo.Core.Forking;
using Xunit;

namespace Drongo.Core.Tests.Forking;

/// <summary>
/// Tests for ForkingManager per RFC 3261 Section 16.6 (Parallel forking).
/// </summary>
public class ForkingManagerTests
{
    [Fact]
    public void ForkingManager_AddTarget_ReturnsTargetId()
    {
        var manager = new ForkingManager();
        
        var targetId = manager.AddTarget("sip:user1@example.com");
        
        Assert.NotNull(targetId);
        Assert.NotEmpty(targetId);
    }

    [Fact]
    public void ForkingManager_AddMultipleTargets_ReturnsUniqueIds()
    {
        var manager = new ForkingManager();
        
        var id1 = manager.AddTarget("sip:user1@example.com");
        var id2 = manager.AddTarget("sip:user2@example.com");
        
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ForkingManager_AddTarget_TracksAllTargets()
    {
        var manager = new ForkingManager();
        
        manager.AddTarget("sip:user1@example.com");
        manager.AddTarget("sip:user2@example.com");
        
        var targets = manager.GetAllTargets();
        
        Assert.Equal(2, targets.Count);
    }

    [Fact]
    public void ForkingManager_RecordResponse_StoresResponse()
    {
        var manager = new ForkingManager();
        
        var targetId = manager.AddTarget("sip:user1@example.com");
        manager.RecordResponse(targetId, 180, "Ringing");
        
        var responses = manager.GetResponses(targetId);
        
        Assert.Single(responses);
        Assert.Equal(180, responses[0].StatusCode);
    }

    [Fact]
    public void ForkingManager_First2xx_Wins()
    {
        var manager = new ForkingManager();
        
        var id1 = manager.AddTarget("sip:user1@example.com");
        var id2 = manager.AddTarget("sip:user2@example.com");
        
        manager.RecordResponse(id1, 180, "Ringing");
        manager.RecordResponse(id2, 180, "Ringing");
        manager.RecordResponse(id2, 200, "OK");
        
        var winningTarget = manager.GetWinningTarget();
        
        Assert.Equal(id2, winningTarget);
    }

    [Fact]
    public void ForkingManager_6xx_EliminatesBranch()
    {
        var manager = new ForkingManager();
        
        var id1 = manager.AddTarget("sip:user1@example.com");
        var id2 = manager.AddTarget("sip:user2@example.com");
        
        manager.RecordResponse(id1, 486, "Busy Here");
        
        var isAlive = manager.IsBranchAlive(id1);
        
        Assert.False(isAlive);
    }

    [Fact]
    public void ForkingManager_AllBranchesDead_ReturnsNoWinner()
    {
        var manager = new ForkingManager();
        
        var id1 = manager.AddTarget("sip:user1@example.com");
        var id2 = manager.AddTarget("sip:user2@example.com");
        
        manager.RecordResponse(id1, 486, "Busy Here");
        manager.RecordResponse(id2, 603, "Decline");
        
        var winningTarget = manager.GetWinningTarget();
        
        Assert.Null(winningTarget);
    }

    [Fact]
    public void ForkingManager_GetPendingTargets_ReturnsUnanswered()
    {
        var manager = new ForkingManager();
        
        var id1 = manager.AddTarget("sip:user1@example.com");
        var id2 = manager.AddTarget("sip:user2@example.com");
        
        manager.RecordResponse(id1, 180, "Ringing");
        
        var pending = manager.GetPendingTargets();
        
        Assert.Single(pending);
        Assert.Contains(id2, pending);
    }

    [Fact]
    public void ForkingManager_CancelPending_CancelsUnanswered()
    {
        var manager = new ForkingManager();
        
        var id1 = manager.AddTarget("sip:user1@example.com");
        var id2 = manager.AddTarget("sip:user2@example.com");
        
        manager.RecordResponse(id1, 180, "Ringing");
        manager.RecordResponse(id2, 180, "Ringing");
        manager.RecordResponse(id2, 200, "OK");
        
        var toCancel = manager.GetTargetsToCancel();
        
        Assert.Single(toCancel);
        Assert.Contains(id1, toCancel);
    }

    [Fact]
    public void ForkingManager_RemoveTarget_RemovesFromTracking()
    {
        var manager = new ForkingManager();
        
        var targetId = manager.AddTarget("sip:user1@example.com");
        manager.RemoveTarget(targetId);
        
        var targets = manager.GetAllTargets();
        
        Assert.Empty(targets);
    }
}
