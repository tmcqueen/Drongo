using Drongo.Core.SIP.Messages;

namespace Drongo.Core.SIP.Dialogs;

public interface IDialog
{
    string CallId { get; }
    string LocalTag { get; }
    string? RemoteTag { get; }
    DialogState State { get; }
    SipUri LocalUri { get; }
    SipUri RemoteUri { get; }
    SipUri? RemoteTarget { get; }
    int LocalSequenceNumber { get; }
    int RemoteSequenceNumber { get; }
    IReadOnlyList<SipUri> RouteSet { get; }
    bool IsSecure { get; }

    void HandleUacResponse(SipResponse response);
    void HandleUasRequest(SipRequest request);
    void Terminate();
}
