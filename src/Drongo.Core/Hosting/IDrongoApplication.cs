using System.Net;
using Drongo.Core.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Hosting;

public interface IDrongoApplication
{
    IServiceCollection Services { get; }
    IApplicationLifetime AppLifetime { get; }
    
    Task RunAsync();
    Task RunAsync(Func<ApplicationContext, Task> onStarted, Func<ApplicationContext, Task> onStopping);
    
    IEndpointBuilder MapEndpoint(IPAddress address, int port);
    IReadOnlyList<EndpointInfo> GetEndpoints();
    
    IDrongoApplication AddLogging(Action<ILoggingBuilder> configure);
    IDrongoApplication MapInvite(Func<InviteContext, Task> handler);
    IDrongoApplication MapRegister(Func<RegisterContext, Task> handler);
    
    Task HandleInviteAsync(SipRequest request, IPEndPoint remoteEndpoint);
    Task HandleRegisterAsync(SipRequest request, IPEndPoint remoteEndpoint);
}

public interface IDrongoApplicationBuilder
{
    IServiceCollection Services { get; }
    IDrongoApplication Build();
}
