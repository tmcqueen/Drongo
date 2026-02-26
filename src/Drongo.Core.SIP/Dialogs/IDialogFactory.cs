using System.Net;
using Drongo.Core.SIP.Messages;

namespace Drongo.Core.SIP.Dialogs;

public interface IDialogFactory
{
    IDialog CreateUasDialog(SipRequest request, IPEndPoint remoteEndpoint);
    IDialog CreateUacDialog(SipRequest request, SipResponse response, IPEndPoint remoteEndpoint);
}
