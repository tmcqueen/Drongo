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

public class InviteServerTransactionTests
{
    private readonly ITimerFactory _timerFactory;
    private readonly ILogger<InviteServerTransaction> _logger;
    private readonly EndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 5060);

    public InviteServerTransactionTests()
    {
        _timerFactory = Substitute.For<ITimerFactory>();
        _logger = Substitute.For<ILogger<InviteServerTransaction>>();
        
        _timerFactory.T1.Returns(TimeSpan.FromMilliseconds(500));
        _timerFactory.T2.Returns(TimeSpan.FromSeconds(4));
        _timerFactory.T4.Returns(TimeSpan.FromSeconds(5));
        _timerFactory.TimerH.Returns(TimeSpan.FromSeconds(32));
        _timerFactory.TimerI.Returns(TimeSpan.FromSeconds(5));
        
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
            ["Contact"] = "<sip:alice@atlanta.com>",
            ["Max-Forwards"] = "70"
        });

    [Fact]
    public void Start_RequestReceived_EntersProceedingState()
    {
        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        var request = CreateInviteRequest();
        
        tx.Start(request, _remoteEndPoint);
        
        tx.State.ShouldBe(ServerTransactionState.Proceeding);
        tx.Request.ShouldBe(request);
    }

    [Fact]
    public async Task SendResponse_1xxInProceeding_ForwardsResponse()
    {
        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        tx.Start(CreateInviteRequest(), _remoteEndPoint);
        
        var response = SipResponse.CreateRinging(new Dictionary<string, string>());
        
        await tx.SendResponseAsync(response);
        
        tx.State.ShouldBe(ServerTransactionState.Proceeding);
    }

    [Fact]
    public async Task SendResponse_2xxInProceeding_ForwardsResponseAndTerminates()
    {
        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        tx.Start(CreateInviteRequest(), _remoteEndPoint);
        
        var response = SipResponse.CreateOk(new Dictionary<string, string>());
        
        await tx.SendResponseAsync(response);
        
        tx.State.ShouldBe(ServerTransactionState.Terminated);
    }

    [Fact]
    public async Task SendResponse_300To699InProceeding_EntersCompletedAndStartsTimerG()
    {
        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        tx.Start(CreateInviteRequest(), _remoteEndPoint);
        
        var response = SipResponse.Create(486, "Busy Here", new Dictionary<string, string>());
        
        await tx.SendResponseAsync(response);
        
        tx.State.ShouldBe(ServerTransactionState.Completed);
        _timerFactory.Received().Create();
    }

    [Fact]
    public async Task SendResponse_300To699InTerminated_IgnoresResponse()
    {
        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        tx.Start(CreateInviteRequest(), _remoteEndPoint);
        
        await tx.SendResponseAsync(SipResponse.CreateOk(new Dictionary<string, string>()));
        
        var response = SipResponse.Create(486, "Busy Here", new Dictionary<string, string>());
        await tx.SendResponseAsync(response);
        
        tx.State.ShouldBe(ServerTransactionState.Terminated);
    }

    [Fact]
    public async Task AckReceived_InCompletedState_TransitionsToConfirmed()
    {
        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        tx.Start(CreateInviteRequest(), _remoteEndPoint);
        
        await tx.SendResponseAsync(SipResponse.Create(486, "Busy Here", new Dictionary<string, string>()));
        
        tx.AckReceived();
        
        tx.State.ShouldBe(ServerTransactionState.Confirmed);
    }

    [Fact]
    public void AckReceived_InProceedingState_Ignored()
    {
        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        tx.Start(CreateInviteRequest(), _remoteEndPoint);
        
        tx.AckReceived();
        
        tx.State.ShouldBe(ServerTransactionState.Proceeding);
    }

    [Fact]
    public void RetransmitRequest_InProceeding_StateUnchanged()
    {
        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        tx.Start(CreateInviteRequest(), _remoteEndPoint);

        tx.RetransmitRequest();

        tx.State.ShouldBe(ServerTransactionState.Proceeding);
    }

    [Fact]
    public async Task OnTimerGFires_TimerRemovedConcurrentlyDuringCallback_DoesNotThrow()
    {
        // Arrange: capture the Timer G callback so we can invoke it manually.
        // The race: OnTimerGFires passes the state guard (state=Completed), calls
        // OnResponseSent (via ResponseSent event), and concurrently an ACK arrives,
        // removing Timer G from the dict before reaching _timers["G"].Change(...).
        // We reproduce this by having the ResponseSent handler deliver the ACK.
        Action? timerGCallback = null;
        var timerG = Substitute.For<ITimer>();
        var timerI = Substitute.For<ITimer>();

        var callCount = 0;
        _timerFactory.Create().Returns(_ =>
        {
            callCount++;
            return callCount switch
            {
                1 => timerG,  // StartTimerG in SendFinalNon2xxResponse
                _ => timerI,  // StartTimerI when ACK received
            };
        });

        timerG.When(t => t.Start(Arg.Any<TimeSpan>(), Arg.Any<Action>()))
              .Do(ci => timerGCallback = ci.Arg<Action>());

        var tx = new InviteServerTransaction("test", _timerFactory, _logger);
        tx.Start(CreateInviteRequest(), _remoteEndPoint);

        // Transition to Completed and start Timer G
        var busyResponse = SipResponse.Create(486, "Busy Here", new Dictionary<string, string>());
        await tx.SendResponseAsync(busyResponse);
        tx.State.ShouldBe(ServerTransactionState.Completed);
        timerGCallback.ShouldNotBeNull("Timer G callback was never registered");

        // Wire ResponseSent (fired inside OnTimerGFires via OnResponseSent) to deliver
        // an ACK, which stops Timer G (removes from dict) mid-callback.
        tx.ResponseSent += (_, _) => tx.AckReceived();

        // Act: fire the callback. State=Completed passes the guard, OnResponseSent fires,
        // the ResponseSent handler delivers ACK → StopTimer("G") removes from dict,
        // then the buggy _timers["G"] line hits a missing key → KeyNotFoundException.
        Should.NotThrow(() => timerGCallback!());
    }
}

