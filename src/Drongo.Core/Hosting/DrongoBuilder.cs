using Microsoft.Extensions.DependencyInjection;

namespace Drongo.Core.Hosting;

public sealed class DrongoBuilder : IDrongoApplicationBuilder
{
    private readonly List<Func<InviteContext, Task>> _inviteHandlers = new();
    private readonly List<Func<RegisterContext, Task>> _registerHandlers = new();
    private readonly IEndpointBuilder _endpointBuilder;

    public IServiceCollection Services { get; } = new ServiceCollection();

    public DrongoBuilder()
    {
        _endpointBuilder = new EndpointBuilder(System.Net.IPAddress.Any, 5060);
    }

    public IDrongoApplication Build()
    {
        Services.AddSingleton(_inviteHandlers);
        Services.AddSingleton(_registerHandlers);
        Services.AddSingleton<IInviteRouter>(sp => new InviteRouter(sp.GetRequiredService<List<Func<InviteContext, Task>>>()));
        Services.AddSingleton<IRegisterRouter>(sp => new RegisterRouter(sp.GetRequiredService<List<Func<RegisterContext, Task>>>()));
        Services.AddSingleton<IApplicationLifetime, ApplicationLifetime>();

        return new DrongoApplication(
            Services.BuildServiceProvider(),
            _inviteHandlers,
            _registerHandlers,
            _endpointBuilder);
    }
}
