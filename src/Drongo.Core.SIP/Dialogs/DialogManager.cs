using System.Collections.Concurrent;
using Drongo.Core.SIP.Messages;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.SIP.Dialogs;

/// <summary>
/// Manages SIP dialogs per RFC3261 Section 12.
/// Thread-safe registry of active dialogs identified by Call-ID and tags.
/// </summary>
public sealed class DialogManager : IDialogManager
{
    private readonly ILogger<Dialog> _logger;
    private readonly ConcurrentDictionary<string, IDialog> _dialogs;

    /// <summary>
    /// Gets the count of currently active dialogs in the system.
    /// </summary>
    public long ActiveDialogCount => _dialogs.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogManager"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public DialogManager(ILogger<Dialog> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _dialogs = new ConcurrentDictionary<string, IDialog>();
    }

    /// <summary>
    /// Creates a new dialog with the specified parameters per RFC3261 Section 12.
    /// </summary>
    /// <param name="callId">The Call-ID header value that uniquely identifies the call.</param>
    /// <param name="localTag">The local tag assigned to this dialog.</param>
    /// <param name="localUri">The local user's SIP URI.</param>
    /// <param name="remoteUri">The remote user's SIP URI.</param>
    /// <param name="isSecure">Whether the dialog uses secure transport (TLS).</param>
    /// <returns>The newly created dialog instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when callId or localTag is empty.</exception>
    public IDialog CreateDialog(string callId, string localTag, SipUri localUri, SipUri remoteUri, bool isSecure)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        ArgumentException.ThrowIfNullOrEmpty(localTag);
        ArgumentNullException.ThrowIfNull(localUri);
        ArgumentNullException.ThrowIfNull(remoteUri);

        var dialog = new Dialog(callId, localTag, localUri, remoteUri, isSecure, _logger);
        var key = CreateDialogKey(callId, localTag);

        _dialogs.TryAdd(key, dialog);

        _logger.LogDebug("Created dialog {CallId} with local tag {LocalTag}", callId, localTag);

        return dialog;
    }

    /// <summary>
    /// Attempts to retrieve an existing dialog by Call-ID and tags.
    /// Per RFC3261 Section 12, dialogs are identified by Call-ID + local tag (+ remote tag when specified).
    /// </summary>
    /// <param name="callId">The Call-ID value to search for.</param>
    /// <param name="localTag">The local tag value to search for.</param>
    /// <param name="remoteTag">Optional remote tag to validate; if provided, must match the dialog's remote tag.</param>
    /// <param name="dialog">The dialog if found; otherwise null.</param>
    /// <returns>True if a matching dialog was found; otherwise false.</returns>
    public bool TryGetDialog(string callId, string localTag, string? remoteTag, out IDialog? dialog)
    {
        dialog = null;

        if (string.IsNullOrEmpty(callId) || string.IsNullOrEmpty(localTag))
            return false;

        var key = CreateDialogKey(callId, localTag);

        if (!_dialogs.TryGetValue(key, out var foundDialog))
            return false;

        // If remoteTag is provided, verify it matches the dialog's remote tag
        if (!string.IsNullOrEmpty(remoteTag) && remoteTag != foundDialog.RemoteTag)
            return false;

        dialog = foundDialog;
        return true;
    }

    /// <summary>
    /// Removes a dialog from the manager when it is terminated per RFC3261 Section 12.
    /// </summary>
    /// <param name="callId">The Call-ID of the dialog to remove.</param>
    /// <param name="localTag">The local tag of the dialog to remove.</param>
    /// <param name="remoteTag">Optional remote tag; currently unused but provided for semantic completeness per RFC3261 Section 12.4.</param>
    public void RemoveDialog(string callId, string localTag, string? remoteTag)
    {
        if (string.IsNullOrEmpty(callId) || string.IsNullOrEmpty(localTag))
            return;

        var key = CreateDialogKey(callId, localTag);

        if (_dialogs.TryRemove(key, out var dialog))
        {
            _logger.LogDebug("Removed dialog {CallId} with local tag {LocalTag}", callId, localTag);
        }
    }

    /// <summary>
    /// Creates a unique key for dialog lookup using Call-ID and local tag.
    /// Per RFC3261 Section 12, dialogs are uniquely identified by these two components.
    /// </summary>
    private static string CreateDialogKey(string callId, string localTag)
    {
        return $"{callId}:{localTag}";
    }
}