public class NonInviteServerTransactionTests
{
    private readonly ITimerFactory _timerFactory;
    private readonly ILogger<NonInviteServerTransaction> _logger;
    private readonly EndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 5060);

    public NonInviteServerTransactionTests()
    {
        _timerFactory = Substitute.For<ITimerFactory>();
        _logger = Substitute.For<ILogger<NonInviteServerTransaction>>();
        
        _timerFactory.T1.Returns(TimeSpan.FromMilliseconds(500));
        _timerFactory.T2.Returns(TimeSpan.FromSeconds(4));
        _timerFactory.TimerJ.Returns(TimeSpan.FromSeconds(32));
        
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
    public void Start_RequestReceived_EntersTryingState()
    {
        var tx = new NonInviteServerTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        var request = CreateByeRequest();
        
        tx.Start(request, _remoteEndPoint);
        
        tx.State.ShouldBe(ServerTransactionState.Trying);
        tx.Request.ShouldBe(request);
    }

    [Fact]
    public async Task SendResponse_1xxInTrying_EntersProceeding()
    {
        var tx = new NonInviteServerTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        tx.Start(CreateByeRequest(), _remoteEndPoint);
        
        var response = SipResponse.Create(100, "Trying", new Dictionary<string, string>());
        
        await tx.SendResponseAsync(response);
        
        tx.State.ShouldBe(ServerTransactionState.Proceeding);
    }

    [Fact]
    public async Task SendResponse_1xxInProceeding_StaysProceeding()
    {
        var tx = new NonInviteServerTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        tx.Start(CreateByeRequest(), _remoteEndPoint);
        
        await tx.SendResponseAsync(SipResponse.Create(100, "Trying", new Dictionary<string, string>()));
        
        var response = SipResponse.Create(180, "Ringing", new Dictionary<string, string>());
        await tx.SendResponseAsync(response);
        
        tx.State.ShouldBe(ServerTransactionState.Proceeding);
    }

    [Fact]
    public async Task SendResponse_FinalInTrying_EntersCompletedAndStartsTimerJ()
    {
        var tx = new NonInviteServerTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        tx.Start(CreateByeRequest(), _remoteEndPoint);
        
        var response = SipResponse.CreateOk(new Dictionary<string, string>());
        
        await tx.SendResponseAsync(response);
        
        tx.State.ShouldBe(ServerTransactionState.Completed);
        _timerFactory.Received().Create();
    }

    [Fact]
    public async Task SendResponse_FinalInProceeding_EntersCompleted()
    {
        var tx = new NonInviteServerTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        tx.Start(CreateByeRequest(), _remoteEndPoint);
        
        await tx.SendResponseAsync(SipResponse.Create(100, "Trying", new Dictionary<string, string>()));
        
        var response = SipResponse.CreateOk(new Dictionary<string, string>());
        await tx.SendResponseAsync(response);
        
        tx.State.ShouldBe(ServerTransactionState.Completed);
    }

    [Fact]
    public void RetransmitRequest_InTrying_Discarded()
    {
        var tx = new NonInviteServerTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        tx.Start(CreateByeRequest(), _remoteEndPoint);
        
        tx.RetransmitRequest();
        
        tx.State.ShouldBe(ServerTransactionState.Trying);
    }

    [Fact]
    public void AckReceived_IgnoredForNonInvite()
    {
        var tx = new NonInviteServerTransaction("test", SipMethod.Bye, _timerFactory, _logger);
        tx.Start(CreateByeRequest(), _remoteEndPoint);
        
        tx.AckReceived();
        
        tx.State.ShouldBe(ServerTransactionState.Trying);
    }
}

