using System.Net;
using Drongo.Core.Messages;
using Drongo.Core.Timers;
using Drongo.Core.Transactions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Shouldly;
using ITimer = Drongo.Core.Timers.ITimer;

namespace Drongo.Core.Tests.Transactions;

public class InviteClientTransactionTests
{
    private readonly ITimerFactory _timerFactory;
    private readonly ILogger<InviteClientTransaction> _logger;
    private readonly EndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 5060);

    public InviteClientTransactionTests()
    {
        _timerFactory = Substitute.For<ITimerFactory>();
        _logger = Substitute.For<ILogger<InviteClientTransaction>>();
        
        _timerFactory.T1.Returns(TimeSpan.FromMilliseconds(500));
        _timerFactory.T2.Returns(TimeSpan.FromSeconds(4));
        _timerFactory.TimerB.Returns(TimeSpan.FromSeconds(32));
        _timerFactory.TimerD.Returns(TimeSpan.FromSeconds(32));
        
        _timerFactory.Create().Returns(ci => Substitute.For<ITimer>());
    }

    private SipRequest CreateInviteRequest() => new(
        SipMethod.Invite,
        new SipUri("sip", "bob@biloxi.com"),
        "SIP/2.0",
        new Dictionary<string, string>
        {
            ["Via"] = "SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds",
            ["From"] = "Alice <sip:alice@atlanta.com>;tag=1928301774",
            ["To"] = "Bob <sip:bob@biloxi.com>",
            ["Call-ID"] = "test-call-id",
            ["CSeq"] = "1 INVITE",
            ["Max-Forwards"] = "70"
        });

    [Fact]
    public async Task StartAsync_RequestSent_EntersCallingState()
    {
        var tx = new InviteClientTransaction("test", _timerFactory, _logger);
        
        await tx.StartAsync(CreateInviteRequest(), _remoteEndPoint);
        
        tx.State.ShouldBe(ClientTransactionState.Calling);
    }

    [Fact]
    public async Task ReceiveResponse_1xxInCalling_EntersProceedingState()
    {
        var tx = new InviteClientTransaction("test", _timerFactory, _logger);
        await tx.StartAsync(CreateInviteRequest(), _remoteEndPoint);
        
        var response = SipResponse.Create(180, "Ringing", new Dictionary<string, string>());
        
        tx.ReceiveResponse(response);
        
        tx.State.ShouldBe(ClientTransactionState.Proceeding);
    }

    [Fact]
    public async Task ReceiveResponse_2xxInCalling_Terminates()
    {
        var tx = new InviteClientTransaction("test", _timerFactory, _logger);
        await tx.StartAsync(CreateInviteRequest(), _remoteEndPoint);
        
        var response = SipResponse.CreateOk(new Dictionary<string, string>());
        
        tx.ReceiveResponse(response);
        
        tx.State.ShouldBe(ClientTransactionState.Terminated);
    }

    [Fact]
    public async Task ReceiveResponse_300To699InCalling_EntersCompletedState()
    {
        var tx = new InviteClientTransaction("test", _timerFactory, _logger);
        await tx.StartAsync(CreateInviteRequest(), _remoteEndPoint);
        
        var response = SipResponse.Create(486, "Busy Here", new Dictionary<string, string>());
        
        tx.ReceiveResponse(response);
        
        tx.State.ShouldBe(ClientTransactionState.Completed);
    }

    [Fact]
    public async Task ReceiveResponse_1xxInProceeding_StaysProceeding()
    {
        var tx = new InviteClientTransaction("test", _timerFactory, _logger);
        await tx.StartAsync(CreateInviteRequest(), _remoteEndPoint);
        
        tx.ReceiveResponse(SipResponse.Create(180, "Ringing", new Dictionary<string, string>()));
        
        tx.ReceiveResponse(SipResponse.Create(181, "Call Is Being Forwarded", new Dictionary<string, string>()));
        
        tx.State.ShouldBe(ClientTransactionState.Proceeding);
    }

    [Fact]
    public async Task TransportError_InCallingState_Terminates()
    {
        var tx = new InviteClientTransaction("test", _timerFactory, _logger);
        await tx.StartAsync(CreateInviteRequest(), _remoteEndPoint);
        
        tx.TransportError();
        
        tx.State.ShouldBe(ClientTransactionState.Terminated);
    }
}

