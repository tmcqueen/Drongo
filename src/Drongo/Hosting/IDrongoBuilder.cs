using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drongo.Hosting;

public interface IDrongoBuilder
{
    IServiceCollection Services { get; }
    IDrongoBuilder AddLogging(Action<ILoggingBuilder> configure);
    IDrongoBuilder MapInvite(Func<InviteContext, Task> handler);
    IDrongoBuilder MapRegister(Func<RegisterContext, Task> handler);
}

public sealed class DrongoBuilder : IDrongoBuilder
{
    private readonly List<Func<InviteContext, Task>> _inviteHandlers = new();
    private readonly List<Func<RegisterContext, Task>> _registerHandlers = new();

    public IServiceCollection Services { get; }

    public DrongoBuilder()
    {
        Services = new ServiceCollection();
    }

    public IDrongoBuilder AddLogging(Action<ILoggingBuilder> configure)
    {
        Services.AddLogging(configure);
        return this;
    }

    public IDrongoBuilder MapInvite(Func<InviteContext, Task> handler)
    {
        _inviteHandlers.Add(handler);
        Services.AddSingleton<IInviteRouter>(new InviteRouter(_inviteHandlers));
        return this;
    }

    public IDrongoBuilder MapRegister(Func<RegisterContext, Task> handler)
    {
        _registerHandlers.Add(handler);
        Services.AddSingleton<IRegisterRouter>(new RegisterRouter(_registerHandlers));
        return this;
    }
}

public interface IInviteRouter
{
    Task RouteAsync(InviteContext context);
}

public sealed class InviteRouter : IInviteRouter
{
    private readonly List<Func<InviteContext, Task>> _handlers;

    public InviteRouter(List<Func<InviteContext, Task>> handlers)
    {
        _handlers = handlers;
    }

    public async Task RouteAsync(InviteContext context)
    {
        foreach (var handler in _handlers)
        {
            await handler(context);
        }
    }
}

public interface IRegisterRouter
{
    Task RouteAsync(RegisterContext context);
}

public sealed class RegisterRouter : IRegisterRouter
{
    private readonly List<Func<RegisterContext, Task>> _handlers;

    public RegisterRouter(List<Func<RegisterContext, Task>> handlers)
    {
        _handlers = handlers;
    }

    public async Task RouteAsync(RegisterContext context)
    {
        foreach (var handler in _handlers)
        {
            await handler(context);
        }
    }
}
