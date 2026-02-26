using Drongo.Core.SIP.Messages;

namespace Drongo.Core.SIP.Dialogs;

/// <summary>
/// Represents one side of a B2BUA dialog (either UAC or UAS leg).
/// Per RFC3261 Section 12, call legs track state, sequence numbers, and routes.
/// </summary>
public interface ICallLeg
{
    /// <summary>Current state of the call leg</summary>
    CallLegState State { get; }

    /// <summary>Call-ID for this leg (shared across both legs of same dialog)</summary>
    string CallId { get; }

    /// <summary>Local tag for this leg (unique identifier for this side)</summary>
    string LocalTag { get; }

    /// <summary>Remote tag for this leg (identifier for the other side)</summary>
    string? RemoteTag { get; }

    /// <summary>Local URI (this endpoint's address)</summary>
    SipUri LocalUri { get; }

    /// <summary>Remote URI (the other endpoint's address)</summary>
    SipUri RemoteUri { get; }

    /// <summary>Is this a secure (TLS) connection?</summary>
    bool IsSecure { get; }

    /// <summary>Local sequence number for requests on this leg</summary>
    long LocalSequenceNumber { get; }

    /// <summary>Remote sequence number tracking responses from peer</summary>
    long RemoteSequenceNumber { get; }

    /// <summary>
    /// Handle an incoming SIP request on this leg.
    /// </summary>
    /// <param name="request">The incoming request</param>
    void HandleRequest(SipRequest request);

    /// <summary>
    /// Handle an incoming SIP response on this leg.
    /// </summary>
    /// <param name="response">The incoming response</param>
    void HandleResponse(SipResponse response);

    /// <summary>
    /// Get the next sequence number for outgoing requests on this leg.
    /// </summary>
    /// <returns>The next sequence number to use</returns>
    long GetNextSequenceNumber();

    /// <summary>
    /// Check if this leg is established (has confirmed dialog state).
    /// </summary>
    /// <returns>True if dialog is confirmed, false otherwise</returns>
    bool IsEstablished();
}
