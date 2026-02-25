using Drongo.Core.Dialogs;
using Drongo.Core.Messages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Shouldly;

namespace Drongo.Core.Tests.Dialogs;

public class DialogManagerTests
{
    private readonly IDialogManager _dialogManager;
    private readonly ILogger<Dialog> _logger;

    public DialogManagerTests()
    {
        _logger = Substitute.For<ILogger<Dialog>>();
        _dialogManager = new DialogManager(_logger);
    }

    [Fact]
    public void CreateDialog_WithValidParams_CreatesAndReturnsDialog()
    {
        var callId = "test-call-id";
        var localTag = "local-tag-123";
        var localUri = new SipUri("sip", "alice@example.com", 5060);
        var remoteUri = new SipUri("sip", "bob@example.com", 5060);

        var dialog = _dialogManager.CreateDialog(callId, localTag, localUri, remoteUri, false);

        dialog.ShouldNotBeNull();
        dialog.CallId.ShouldBe(callId);
        dialog.LocalTag.ShouldBe(localTag);
    }

    [Fact]
    public void TryGetDialog_WithExistingDialog_ReturnsTrue()
    {
        var callId = "test-call-id";
        var localTag = "local-tag-123";
        var remoteTag = "remote-tag-456";
        var localUri = new SipUri("sip", "alice@example.com", 5060);
        var remoteUri = new SipUri("sip", "bob@example.com", 5060);

        var createdDialog = _dialogManager.CreateDialog(callId, localTag, localUri, remoteUri, false);
        // Per RFC3261 Section 12: In a UAS, From header contains remote tag, To header contains local tag
        createdDialog.HandleUasRequest(new SipRequest(
            SipMethod.Invite,
            new SipUri("sip", "alice@example.com", 5060),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["From"] = $"Bob <sip:bob@example.com>;tag={remoteTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={localTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE",
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060"
            }));

        var result = _dialogManager.TryGetDialog(callId, localTag, remoteTag, out var retrievedDialog);

        result.ShouldBeTrue();
        retrievedDialog.ShouldBe(createdDialog);
    }

    [Fact]
    public void TryGetDialog_WithMissingDialog_ReturnsFalse()
    {
        var result = _dialogManager.TryGetDialog("non-existent", "local-tag", "remote-tag", out var dialog);

        result.ShouldBeFalse();
        dialog.ShouldBeNull();
    }

    [Fact]
    public void TryGetDialog_WithIncorrectLocalTag_ReturnsFalse()
    {
        var callId = "test-call-id";
        var localTag = "local-tag-123";
        var remoteTag = "remote-tag-456";
        var localUri = new SipUri("sip", "alice@example.com", 5060);
        var remoteUri = new SipUri("sip", "bob@example.com", 5060);

        _dialogManager.CreateDialog(callId, localTag, localUri, remoteUri, false);

        var result = _dialogManager.TryGetDialog(callId, "wrong-local-tag", remoteTag, out var dialog);

        result.ShouldBeFalse();
        dialog.ShouldBeNull();
    }

    [Fact]
    public void RemoveDialog_WithExistingDialog_RemovesSuccessfully()
    {
        var callId = "test-call-id";
        var localTag = "local-tag-123";
        var remoteTag = "remote-tag-456";
        var localUri = new SipUri("sip", "alice@example.com", 5060);
        var remoteUri = new SipUri("sip", "bob@example.com", 5060);

        var dialog = _dialogManager.CreateDialog(callId, localTag, localUri, remoteUri, false);
        dialog.HandleUasRequest(new SipRequest(
            SipMethod.Invite,
            new SipUri("sip", "alice@example.com", 5060),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["From"] = $"Bob <sip:bob@example.com>;tag={remoteTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={localTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE",
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060"
            }));

        _dialogManager.RemoveDialog(callId, localTag, remoteTag);

        var result = _dialogManager.TryGetDialog(callId, localTag, remoteTag, out var removedDialog);
        result.ShouldBeFalse();
    }

    [Fact]
    public void ActiveDialogCount_ReflectsCreatedAndRemovedDialogs()
    {
        var localUri = new SipUri("sip", "alice@example.com", 5060);
        var remoteUri = new SipUri("sip", "bob@example.com", 5060);

        _dialogManager.ActiveDialogCount.ShouldBe(0);

        var dialog1 = _dialogManager.CreateDialog("call-1", "tag-1", localUri, remoteUri, false);
        _dialogManager.ActiveDialogCount.ShouldBe(1);

        var dialog2 = _dialogManager.CreateDialog("call-2", "tag-2", localUri, remoteUri, false);
        _dialogManager.ActiveDialogCount.ShouldBe(2);

        // Setup for removal - From has remote tag, To has local tag
        dialog1.HandleUasRequest(new SipRequest(
            SipMethod.Invite,
            new SipUri("sip", "alice@example.com", 5060),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["From"] = "Bob <sip:bob@example.com>;tag=rtag1",
                ["To"] = "Alice <sip:alice@example.com>;tag=tag-1",
                ["Call-ID"] = "call-1",
                ["CSeq"] = "1 INVITE",
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060"
            }));

        _dialogManager.RemoveDialog("call-1", "tag-1", "rtag1");
        _dialogManager.ActiveDialogCount.ShouldBe(1);
    }
}
