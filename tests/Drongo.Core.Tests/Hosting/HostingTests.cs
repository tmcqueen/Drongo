using System.Net;
using System.Net.Sockets;
using Drongo.Core.Hosting;
using Drongo.Core.SIP.Messages;
using Drongo.Core.SIP.Registration;
using NSubstitute;
using Xunit;
using Shouldly;

namespace Drongo.Core.Tests.Hosting;

public class EndpointInfoTests
{
    [Fact]
    public void Create_ValidParameters_ReturnsEndpointInfo()
    {
        var endpoint = new EndpointInfo(ProtocolType.Udp, IPAddress.Any, 5060);

        endpoint.Protocol.ShouldBe(ProtocolType.Udp);
        endpoint.Address.ShouldBe(IPAddress.Any);
        endpoint.Port.ShouldBe(5060);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var endpoint = new EndpointInfo(ProtocolType.Udp, IPAddress.Parse("192.168.1.1"), 5060);

        endpoint.ToString().ShouldBe("Udp 192.168.1.1:5060");
    }
}

public class EndpointBuilderTests
{
    [Fact]
    public void Create_WithAddressAndPort_SetsProperties()
    {
        var builder = new EndpointBuilder(IPAddress.Any, 5060);

        builder.Address.ShouldBe(IPAddress.Any);
        builder.Port.ShouldBe(5060);
    }

    [Fact]
    public void WithTransport_SetsTransportType()
    {
        var builder = new EndpointBuilder(IPAddress.Any, 5060);
        builder.WithTransport<Socket>();

        var endpoints = builder.Build();

        endpoints.Count.ShouldBe(1);
        endpoints[0].Protocol.ShouldBe(ProtocolType.Tcp);
    }

    [Fact]
    public void MapEndpoint_AddsNewEndpoint()
    {
        var builder = new EndpointBuilder(IPAddress.Any, 5060);
        builder.MapEndpoint(IPAddress.Any, 5061);

        var endpoints = builder.Build();

        endpoints.Count.ShouldBe(2);
        endpoints[0].Port.ShouldBe(5060);
        endpoints[1].Port.ShouldBe(5061);
    }

    [Fact]
    public void WithTransport_AppliesToAllEndpoints()
    {
        var builder = new EndpointBuilder(IPAddress.Any, 5060);
        builder.MapEndpoint(IPAddress.Any, 5061);
        builder.WithTransport<Socket>();

        var endpoints = builder.Build();

        endpoints[0].Protocol.ShouldBe(ProtocolType.Tcp);
        endpoints[1].Protocol.ShouldBe(ProtocolType.Tcp);
    }

    [Fact]
    public void Build_Default_UsesUdp()
    {
        var builder = new EndpointBuilder(IPAddress.Any, 5060);

        var endpoints = builder.Build();

        endpoints[0].Protocol.ShouldBe(ProtocolType.Udp);
    }

    [Fact]
    public void WithTls_SetsTlsInfo()
    {
        var builder = new EndpointBuilder(IPAddress.Any, 5061);
        builder.WithTls("cert.pfx", "password");

        var endpoints = builder.Build();

        endpoints[0].Protocol.ShouldBe(ProtocolType.Tcp);
    }
}

public class ApplicationLifetimeTests
{
    [Fact]
    public void Create_HasEvents()
    {
        var lifetime = new ApplicationLifetime();

        lifetime.ApplicationStarting.ShouldNotBeNull();
        lifetime.ApplicationStopping.ShouldNotBeNull();
        lifetime.ApplicationStarted.ShouldNotBeNull();
    }