public class NonInviteClientTransactionTests
{
    private readonly ITimerFactory _timerFactory;
    private readonly ILogger<NonInviteClientTransaction> _logger;
    private readonly EndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 5060);

    public NonInviteClientTransactionTests()
    {
        _timerFactory = Substitute.For<ITimerFactory>();
        _logger = Substitute.For<ILogger<NonInviteClientTransaction>>();
        
        _timerFactory.T1.Returns(TimeSpan.FromMilliseconds(500));
        _timerFactory.T2.Returns(TimeSpan.FromSeconds(4));
        _timerFactory.TimerF.Returns(TimeSpan.FromSeconds(32));
        _timerFactory.TimerK.Returns(TimeSpan.FromSeconds(5));
        
        _timerFactory.Create().Returns(ci => Substitute.For<ITimer>());
    }

    private SipRequest CreateByeRequest() => new(
        SipMethod.Bye,
        new SipUri("sip", "bob@biloxi.com"),
        "SIP/2.0",
        new Dictionary<string, string>
        {
            ["Via"] = "SIP/2.0/UDP pc33.atlanta.com;branch=z9hG4bK776asdhds",
            ["From"] = "Alice <sip:alice@atlanta.com>;tag=1928301774",
            ["To"] = "Bob <sip:bob@biloxi.com>",
            ["Call-ID"] = "test-call-id",
            ["CSeq"] = "2 BYE",
            ["Max-Forwards"] = "70"
        });

    [Fact]
    public async Task StartAsync_RequestSent_EntersTryingState()
    {
        var tx = new NonInviteClientTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        
        await tx.StartAsync(CreateByeRequest(), _remoteEndPoint);
        
        tx.State.ShouldBe(ClientTransactionState.Trying);
    }

    [Fact]
    public async Task ReceiveResponse_1xxInTrying_EntersProceedingState()
    {
        var tx = new NonInviteClientTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        await tx.StartAsync(CreateByeRequest(), _remoteEndPoint);
        
        var response = SipResponse.Create(100, "Trying", new Dictionary<string, string>());
        
        tx.ReceiveResponse(response);
        
        tx.State.ShouldBe(ClientTransactionState.Proceeding);
    }

    [Fact]
    public async Task ReceiveResponse_2xxInTrying_EntersCompletedState()
    {
        var tx = new NonInviteClientTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        await tx.StartAsync(CreateByeRequest(), _remoteEndPoint);
        
        var response = SipResponse.CreateOk(new Dictionary<string, string>());
        
        tx.ReceiveResponse(response);
        
        tx.State.ShouldBe(ClientTransactionState.Completed);
    }

    [Fact]
    public async Task ReceiveResponse_4xxInProceeding_EntersCompletedState()
    {
        var tx = new NonInviteClientTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        await tx.StartAsync(CreateByeRequest(), _remoteEndPoint);
        
        tx.ReceiveResponse(SipResponse.Create(100, "Trying", new Dictionary<string, string>()));
        
        var response = SipResponse.Create(481, "Call Leg/Transaction Does Not Exist", new Dictionary<string, string>());
        tx.ReceiveResponse(response);
        
        tx.State.ShouldBe(ClientTransactionState.Completed);
    }

    [Fact]
    public async Task TransportError_InTryingState_Terminates()
    {
        var tx = new NonInviteClientTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        await tx.StartAsync(CreateByeRequest(), _remoteEndPoint);

        tx.TransportError();

        tx.State.ShouldBe(ClientTransactionState.Terminated);
    }

    [Fact]
    public async Task OnTimerEFires_TimerRemovedConcurrentlyDuringCallback_DoesNotThrow()
    {
        // Arrange: capture the Timer E callback so we can invoke it manually.
        // The race: OnTimerEFires passes the state guard (state=Trying), calls Retransmit(),
        // and concurrently a final response removes Timer E from the dict before reaching
        // _timers["E"].Change(...). We reproduce this by having the RequestSent event
        // (fired inside Retransmit) deliver the final response, which stops Timer E,
        // so the dict no longer has "E" when the buggy line executes.
        Action? timerECallback = null;
        var timerF = Substitute.For<ITimer>();
        var timerE = Substitute.For<ITimer>();
        var timerK = Substitute.For<ITimer>();

        var callCount = 0;
        _timerFactory.Create().Returns(_ =>
        {
            callCount++;
            return callCount switch
            {
                1 => timerF,  // StartTimerF in StartAsync
                2 => timerE,  // StartTimerE when 1xx received
                _ => timerK,  // StartTimerK after final response
            };
        });

        timerE.When(t => t.Start(Arg.Any<TimeSpan>(), Arg.Any<Action>()))
              .Do(ci => timerECallback = ci.Arg<Action>());

        var tx = new NonInviteClientTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        await tx.StartAsync(CreateByeRequest(), _remoteEndPoint);

        // Move to Proceeding so state guard in OnTimerEFires is passed
        tx.ReceiveResponse(SipResponse.Create(100, "Trying", new Dictionary<string, string>()));
        tx.State.ShouldBe(ClientTransactionState.Proceeding);
        timerECallback.ShouldNotBeNull("Timer E callback was never registered");

        // Wire RequestSent (fired inside Retransmit inside the callback) to deliver
        // a final response, which removes Timer E from the dict mid-callback.
        tx.RequestSent += (_, _) =>
            tx.ReceiveResponse(SipResponse.CreateOk(new Dictionary<string, string>()));

        // Act: fire the callback. In Proceeding state, state guard passes, Retransmit fires,
        // the RequestSent handler delivers the 200 OK → StopTimer("E") removes from dict,
        // then the buggy _timers["E"] line executes against an empty entry → KeyNotFoundException.
        Should.NotThrow(() => timerECallback!());
    }
}

