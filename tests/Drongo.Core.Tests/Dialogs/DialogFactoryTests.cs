using System.Net;
using Drongo.Core.Dialogs;
using Drongo.Core.Messages;
using Drongo.Core.Parsing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Shouldly;

namespace Drongo.Core.Tests.Dialogs;

public class DialogFactoryTests
{
    private readonly ILogger<Dialog> _logger;
    private readonly DialogFactory _factory;

    public DialogFactoryTests()
    {
        _logger = Substitute.For<ILogger<Dialog>>();
        _factory = new DialogFactory(_logger);
    }

    [Fact]
    public void CreateUasDialog_FromInvite_CreatesEarlyDialog()
    {
        var request = CreateInviteRequest();
        var remoteEndpoint = new IPEndPoint(System.Net.IPAddress.Parse("192.0.2.1"), 5060);

        var dialog = _factory.CreateUasDialog(request, remoteEndpoint);

        dialog.CallId.ShouldBe("test-call-id");
        dialog.LocalTag.ShouldNotBeEmpty();
        dialog.RemoteTag.ShouldBe("caller-tag");
        dialog.State.ShouldBe(DialogState.Early);
    }

    [Fact]
    public void CreateUacDialog_From2xxResponse_CreatesConfirmedDialog()
    {
        var request = CreateInviteRequest();
        var response = CreateSuccessResponse();
        var remoteEndpoint = new IPEndPoint(System.Net.IPAddress.Parse("192.0.2.1"), 5060);

        var dialog = _factory.CreateUacDialog(request, response, remoteEndpoint);

        dialog.CallId.ShouldBe("test-call-id");
        dialog.LocalTag.ShouldBe("caller-tag");
        dialog.RemoteTag.ShouldBe("callee-tag");
        dialog.State.ShouldBe(DialogState.Confirmed);
    }

    [Fact]
    public void CreateUasDialog_FromInviteWithContact_SetsRemoteTarget()
    {
        var request = CreateInviteRequest();
        var remoteEndpoint = new IPEndPoint(System.Net.IPAddress.Parse("192.0.2.1"), 5060);

        var dialog = _factory.CreateUasDialog(request, remoteEndpoint);

        dialog.RemoteTarget.ShouldNotBeNull();
    }

    [Fact]
    public void CreateUacDialog_From2xxWithContact_SetsRemoteTarget()
    {
        var request = CreateInviteRequest();
        var response = CreateSuccessResponse();
        var remoteEndpoint = new IPEndPoint(System.Net.IPAddress.Parse("192.0.2.1"), 5060);

        var dialog = _factory.CreateUacDialog(request, response, remoteEndpoint);

        dialog.RemoteTarget.ShouldNotBeNull();
    }

    private static SipRequest CreateInviteRequest()
    {
        return new SipRequest(
            SipMethod.Invite,
            new SipUri("sip", "callee@example.com", 5060),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP caller.example.com;branch=z9hG4bK776asdhds",
                ["From"] = "Caller <sip:caller@example.com>;tag=caller-tag",
                ["To"] = "Callee <sip:callee@example.com>",
                ["Call-ID"] = "test-call-id",
                ["CSeq"] = "1 INVITE",
                ["Contact"] = "<sip:caller@192.0.2.100>",
                ["Max-Forwards"] = "70"
            });
    }

    private static SipResponse CreateSuccessResponse()
    {
        return new SipResponse(
            200,
            "OK",
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP caller.example.com;branch=z9hG4bK776asdhds",
                ["From"] = "Caller <sip:caller@example.com>;tag=caller-tag",
                ["To"] = "Callee <sip:callee@example.com>;tag=callee-tag",
                ["Call-ID"] = "test-call-id",
                ["CSeq"] = "1 INVITE",
                ["Contact"] = "<sip:callee@192.0.2.200>"
            });
    }
}
