using System.Net;
using Drongo.Core.Messages;

namespace Drongo.Core.Dialogs;

public interface IDialogFactory
{
    IDialog CreateUasDialog(SipRequest request, IPEndPoint remoteEndpoint);
    IDialog CreateUacDialog(SipRequest request, SipResponse response, IPEndPoint remoteEndpoint);
}
