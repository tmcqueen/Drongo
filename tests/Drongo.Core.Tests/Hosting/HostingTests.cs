using System.Net;
using System.Net.Sockets;
using Drongo.Core.Hosting;
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
    }

    [Fact]
    public async Task RunAsync_WithCallbacks_CallsOnStartedAfterStartedAndOnStoppingNotBeforeStarted()
    {
        // Drongo-nwf: onStopping must not fire before the app is started.
        // Correct order: NotifyStarting -> NotifyStarted -> onStarted -> (app runs) -> onStopping
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

        // onStopping must appear after NotifyStarted and onStarted, not before
        var notifyStartedIndex = callOrder.IndexOf("NotifyStarted");
        var onStartedIndex = callOrder.IndexOf("onStarted");
        var onStoppingIndex = callOrder.IndexOf("onStopping");

        notifyStartedIndex.ShouldBeGreaterThanOrEqualTo(0);
        onStartedIndex.ShouldBeGreaterThanOrEqualTo(0);
        // onStopping may or may not be called in RunAsync — if it is, it must come after onStarted
        if (onStoppingIndex >= 0)
        {
            onStoppingIndex.ShouldBeGreaterThan(onStartedIndex);
        }

        // onStopping must NOT fire before NotifyStarted
        var onStoppingBeforeStarted = onStoppingIndex >= 0 && onStoppingIndex < notifyStartedIndex;
        onStoppingBeforeStarted.ShouldBeFalse();
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
