using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Drongo;
using Drongo.Core.Dialogs;
using Drongo.Core.Messages;
using Drongo.Core.Parsing;
using Drongo.Core.Registration;
using Drongo.Core.Transport;
using Drongo.Hosting;

namespace Drongo;

public sealed class DrongoApplication
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DrongoApplication> _logger;
    private readonly IDialogFactory _dialogFactory;
    private readonly IRegistrar _registrar;
    private readonly IInviteRouter _inviteRouter;
    private readonly IRegisterRouter _registerRouter;

    public static IDrongoBuilder CreateBuilder() => new DrongoBuilder();

    public DrongoApplication(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<DrongoApplication>>();
        _dialogFactory = serviceProvider.GetRequiredService<IDialogFactory>();
        _registrar = serviceProvider.GetRequiredService<IRegistrar>();
        _inviteRouter = serviceProvider.GetRequiredService<IInviteRouter>();
        _registerRouter = serviceProvider.GetRequiredService<IRegisterRouter>();
    }

    public async Task RunAsync(IPEndPoint endpoint)
    {
        _logger.LogInformation("Drongo initialized on {Endpoint}", endpoint);
        await Task.Delay(1);
    }

    public async Task HandleInviteAsync(SipRequest request, IPEndPoint remoteEndpoint)
    {
        var dialog = _dialogFactory.CreateUasDialog(request, remoteEndpoint);
        
        var context = new InviteContext
        {
            Request = request,
            RemoteEndpoint = remoteEndpoint,
            Dialog = dialog,
            Router = _inviteRouter
        };

        await _inviteRouter.RouteAsync(context);
    }

    public async Task HandleRegisterAsync(SipRequest request, IPEndPoint remoteEndpoint)
    {
        var result = await _registrar.RegisterAsync(request);

        var context = new RegisterContext
        {
            Request = request,
            RemoteEndpoint = remoteEndpoint,
            Router = _registerRouter,
            Registrar = _registrar,
            Bindings = result.Bindings
        };

        await context.SendResponseAsync(result.StatusCode, result.ReasonPhrase);
    }
}
