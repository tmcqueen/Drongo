using Drongo.Core.SIP.Messages;

namespace Drongo.Core.SIP.Dialogs;

public interface IDialogManager
{
    /// <summary>
    /// Creates a new dialog with the specified parameters.
    /// Per RFC3261 Section 12, a dialog is identified by Call-ID and both tags (local and remote).
    /// </summary>
    IDialog CreateDialog(string callId, string localTag, SipUri localUri, SipUri remoteUri, bool isSecure);

    /// <summary>
    /// Attempts to retrieve an existing dialog by Call-ID and tags.
    /// Implements RFC3261 Section 12 tag matching: CallID + (localTag, remoteTag).
    /// </summary>
    bool TryGetDialog(string callId, string localTag, string? remoteTag, out IDialog? dialog);

    /// <summary>
    /// Removes a dialog from the manager.
    /// Called when dialog is terminated per RFC3261 Section 12.
    /// </summary>
    void RemoveDialog(string callId, string localTag, string? remoteTag);

    /// <summary>
    /// Gets the current count of active dialogs.
    /// </summary>
    long ActiveDialogCount { get; }
}
