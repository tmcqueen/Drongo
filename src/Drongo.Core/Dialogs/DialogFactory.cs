using System.Net;
using Drongo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Dialogs;

public sealed class DialogFactory : IDialogFactory
{
    private readonly ILogger<Dialog> _logger;

    public DialogFactory(ILogger<Dialog> logger)
    {
        _logger = logger;
    }

    public IDialog CreateUasDialog(SipRequest request, IPEndPoint remoteEndpoint)
    {
        var callId = request.CallId;
        var localTag = GenerateTag();
        var remoteTag = Dialog.ExtractTag(request.From);

        var localUri = SipUri.Parse(request.To);
        var remoteUri = SipUri.Parse(request.From);

        var isSecure = request.RequestUri.IsSecure;

        var dialog = new Dialog(callId, localTag, localUri, remoteUri, isSecure, _logger);

        dialog.HandleUasRequest(request);

        if (remoteEndpoint != null)
        {
            dialog.HandleUacResponse(SipResponse.CreateTrying(request.Headers));
        }

        return dialog;
    }

    public IDialog CreateUacDialog(SipRequest request, SipResponse response, IPEndPoint remoteEndpoint)
    {
        var callId = request.CallId;
        var localTag = Dialog.ExtractTag(request.From);
        var remoteTag = Dialog.ExtractTag(response.To);

        var localUri = SipUri.Parse(request.From);
        var remoteUri = SipUri.Parse(request.To);

        var isSecure = request.RequestUri.IsSecure;

        var dialog = new Dialog(callId, localTag, localUri, remoteUri, isSecure, _logger);

        dialog.HandleUacResponse(response);

        return dialog;
    }

    private static string GenerateTag()
    {
        return Guid.NewGuid().ToString("N")[..16];
    }
}
