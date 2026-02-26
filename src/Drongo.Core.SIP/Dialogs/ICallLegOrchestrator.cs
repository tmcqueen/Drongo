using Drongo.Core.SIP.Messages;

namespace Drongo.Core.SIP.Dialogs;

/// <summary>
/// Orchestrates message routing between UAC and UAS call legs in a B2BUA dialog.
/// Per RFC3261 Section 12, coordinates provisional (1xx) and final (2xx-6xx) responses
/// between the two legs of a back-to-back user agent call.
/// </summary>
public interface ICallLegOrchestrator
{
    /// <summary>
    /// Create or get a new call leg pair (UAC and UAS) for a dialog.
    /// </summary>
    /// <param name="callId">The Call-ID for the dialog</param>
    /// <param name="uacTag">Local tag for UAC leg</param>
    /// <param name="uasTag">Local tag for UAS leg</param>
    /// <param name="uacUri">URI for UAC endpoint</param>
    /// <param name="uasUri">URI for UAS endpoint</param>
    /// <param name="isSecure">Whether this is a secure connection</param>
    /// <returns>A tuple of (UAC leg, UAS leg)</returns>
    (ICallLeg UacLeg, ICallLeg UasLeg) CreateCallLegPair(
        string callId,
        string uacTag,
        string uasTag,
        SipUri uacUri,
        SipUri uasUri,
        bool isSecure);

    /// <summary>
    /// Route a provisional response (1xx) from UAS leg back to UAC leg.
    /// Per RFC3261 Section 12.1.1, provisional responses are forwarded immediately.
    /// </summary>
    /// <param name="callId">The Call-ID of the dialog</param>
    /// <param name="response">The 1xx response from UAS</param>
    /// <returns>The modified response to send to UAC, or null if should not be forwarded</returns>
    SipResponse? RouteProvisionalResponse(string callId, SipResponse response);

    /// <summary>
    /// Route a final response (2xx) from UAS leg back to UAC leg.
    /// Per RFC3261 Section 12.1.1, confirms the dialog and must include appropriate tags.
    /// </summary>
    /// <param name="callId">The Call-ID of the dialog</param>
    /// <param name="response">The 2xx response from UAS</param>
    /// <returns>The modified response to send to UAC, or null if should not be forwarded</returns>
    SipResponse? RouteFinalResponse(string callId, SipResponse response);

    /// <summary>
    /// Route an error response (3xx-6xx) from UAS leg back to UAC leg.
    /// Per RFC3261 Section 12.1.1, failures are forwarded and dialog remains unconsumed.
    /// </summary>
    /// <param name="callId">The Call-ID of the dialog</param>
    /// <param name="response">The error response (3xx-6xx) from UAS</param>
    /// <returns>The modified response to send to UAC, or null if should not be forwarded</returns>
    SipResponse? RouteErrorResponse(string callId, SipResponse response);

    /// <summary>
    /// Get the call legs for a dialog.
    /// </summary>
    /// <param name="callId">The Call-ID of the dialog</param>
    /// <param name="uacLeg">Output parameter for UAC leg</param>
    /// <param name="uasLeg">Output parameter for UAS leg</param>
    /// <returns>True if legs found, false otherwise</returns>
    bool TryGetCallLegs(string callId, out ICallLeg? uacLeg, out ICallLeg? uasLeg);

    /// <summary>
    /// Check if a dialog has both legs in confirmed state.
    /// </summary>
    /// <param name="callId">The Call-ID of the dialog</param>
    /// <returns>True if both legs are confirmed, false otherwise</returns>
    bool IsDialogConfirmed(string callId);

    /// <summary>
    /// Route an in-dialog request (ACK, BYE, re-INVITE, etc.).
    /// Per RFC3261 Section 12.2, in-dialog requests must match the dialog by tags and Call-ID.
    /// ACK requests are NOT forwarded to the other leg; BYE and re-INVITE may be forwarded.
    /// </summary>
    /// <param name="callId">The Call-ID of the dialog</param>
    /// <param name="request">The in-dialog request</param>
    /// <returns>The modified request to forward to other leg, or null if should not be forwarded (e.g., ACK)</returns>
    SipRequest? RouteInDialogRequest(string callId, SipRequest request);

    /// <summary>
    /// Get the total number of active call leg pairs.
    /// </summary>
    long ActiveDialogCount { get; }
}
