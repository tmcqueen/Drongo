using System.Collections.Generic;
using Drongo.Core.Messages;

namespace Drongo.Core.Prack;

/// <summary>
/// Provides PRACK functionality per RFC 3262.
/// Combines provisional response tracking and PRACK request generation.
/// </summary>
public class PrackProvider
{
    private readonly ProvisionalResponseTracker _tracker = new();
    private readonly PrackRequestGenerator _generator = new();
    private readonly Dictionary<string, DialogPrackState> _dialogStates = new();

    /// <summary>
    /// Tracks a provisional response and returns the RSeq to include.
    /// Returns 0 if the response doesn't require PRACK (e.g., 100 Trying).
    /// </summary>
    public int TrackProvisionalResponse(string dialogId, int statusCode)
    {
        if (!_tracker.IsProvisionalResponseReliable(statusCode))
        {
            return 0;
        }

        var rseq = _tracker.TrackProvisionalResponse(dialogId);

        if (!_dialogStates.TryGetValue(dialogId, out var state))
        {
            state = new DialogPrackState();
            _dialogStates[dialogId] = state;
        }

        state.LastProvisionalMethod = "INVITE";
        state.LastProvisionalCSeq = state.NextCSeq;
        state.NextCSeq++;

        return rseq;
    }

    /// <summary>
    /// Acknowledges a provisional response by RSeq.
    /// </summary>
    public bool AcknowledgeProvisionalResponse(string dialogId, int rseq)
    {
        return _tracker.AcknowledgeProvisionalResponse(dialogId, rseq);
    }

    /// <summary>
    /// Generates a PRACK request for a dialog.
    /// </summary>
    public SipRequest GeneratePrackRequest(
        string dialogId,
        SipUri targetUri,
        SipUri localUri,
        SipUri remoteUri,
        Dictionary<string, string>? routeHeaders = null)
    {
        if (!_dialogStates.TryGetValue(dialogId, out var state))
        {
            throw new InvalidOperationException($"No provisional response tracked for dialog {dialogId}");
        }

        return _generator.GeneratePrackRequest(
            dialogId: dialogId,
            rseq: 1,
            method: state.LastProvisionalMethod ?? "INVITE",
            cseq: state.LastProvisionalCSeq,
            targetUri: targetUri,
            localUri: localUri,
            remoteUri: remoteUri,
            routeHeaders: routeHeaders ?? new Dictionary<string, string>()
        );
    }

    /// <summary>
    /// Clears PRACK state for a dialog.
    /// </summary>
    public void ClearDialog(string dialogId)
    {
        _tracker.ClearDialog(dialogId);
        _dialogStates.Remove(dialogId);
    }

    private class DialogPrackState
    {
        public string? LastProvisionalMethod { get; set; }
        public int LastProvisionalCSeq { get; set; }
        public int NextCSeq { get; set; } = 100;
    }
}
