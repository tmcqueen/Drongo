using System.Collections.Generic;

namespace Drongo.Core.Prack;

/// <summary>
/// Tracks provisional responses (1xx) per RFC 3262 for reliable delivery.
/// </summary>
public class ProvisionalResponseTracker
{
    private readonly Dictionary<string, DialogProvisionalState> _dialogStates = new();
    private static readonly HashSet<int> ReliableProvisionalCodes = new() { 180, 181, 182, 183, 199 };

    /// <summary>
    /// Tracks a provisional response and returns the RSeq to include.
    /// </summary>
    /// <param name="dialogId">The dialog identifier</param>
    /// <returns>The RSeq sequence number for this response</returns>
    public int TrackProvisionalResponse(string dialogId)
    {
        if (!_dialogStates.TryGetValue(dialogId, out var state))
        {
            state = new DialogProvisionalState();
            _dialogStates[dialogId] = state;
        }

        state.NextRSeq++;
        state.ProvisionalResponses[state.NextRSeq] = false;
        return state.NextRSeq;
    }

    /// <summary>
    /// Acknowledges a provisional response by RSeq.
    /// </summary>
    /// <param name="dialogId">The dialog identifier</param>
    /// <param name="rseq">The RSeq being acknowledged</param>
    /// <returns>True if successfully acknowledged, false if invalid or already acknowledged</returns>
    public bool AcknowledgeProvisionalResponse(string dialogId, int rseq)
    {
        if (!_dialogStates.TryGetValue(dialogId, out var state))
        {
            return false;
        }

        if (!state.ProvisionalResponses.ContainsKey(rseq))
        {
            return false;
        }

        if (state.ProvisionalResponses[rseq])
        {
            return false;
        }

        state.ProvisionalResponses[rseq] = true;
        return true;
    }

    /// <summary>
    /// Determines if a provisional response code requires reliable delivery per RFC 3262.
    /// </summary>
    /// <param name="statusCode">The SIP status code</param>
    /// <returns>True if PRACK is required</returns>
    public bool IsProvisionalResponseReliable(int statusCode)
    {
        return ReliableProvisionalCodes.Contains(statusCode);
    }

    /// <summary>
    /// Gets all unacknowledged provisional responses for a dialog.
    /// </summary>
    /// <param name="dialogId">The dialog identifier</param>
    /// <returns>List of unacknowledged RSeq numbers</returns>
    public List<int> GetUnacknowledgedResponses(string dialogId)
    {
        var result = new List<int>();
        
        if (!_dialogStates.TryGetValue(dialogId, out var state))
        {
            return result;
        }

        foreach (var kvp in state.ProvisionalResponses)
        {
            if (!kvp.Value)
            {
                result.Add(kvp.Key);
            }
        }

        return result;
    }

    /// <summary>
    /// Clears all tracking for a dialog.
    /// </summary>
    /// <param name="dialogId">The dialog identifier</param>
    public void ClearDialog(string dialogId)
    {
        _dialogStates.Remove(dialogId);
    }

    private class DialogProvisionalState
    {
        public int NextRSeq { get; set; }
        public Dictionary<int, bool> ProvisionalResponses { get; } = new();
    }
}
