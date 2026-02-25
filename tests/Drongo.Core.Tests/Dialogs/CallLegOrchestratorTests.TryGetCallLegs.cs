using Drongo.Core.Messages;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Dialogs;

/// <summary>
/// Tests for TryGetCallLegs dialog lookup functionality.
/// Verifies successful retrieval of leg pairs and proper handling of missing dialogs.
/// </summary>
public partial class CallLegOrchestratorTests
{
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
}
