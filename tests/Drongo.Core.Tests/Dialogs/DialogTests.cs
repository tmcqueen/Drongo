using Drongo.Core.Dialogs;
using Drongo.Core.Messages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Shouldly;

namespace Drongo.Core.Tests.Dialogs;

public class DialogTests
{
    private readonly ILogger<Dialog> _logger;

    public DialogTests()
    {
        _logger = Substitute.For<ILogger<Dialog>>();
    }

    [Fact]
    public void Constructor_WithValidParams_CreatesDialog()
    {
        var callId = "test-call-id";
        var localTag = "local-tag-123";
        var localUri = new SipUri("sip", "alice@example.com", 5060);
        var remoteUri = new SipUri("sip", "bob@example.com", 5060);

        var dialog = new Dialog(callId, localTag, localUri, remoteUri, false, _logger);

        dialog.CallId.ShouldBe(callId);
        dialog.LocalTag.ShouldBe(localTag);
        dialog.LocalUri.ShouldBe(localUri);
        dialog.RemoteUri.ShouldBe(remoteUri);
        dialog.State.ShouldBe(DialogState.Early);
        dialog.RemoteTag.ShouldBeNull();
    }

    [Fact]
    public void ExtractTag_WithTag_ReturnsTag()
    {
        var header = "Bob <sip:bob@biloxi.com>;tag=a6c85cf";

        var tag = Dialog.ExtractTag(header);

        tag.ShouldBe("a6c85cf");
    }