    [Fact]
    public async Task NotifyStarting_CallsRegisteredCallbacks()
    {
        var lifetime = new ApplicationLifetime();
        var called = false;
        lifetime.ApplicationStarting.Register(ctx => 
        { 
            called = true; 
            return Task.CompletedTask; 
        });

        await lifetime.NotifyStartingAsync(new ApplicationContext());

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task NotifyStopping_CallsRegisteredCallbacks()
    {
        var lifetime = new ApplicationLifetime();
        var called = false;
        lifetime.ApplicationStopping.Register(ctx => 
        { 
            called = true; 
            return Task.CompletedTask; 
        });

        await lifetime.NotifyStoppingAsync(new ApplicationContext());

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task NotifyStarted_CallsRegisteredCallbacks()
    {
        var lifetime = new ApplicationLifetime();
        var called = false;
        lifetime.ApplicationStarted.Register(ctx => 
        { 
            called = true; 
            return Task.CompletedTask; 
        });

        await lifetime.NotifyStartedAsync(new ApplicationContext());

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task NotifyStarting_PassesContext()
    {
        var lifetime = new ApplicationLifetime();
        ApplicationContext? receivedContext = null;
        lifetime.ApplicationStarting.Register(ctx => 
        { 
            receivedContext = ctx; 
            return Task.CompletedTask; 
        });

        var context = new ApplicationContext { };
        await lifetime.NotifyStartingAsync(context);

        receivedContext.ShouldBe(context);
    }

    [Fact]
    public async Task MultipleCallbacks_AllCalled()
    {
        var lifetime = new ApplicationLifetime();
        var callCount = 0;
        
        lifetime.ApplicationStarting.Register(ctx => 
        { 
            callCount++; 
            return Task.CompletedTask; 
        });
        lifetime.ApplicationStarting.Register(ctx => 
        { 
            callCount++; 
            return Task.CompletedTask; 
        });

        await lifetime.NotifyStartingAsync(new ApplicationContext());

        callCount.ShouldBe(2);
    }
}

public class ApplicationContextTests
{
    [Fact]
    public void Create_WithDefaults_HasEmptyCollections()
    {
        var context = new ApplicationContext();

        context.Services.ShouldBeNull();
        context.Endpoints.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithValues_SetsProperties()
    {
        var endpoints = new List<EndpointInfo> 
        { 
            new EndpointInfo(ProtocolType.Udp, IPAddress.Any, 5060) 
        };
        var context = new ApplicationContext { Endpoints = endpoints };

        context.Endpoints.ShouldBe(endpoints);
    }
}

public class DrongoBuilderTests
{
    [Fact]
    public void Create_HasServices()
    {
        var builder = new DrongoBuilder();

        builder.Services.ShouldNotBeNull();
    }

    [Fact]
    public void Build_ReturnsApplication()
    {
        var builder = new DrongoBuilder();

        var app = builder.Build();

        app.ShouldNotBeNull();
    }

    [Fact]
    public void Build_RegistersRouters()
    {
        var builder = new DrongoBuilder();

        var app = builder.Build() as DrongoApplication;
        app.ShouldNotBeNull();
    }
}

public class DrongoApplicationTests
{
    [Fact]
    public void CreateBuilder_ReturnsBuilder()
    {
        var builder = DrongoApplication.CreateBuilder();

        builder.ShouldNotBeNull();
    }

    [Fact]
    public void MapInvite_AddsHandler()
    {
        var builder = new DrongoBuilder();
        var app = builder.Build();

        app.MapInvite(async ctx => { });

        app.ShouldBeOfType<DrongoApplication>();
    }

    [Fact]
    public void MapRegister_AddsHandler()
    {
        var builder = new DrongoBuilder();
        var app = builder.Build();

        app.MapRegister(async ctx => { });

        app.ShouldBeOfType<DrongoApplication>();
    }

    [Fact]
    public void MapEndpoint_AddsEndpoint()
    {
        var builder = new DrongoBuilder();
        var app = builder.Build();

        app.MapEndpoint(IPAddress.Any, 5060);
        app.MapEndpoint(IPAddress.Any, 5061);

        var endpoints = app.GetEndpoints();
        endpoints.Count.ShouldBe(3);
    }

    [Fact]
    public void GetEndpoints_ReturnsConfiguredEndpoints()
    {
        var builder = new DrongoBuilder();
        var app = builder.Build();

        app.MapEndpoint(IPAddress.Any, 5060);

        var endpoints = app.GetEndpoints();
        endpoints.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AppLifetime_IsAccessible()
    {
        var builder = new DrongoBuilder();
        var app = builder.Build();

        app.AppLifetime.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunAsync_Completes()
    {
        var builder = new DrongoBuilder();
        var app = builder.Build();

        await app.RunAsync();
    }

    [Fact]
    public async Task RunAsync_WithCallbacks_CallsCallbacks()
    {
        var builder = new DrongoBuilder();
        var app = builder.Build();

        var startedCalled = false;
        var stoppingCalled = false;

        await app.RunAsync(
            ctx => { startedCalled = true; return Task.CompletedTask; },
            ctx => { stoppingCalled = true; return Task.CompletedTask; }
        );

        startedCalled.ShouldBeTrue();
        stoppingCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_WithCallbacks_CallsOnStartedAfterStartedAndOnStoppingNotBeforeStarted()
    {
        // Drongo-nwf: onStopping must not fire before the app has started.
        // Correct order: NotifyStarting -> NotifyStarted -> onStarted -> onStopping -> NotifyStoppingAsync
        var builder = new DrongoBuilder();
        var app = builder.Build();

        var callOrder = new List<string>();

        app.AppLifetime.ApplicationStarting.Register(ctx =>
        {
            callOrder.Add("NotifyStarting");
            return Task.CompletedTask;
        });

        app.AppLifetime.ApplicationStarted.Register(ctx =>
        {
            callOrder.Add("NotifyStarted");
            return Task.CompletedTask;
        });

        await app.RunAsync(
            ctx => { callOrder.Add("onStarted"); return Task.CompletedTask; },
            ctx => { callOrder.Add("onStopping"); return Task.CompletedTask; }
        );

        var notifyStartedIndex = callOrder.IndexOf("NotifyStarted");
        var onStartedIndex = callOrder.IndexOf("onStarted");
        var onStoppingIndex = callOrder.IndexOf("onStopping");

        // All three events must have fired
        notifyStartedIndex.ShouldBeGreaterThanOrEqualTo(0);
        onStartedIndex.ShouldBeGreaterThanOrEqualTo(0);
        onStoppingIndex.ShouldBeGreaterThanOrEqualTo(0);

        // onStopping must come after both NotifyStarted and onStarted
        onStoppingIndex.ShouldBeGreaterThan(notifyStartedIndex);
        onStoppingIndex.ShouldBeGreaterThan(onStartedIndex);
    }

    [Fact]
    public async Task RunAsync_WithStoppingCallback_NotifiesStoppingAfterStarted()
    {
        // NotifyStoppingAsync (via ApplicationStopping event) must fire after onStarted.
        var builder = new DrongoBuilder();
        var app = builder.Build();

        var callOrder = new List<string>();

        app.AppLifetime.ApplicationStarted.Register(ctx =>
        {
            callOrder.Add("NotifyStarted");
            return Task.CompletedTask;
        });

        app.AppLifetime.ApplicationStopping.Register(ctx =>
        {
            callOrder.Add("NotifyStopping");
            return Task.CompletedTask;
        });

        Func<ApplicationContext, Task>? noStopping = null;
        await app.RunAsync(
            ctx => { callOrder.Add("onStarted"); return Task.CompletedTask; },
            noStopping
        );

        var notifyStartedIndex = callOrder.IndexOf("NotifyStarted");
        var onStartedIndex = callOrder.IndexOf("onStarted");
        var notifyStoppingIndex = callOrder.IndexOf("NotifyStopping");

        notifyStartedIndex.ShouldBeGreaterThanOrEqualTo(0);
        onStartedIndex.ShouldBeGreaterThanOrEqualTo(0);
        notifyStoppingIndex.ShouldBeGreaterThanOrEqualTo(0);

        // NotifyStopping must come after onStarted
        notifyStoppingIndex.ShouldBeGreaterThan(onStartedIndex);
    }

    [Fact]
    public void Services_AfterBuild_ReturnsNonNullServiceCollection()
    {
        // Drongo-ao0: Services property must not throw; IServiceCollection must be resolvable.
        var builder = new DrongoBuilder();
        var app = builder.Build() as DrongoApplication;

        app.ShouldNotBeNull();
        Should.NotThrow(() =>
        {
            var services = app!.Services;
            services.ShouldNotBeNull();
        });
    }
}

public class RegisterContextTests
{
    private static SipRequest MakeRegisterRequest() =>
        new SipRequest(
            SipMethod.Register,
            SipUri.Parse("sip:example.com"),
            "SIP/2.0",
            new Dictionary<string, string>
            {
                ["Via"] = "SIP/2.0/UDP 192.168.1.1:5060;branch=z9hG4bK776asdhds",
                ["From"] = "<sip:alice@example.com>;tag=1928301774",
                ["To"] = "<sip:alice@example.com>",
                ["Call-ID"] = "a84b4c76e66710@192.168.1.1",
                ["CSeq"] = "314159 REGISTER",
                ["Contact"] = "<sip:alice@192.168.1.1>"
            });

    private static ContactBinding MakeBinding(string user, string host) =>
        new ContactBinding(
            new SipUri("sip", host, user: user),
            DateTimeOffset.MaxValue);

    [Fact]
    public async Task SendResponseAsync_SingleBinding_SetsContactHeader()
    {
        var router = Substitute.For<IRegisterRouter>();
        var registrar = Substitute.For<IRegistrar>();
        var context = new RegisterContext
        {
            Request = MakeRegisterRequest(),
            RemoteEndpoint = new IPEndPoint(IPAddress.Loopback, 5060),
            Router = router,
            Registrar = registrar,
            Bindings = new List<ContactBinding> { MakeBinding("alice", "192.168.1.1") }
        };

        await context.SendResponseAsync(200, "OK");

        context.Response.ShouldNotBeNull();
        context.Response!.Contact.ShouldNotBeNull();
        context.Response.Contact!.ShouldContain("alice@192.168.1.1");
    }

    [Fact]
    public async Task SendResponseAsync_MultipleBindings_AllBindingsIncludedInContactHeader()
    {
        var router = Substitute.For<IRegisterRouter>();
        var registrar = Substitute.For<IRegistrar>();
        var bindings = new List<ContactBinding>
        {
            MakeBinding("alice", "192.168.1.1"),
            MakeBinding("alice", "10.0.0.1"),
            MakeBinding("alice", "172.16.0.1")
        };
        var context = new RegisterContext
        {
            Request = MakeRegisterRequest(),
            RemoteEndpoint = new IPEndPoint(IPAddress.Loopback, 5060),
            Router = router,
            Registrar = registrar,
            Bindings = bindings
        };

        await context.SendResponseAsync(200, "OK");

        context.Response.ShouldNotBeNull();
        var contactHeader = context.Response!.Contact;
        contactHeader.ShouldNotBeNull();
        contactHeader!.ShouldContain("alice@192.168.1.1");
        contactHeader.ShouldContain("alice@10.0.0.1");
        contactHeader.ShouldContain("alice@172.16.0.1");
    }

    [Fact]
    public async Task SendResponseAsync_MultipleBindings_ContactHeaderIsCommaSeparated()
    {
        var router = Substitute.For<IRegisterRouter>();
        var registrar = Substitute.For<IRegistrar>();
        var bindings = new List<ContactBinding>
        {
            MakeBinding("alice", "192.168.1.1"),
            MakeBinding("alice", "10.0.0.1")
        };
        var context = new RegisterContext
        {
            Request = MakeRegisterRequest(),
            RemoteEndpoint = new IPEndPoint(IPAddress.Loopback, 5060),
            Router = router,
            Registrar = registrar,
            Bindings = bindings
        };

        await context.SendResponseAsync(200, "OK");

        context.Response.ShouldNotBeNull();
        var contactHeader = context.Response!.Contact;
        contactHeader.ShouldNotBeNull();
        // RFC 3261: multiple Contact values are comma-separated in one header
        var parts = contactHeader!.Split(',');
        parts.Length.ShouldBe(2);
        parts[0].Trim().ShouldContain("alice@192.168.1.1");
        parts[1].Trim().ShouldContain("alice@10.0.0.1");
    }

    [Fact]
    public async Task SendResponseAsync_NullBindings_OmitsContactHeader()
    {
        var router = Substitute.For<IRegisterRouter>();
        var registrar = Substitute.For<IRegistrar>();
        var context = new RegisterContext
        {
            Request = MakeRegisterRequest(),
            RemoteEndpoint = new IPEndPoint(IPAddress.Loopback, 5060),
            Router = router,
            Registrar = registrar,
            Bindings = null
        };

        await context.SendResponseAsync(200, "OK");

        context.Response.ShouldNotBeNull();
        context.Response!.Contact.ShouldBeNull();
    }
}
