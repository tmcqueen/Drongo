using Drongo.Core.SIP.Dialogs;
using Drongo.Core.Hosting;
using Drongo.Core.SIP.Messages;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Drongo.Core.Tests.Integration;

public class InviteRouterIntegrationTests
{
    private readonly ILogger<CallLeg> _logger;
    private readonly CallLegOrchestrator _orchestrator;

    public InviteRouterIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        _logger = loggerFactory.CreateLogger<CallLeg>();
        _orchestrator = new CallLegOrchestrator(_logger);
    }

    [Fact]
    public async Task InviteRouter_WithInviteRequest_CreatesDialogThroughOrchestrator()
    {
        var callId = "call-invite-001";
        var uacTag = "uac-tag-001";
        var uasTag = "uas-tag-002";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        var inviteRequest = new SipRequest(
            SipMethod.Invite,
            uasUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        var handlers = new List<Func<InviteContext, Task>>
        {
            async context =>
            {
                if (context.Dialog == null && context.Request.Method == SipMethod.Invite)
                {
                    var fromTag = Dialog.ExtractTag(context.Request.From);
                    var toTag = Dialog.ExtractTag(context.Request.To);
                    
                    if (!string.IsNullOrEmpty(fromTag) && !string.IsNullOrEmpty(toTag))
                    {
                        _orchestrator.CreateCallLegPair(
                            context.Request.CallId,
                            fromTag,
                            toTag,
                            uacUri,
                            uasUri,
                            isSecure: false);
                    }
                }
                await Task.CompletedTask;
            }
        };

        var router = new InviteRouter(handlers);
        var remoteEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.0.2.1"), 5060);
        
        var inviteContext = new InviteContext
        {
            Request = inviteRequest,
            RemoteEndpoint = remoteEndpoint,
            Router = router
        };

        await router.RouteAsync(inviteContext);

        _orchestrator.TryGetCallLegs(callId, out var uacLeg, out var uasLeg).ShouldBeTrue();
        uacLeg.ShouldNotBeNull();
        uasLeg.ShouldNotBeNull();
    }

    [Fact]
    public async Task EndToEndFlow_InviteThroughByeAcknowledgement_CompletesSuccessfully()
    {
        var callId = "call-e2e-001";
        var uacTag = "uac-e2e-001";
        var uasTag = "uas-e2e-001";
        var uacUri = new SipUri("sip", "alice@example.com", 5060);
        var uasUri = new SipUri("sip", "bob@example.com", 5060);

        var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
            callId, uacTag, uasTag, uacUri, uasUri, false);

        var confirmResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                ["From"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["To"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "1 INVITE"
            });

        _orchestrator.RouteFinalResponse(callId, confirmResponse);
        _orchestrator.IsDialogConfirmed(callId).ShouldBeTrue();

        var byeRequest = new SipRequest(
            SipMethod.Bye,
            uacUri,
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.0.2.2:5060",
                ["From"] = $"Bob <sip:bob@example.com>;tag={uasTag}",
                ["To"] = $"Alice <sip:alice@example.com>;tag={uacTag}",
                ["Call-ID"] = callId,
                ["CSeq"] = "2 BYE"
            });

        var routedBye = _orchestrator.RouteInDialogRequest(callId, byeRequest);
        routedBye.ShouldNotBeNull();

        var byeOkResponse = new SipResponse(200, "OK", "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = byeRequest.Via,
                ["From"] = byeRequest.From,
                ["To"] = byeRequest.To,
                ["Call-ID"] = byeRequest.CallId,
                ["CSeq"] = "2 BYE"
            });

        _orchestrator.RouteFinalResponse(callId, byeOkResponse);

        _orchestrator.TryGetCallLegs(callId, out uacLeg, out uasLeg).ShouldBeTrue();
        uacLeg!.State.ShouldBe(CallLegState.Terminated);
        uasLeg!.State.ShouldBe(CallLegState.Terminated);
    }

    [Fact]
    public async Task ConcurrentDialogs_MultipleSimultaneousDialogs_HandledIndependently()
    {
        var numDialogs = 10;
        var dialogTasks = new List<Task>();

        for (int i = 0; i < numDialogs; i++)
        {
            var callId = $"call-concurrent-{i:03d}";
            var uacTag = $"uac-{i}";
            var uasTag = $"uas-{i}";
            var uacUri = new SipUri("sip", $"alice{i}@example.com", 5060);
            var uasUri = new SipUri("sip", $"bob{i}@example.com", 5060);

            var task = Task.Run(() =>
            {
                var (uacLeg, uasLeg) = _orchestrator.CreateCallLegPair(
                    callId, uacTag, uasTag, uacUri, uasUri, false);

                var response = new SipResponse(200, "OK", "SIP/2.0",
                    new Dictionary<string, string>
                    {
                        ["Via"] = "SIP/2.0/UDP 192.0.2.1:5060",
                        ["From"] = $"Alice <sip:alice{i}@example.com>;tag={uacTag}",
                        ["To"] = $"Bob <sip:bob{i}@example.com>;tag={uasTag}",
                        ["Call-ID"] = callId,
                        ["CSeq"] = "1 INVITE"
                    });
                _orchestrator.RouteFinalResponse(callId, response);

                _orchestrator.IsDialogConfirmed(callId).ShouldBeTrue();
            });

            dialogTasks.Add(task);
        }

        await Task.WhenAll(dialogTasks);

        for (int i = 0; i < numDialogs; i++)
        {
            var callId = $"call-concurrent-{i:03d}";
            _orchestrator.IsDialogConfirmed(callId).ShouldBeTrue();
        }
    }
}