public class ClientTransactionFactoryTests
{
    private readonly ITimerFactory _timerFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ClientTransactionFactoryTests()
    {
        _timerFactory = Substitute.For<ITimerFactory>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        
        _timerFactory.Create().Returns(ci => Substitute.For<ITimer>());
        
        _loggerFactory.CreateLogger<ClientTransaction>()
            .Returns(Substitute.For<ILogger<ClientTransaction>>());
        _loggerFactory.CreateLogger<InviteClientTransaction>()
            .Returns(Substitute.For<ILogger<InviteClientTransaction>>());
        _loggerFactory.CreateLogger<NonInviteClientTransaction>()
            .Returns(Substitute.For<ILogger<NonInviteClientTransaction>>());
    }

    [Fact]
    public void CreateTransaction_Invite_CreatesInviteClientTransaction()
    {
        var factory = new ClientTransactionFactory(_timerFactory, _loggerFactory);
        var request = new SipRequest(
            SipMethod.Invite,
            new SipUri("sip", "bob@biloxi.com"),
            "SIP/2.0",
            new Dictionary<string, string> { ["Via"] = "SIP/2.0/UDP pc33.atlanta.com" });
        
        var tx = factory.CreateTransaction(request, new IPEndPoint(IPAddress.Parse("192.168.1.1"), 5060));
        
        tx.Method.ShouldBe(SipMethod.Invite);
    }

    [Fact]
    public void CreateTransaction_NonInvite_CreatesNonInviteClientTransaction()
    {
        var factory = new ClientTransactionFactory(_timerFactory, _loggerFactory);
        var request = new SipRequest(
            SipMethod.Bye,
            new SipUri("sip", "bob@biloxi.com"),
            "SIP/2.0",
            new Dictionary<string, string> { ["Via"] = "SIP/2.0/UDP pc33.atlanta.com" });
        
        var tx = factory.CreateTransaction(request, new IPEndPoint(IPAddress.Parse("192.168.1.1"), 5060));
        
        tx.Method.ShouldBe(SipMethod.Bye);
    }
}