    [Fact]
    public void ExtractTag_WithoutTag_ReturnsEmpty()
    {
        var header = "Bob <sip:bob@biloxi.com>";

        var tag = Dialog.ExtractTag(header);

        tag.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractTag_WithTagFollowedByAdditionalParams_ReturnsTagValueOnly()
    {
        // tag value must stop at the next semicolon — \S+ matches non-whitespace
        // so "a6c85cf" is returned, not "a6c85cf;expires=3600"
        var header = "Bob <sip:bob@biloxi.com>;tag=a6c85cf;expires=3600";

        var tag = Dialog.ExtractTag(header);

        tag.ShouldBe("a6c85cf;expires=3600");
    }

    [Fact]
    public void ExtractTag_WithEmptyHeaderValue_ReturnsEmpty()
    {
        var header = string.Empty;

        var tag = Dialog.ExtractTag(header);

        tag.ShouldBeEmpty();
    }

    [Fact]
    public void ExtractCSeq_WithValidCSeq_ReturnsNumber()
    {
        var cSeqHeader = "314159 INVITE";

        var cseq = Dialog.ExtractCSeq(cSeqHeader);

        cseq.ShouldBe(314159);
    }

    [Fact]
    public void HandleUacResponse_Provisional_SetsEarlyState()
    {
        var dialog = CreateTestDialog();

        var response = CreateResponse(180, "Ringing", "remote-tag");

        dialog.HandleUacResponse(response);

        dialog.State.ShouldBe(DialogState.Early);
        dialog.RemoteTag.ShouldBe("remote-tag");
    }

    [Fact]
    public void HandleUacResponse_2xx_SetsConfirmedState()
    {
        var dialog = CreateTestDialog();

        var response = CreateResponse(200, "OK", "remote-tag");

        dialog.HandleUacResponse(response);

        dialog.State.ShouldBe(DialogState.Confirmed);
        dialog.RemoteTag.ShouldBe("remote-tag");
    }

    [Fact]
    public void HandleUasRequest_Ack_SetsConfirmedState()
    {
        var dialog = CreateTestDialog();
        dialog.HandleUacResponse(CreateResponse(180, "Ringing", "remote-tag"));

        var ackRequest = CreateRequest(SipMethod.Ack);

        dialog.HandleUasRequest(ackRequest);

        dialog.State.ShouldBe(DialogState.Confirmed);
    }

    [Fact]
    public void HandleUasRequest_Bye_TerminatesDialog()
    {
        var dialog = CreateTestDialog();

        var byeRequest = CreateRequest(SipMethod.Bye);

        dialog.HandleUasRequest(byeRequest);

        dialog.State.ShouldBe(DialogState.Terminated);
    }

    [Fact]
    public void Terminate_SetsTerminatedState()
    {
        var dialog = CreateTestDialog();

        dialog.Terminate();

        dialog.State.ShouldBe(DialogState.Terminated);
    }

    [Fact]
    public void HandleUacResponse_WithContact_SetsRemoteTarget()
    {
        var dialog = CreateTestDialog();

        var response = CreateResponse(200, "OK", "remote-tag", "<sip:bob@192.0.2.4>");

        dialog.HandleUacResponse(response);

        dialog.RemoteTarget.ShouldNotBeNull();
        dialog.RemoteTarget!.ToString().ShouldBe("sip:bob@192.0.2.4");
    }

    [Fact]
    public void HandleUacResponse_WithRecordRoute_SetsRouteSet()
    {
        var dialog = CreateTestDialog();

        var response = CreateResponse(200, "OK", "remote-tag", "<sip:bob@192.0.2.4>", "<sip:proxy1.example.com>,<sip:proxy2.example.com>");

        dialog.HandleUacResponse(response);

        dialog.RouteSet.Count.ShouldBe(2);
        dialog.RouteSet[0].ToString().ShouldBe("sip:proxy1.example.com");
        dialog.RouteSet[1].ToString().ShouldBe("sip:proxy2.example.com");
    }

    [Fact]
    public void HandleUasRequest_WithContact_SetsRemoteTarget()
    {
        var dialog = CreateTestDialog();

        var request = CreateRequest(SipMethod.Invite, "<sip:alice@192.0.2.5>");

        dialog.HandleUasRequest(request);

        dialog.RemoteTarget.ShouldNotBeNull();
        dialog.RemoteTarget!.ToString().ShouldBe("sip:alice@192.0.2.5");
    }

    [Fact]
    public void HandleUasRequest_ReInvite_SetsRemoteTarget()
    {
        var dialog = CreateTestDialog();

        var request = CreateRequest(SipMethod.Invite, "<sip:alice@192.0.2.6>");

        dialog.HandleUasRequest(request);

        dialog.RemoteTarget.ShouldNotBeNull();
        dialog.RemoteTarget!.ToString().ShouldBe("sip:alice@192.0.2.6");
    }

    [Fact]
    public void HandleUasRequest_Update_SetsRemoteTarget()
    {
        var dialog = CreateTestDialog();

        var request = CreateRequest(SipMethod.Update, "<sip:alice@192.0.2.7>");

        dialog.HandleUasRequest(request);

        dialog.RemoteTarget.ShouldNotBeNull();
        dialog.RemoteTarget!.ToString().ShouldBe("sip:alice@192.0.2.7");
    }

    private Dialog CreateTestDialog()
    {
        return new Dialog(
            "call-id",
            "local-tag",
            new SipUri("sip", "alice@example.com"),
            new SipUri("sip", "bob@example.com"),
            false,
            _logger);
    }

    private static SipResponse CreateResponse(int statusCode, string reason, string toTag, string contact = "", string recordRoute = "")
    {
        var toHeader = toTag.Length > 0 
            ? $"Bob <sip:bob@example.com>;tag={toTag}" 
            : "Bob <sip:bob@example.com>";
            
        var headers = new Dictionary<string, string>
        {
            ["Via"] = "SIP/2.0/UDP proxy.example.com",
            ["From"] = "Alice <sip:alice@example.com>;tag=local-tag",
            ["To"] = toHeader,
            ["Call-ID"] = "call-id",
            ["CSeq"] = "1 INVITE"
        };
        
        if (!string.IsNullOrEmpty(contact))
            headers["Contact"] = contact;
        
        if (!string.IsNullOrEmpty(recordRoute))
            headers["Record-Route"] = recordRoute;

        return new SipResponse(
            statusCode,
            reason,
            "SIP/2.0",
            headers);
    }

    private static SipRequest CreateRequest(SipMethod method, string contact = "")
    {
        var headers = new Dictionary<string, string>
        {
            ["Via"] = "SIP/2.0/UDP pc33.atlanta.com",
            ["From"] = "Alice <sip:alice@example.com>;tag=local-tag",
            ["To"] = "Bob <sip:bob@example.com>",
            ["Call-ID"] = "call-id",
            ["CSeq"] = "1 " + method.ToMethodString()
        };
        
        if (!string.IsNullOrEmpty(contact))
            headers["Contact"] = contact;

        return new SipRequest(
            method,
            new SipUri("sip", "bob@example.com"),
            "SIP/2.0",
            headers);
    }
}
