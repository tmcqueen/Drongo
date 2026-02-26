using System.Net;
using Drongo.Core.SIP.Dialogs;
using Drongo.Core.SIP.Messages;
using Drongo.Core.SIP.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Hosting;

public sealed class DrongoApplication : IDrongoApplication
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DrongoApplication>? _logger;
    private readonly IInviteRouter? _inviteRouter;
    private readonly IRegisterRouter? _registerRouter;
    private readonly IEndpointBuilder _endpointBuilder;
    private readonly ApplicationLifetime _appLifetime;
    private readonly List<Func<InviteContext, Task>> _inviteHandlers;
    private readonly List<Func<RegisterContext, Task>> _registerHandlers;

    public static IDrongoApplicationBuilder CreateBuilder() => new DrongoBuilder();

    public IServiceCollection Services => _serviceProvider.GetRequiredService<IServiceCollection>();
    public IApplicationLifetime AppLifetime => _appLifetime;

    public DrongoApplication(
        IServiceProvider serviceProvider,
        List<Func<InviteContext, Task>> inviteHandlers,
        List<Func<RegisterContext, Task>> registerHandlers,
        IEndpointBuilder endpointBuilder)
    {
        _serviceProvider = serviceProvider;
        _inviteHandlers = inviteHandlers;
        _registerHandlers = registerHandlers;
        _endpointBuilder = endpointBuilder;
        
        _logger = serviceProvider.GetService<ILogger<DrongoApplication>>();
        _inviteRouter = serviceProvider.GetService<IInviteRouter>();
        _registerRouter = serviceProvider.GetService<IRegisterRouter>();
        
        _appLifetime = new ApplicationLifetime();
    }

    public Task RunAsync() => RunAsync(null, null);

    public async Task RunAsync(Func<ApplicationContext, Task>? onStarted, Func<ApplicationContext, Task>? onStopping)
    {
        var context = new ApplicationContext
        {
            Services = _serviceProvider,
            Endpoints = _endpointBuilder.Build()
        };

        await _appLifetime.NotifyStartingAsync(context);
        await _appLifetime.NotifyStartedAsync(context);

        if (onStarted != null)
        {
            await onStarted(context);
        }

        if (onStopping != null)
        {
            await onStopping(context);
        }

        await _appLifetime.NotifyStoppingAsync(context);
    }

    public IEndpointBuilder MapEndpoint(IPAddress address, int port)
    {
        return _endpointBuilder.MapEndpoint(address, port);
    }

    public IReadOnlyList<EndpointInfo> GetEndpoints()
    {
        return _endpointBuilder.Build();
    }

    public IDrongoApplication AddLogging(Action<ILoggingBuilder> configure)
    {
        _logger?.LogDebug("AddLogging called");
        return this;
    }

    public IDrongoApplication MapInvite(Func<InviteContext, Task> handler)
    {
        _inviteHandlers.Add(handler);
        return this;
    }

    public IDrongoApplication MapRegister(Func<RegisterContext, Task> handler)
    {
        _registerHandlers.Add(handler);
        return this;
    }

    public async Task HandleInviteAsync(SipRequest request, IPEndPoint remoteEndpoint)
    {
        if (_inviteRouter != null)
        {
            var context = new InviteContext
            {
                Request = request,
                RemoteEndpoint = remoteEndpoint,
                Router = _inviteRouter
            };

            await _inviteRouter.RouteAsync(context);
        }
    }

    public async Task HandleRegisterAsync(SipRequest request, IPEndPoint remoteEndpoint)
    {
        if (_registerRouter != null)
        {
            var registrar = _serviceProvider.GetService<IRegistrar>();
            
            var context = new RegisterContext
            {
                Request = request,
                RemoteEndpoint = remoteEndpoint,
                Router = _registerRouter,
                Registrar = registrar!
            };

            await _registerRouter.RouteAsync(context);
        }
    }
}