public class ServerTransactionFactoryTests
{
    private readonly ITimerFactory _timerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly EndPoint _remoteEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 5060);

    public ServerTransactionFactoryTests()
    {
        _timerFactory = Substitute.For<ITimerFactory>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        
        _timerFactory.Create().Returns(ci => Substitute.For<ITimer>());
        
        _loggerFactory.CreateLogger<ServerTransaction>()
            .Returns(Substitute.For<ILogger<ServerTransaction>>());
        _loggerFactory.CreateLogger<InviteServerTransaction>()
            .Returns(Substitute.For<ILogger<InviteServerTransaction>>());
        _loggerFactory.CreateLogger<NonInviteServerTransaction>()
            .Returns(Substitute.For<ILogger<NonInviteServerTransaction>>());
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
    public void CreateTransaction_Invite_CreatesInviteServerTransaction()
    {
        var factory = new ServerTransactionFactory(_timerFactory, _loggerFactory);
        var request = CreateInviteRequest();
        
        var tx = factory.CreateTransaction(request, _remoteEndPoint);
        
        tx.Method.ShouldBe(SipMethod.Invite);
    }

    [Fact]
    public void CreateTransaction_NonInvite_CreatesNonInviteServerTransaction()
    {
        var factory = new ServerTransactionFactory(_timerFactory, _loggerFactory);
        var request = CreateByeRequest();
        
        var tx = factory.CreateTransaction(request, _remoteEndPoint);
        
        tx.Method.ShouldBe(SipMethod.Bye);
    }

    [Fact]
    public void MatchTransaction_WithMatchingBranch_ReturnsTransaction()
    {
        var factory = new ServerTransactionFactory(_timerFactory, _loggerFactory);
        var request = CreateInviteRequest();
        
        var tx = factory.CreateTransaction(request, _remoteEndPoint);
        
        var matched = factory.MatchTransaction(request, _remoteEndPoint);
        
        matched.ShouldNotBeNull();
        matched.Id.ShouldBe(tx.Id);
    }

    [Fact]
    public void MatchTransaction_WithoutMatchingBranch_ReturnsNull()
    {
        var factory = new ServerTransactionFactory(_timerFactory, _loggerFactory);
        
        var request = CreateInviteRequest();
        
        var matched = factory.MatchTransaction(request, _remoteEndPoint);
        
        matched.ShouldBeNull();
    }

    [Fact]
    public void RemoveTransaction_RemovesFromFactory()
    {
        var factory = new ServerTransactionFactory(_timerFactory, _loggerFactory);
        var request = CreateInviteRequest();
        
        var tx = factory.CreateTransaction(request, _remoteEndPoint);
        factory.RemoveTransaction(tx.Id);
        
        var matched = factory.MatchTransaction(request, _remoteEndPoint);
        
        matched.ShouldBeNull();
    }
}
