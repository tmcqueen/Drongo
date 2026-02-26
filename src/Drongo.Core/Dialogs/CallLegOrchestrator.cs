using System.Collections.Concurrent;
using Drongo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Dialogs;

/// <summary>
/// Orchestrates routing of SIP messages between UAC and UAS call legs in B2BUA dialogs.
/// Per RFC3261 Section 12, manages provisional and final responses between dialog legs.
/// </summary>
public sealed class CallLegOrchestrator : ICallLegOrchestrator
{
    private readonly ILogger<CallLeg> _logger;
    private readonly ConcurrentDictionary<string, (CallLeg UacLeg, CallLeg UasLeg)> _dialogLegs;

    public long ActiveDialogCount => _dialogLegs.Count;

    public CallLegOrchestrator(ILogger<CallLeg> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _dialogLegs = new ConcurrentDictionary<string, (CallLeg, CallLeg)>();
    }

    public (ICallLeg UacLeg, ICallLeg UasLeg) CreateCallLegPair(
        string callId,
        string uacTag,
        string uasTag,
        SipUri uacUri,
        SipUri uasUri,
        bool isSecure)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        ArgumentException.ThrowIfNullOrEmpty(uacTag);
        ArgumentException.ThrowIfNullOrEmpty(uasTag);
        ArgumentNullException.ThrowIfNull(uacUri);
        ArgumentNullException.ThrowIfNull(uasUri);

        // Create UAC leg (caller side)
        var uacLeg = new CallLeg(callId, uacTag, uacUri, uasUri, isSecure, _logger)
        {
            RemoteTag = uasTag
        };

        // Create UAS leg (called party side)
        var uasLeg = new CallLeg(callId, uasTag, uasUri, uacUri, isSecure, _logger)
        {
            RemoteTag = uacTag
        };

        if (!_dialogLegs.TryAdd(callId, (uacLeg, uasLeg)))
        {
            throw new InvalidOperationException(
                $"Call leg pair for Call-ID '{callId}' already exists");
        }

        _logger.LogDebug(
            "Created call leg pair for dialog {CallId}: UAC={UacTag}, UAS={UasTag}",
            callId, uacTag, uasTag);

        return (uacLeg, uasLeg);
    }

    public SipResponse? RouteProvisionalResponse(string callId, SipResponse response)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        ArgumentNullException.ThrowIfNull(response);

        if (!_dialogLegs.TryGetValue(callId, out var legs))
        {
            _logger.LogWarning("Dialog {CallId} not found for provisional response routing", callId);
            return null;
        }

        var (uacLeg, uasLeg) = legs;

        // Provisional responses (1xx) are forwarded from UAS back to UAC
        _logger.LogDebug(
            "Routing provisional response {StatusCode} from UAS to UAC for dialog {CallId}",
            response.StatusCode, callId);

        // Update both legs symmetrically per RFC3261 Section 12.1
        uasLeg.HandleResponse(response);
        uacLeg.HandleResponse(response);

        // Return response to be sent to UAC
        return response;
    }

    public SipResponse? RouteFinalResponse(string callId, SipResponse response)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        ArgumentNullException.ThrowIfNull(response);

        if (!_dialogLegs.TryGetValue(callId, out var legs))
        {
            _logger.LogWarning("Dialog {CallId} not found for final response routing", callId);
            return null;
        }

        var (uacLeg, uasLeg) = legs;

        // Final response (2xx) confirms the dialog per RFC3261 Section 12.1.1
        // OR acknowledges BYE request terminating the dialog per RFC3261 Section 12.2.1.3
        if (response.StatusCode is >= 200 and < 300)
        {
            // Check if both legs are in Terminating state (response to BYE)
            if (uacLeg.State == CallLegState.Terminating && uasLeg.State == CallLegState.Terminating)
            {
                _logger.LogInformation(
                    "BYE acknowledged for dialog {CallId}; transitioning both legs to Terminated",
                    callId);

                // Transition both legs to Terminated state per RFC3261 Section 12.2.1.3
                uacLeg.TransitionToState(CallLegState.Terminated);
                uasLeg.TransitionToState(CallLegState.Terminated);
            }
            else
            {
                _logger.LogInformation(
                    "Dialog {CallId} confirmed with {StatusCode} response",
                    callId, response.StatusCode);

                // Update both legs symmetrically using HandleResponse per RFC3261 Section 12.1
                uasLeg.HandleResponse(response);
                uacLeg.HandleResponse(response);
            }
        }

        return response;
    }

    public SipResponse? RouteErrorResponse(string callId, SipResponse response)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        ArgumentNullException.ThrowIfNull(response);

        if (!_dialogLegs.TryGetValue(callId, out var legs))
        {
            _logger.LogWarning("Dialog {CallId} not found for error response routing", callId);
            return null;
        }

        var (uacLeg, uasLeg) = legs;

        // Error responses (3xx-6xx) are forwarded but don't confirm dialog
        _logger.LogDebug(
            "Routing error response {StatusCode} from UAS to UAC for dialog {CallId}",
            response.StatusCode, callId);

        // Update both legs symmetrically per RFC3261 Section 12.1
        uasLeg.HandleResponse(response);
        uacLeg.HandleResponse(response);

        return response;
    }

    public bool TryGetCallLegs(string callId, out ICallLeg? uacLeg, out ICallLeg? uasLeg)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);

        uacLeg = null;
        uasLeg = null;

        if (_dialogLegs.TryGetValue(callId, out var legs))
        {
            uacLeg = legs.UacLeg;
            uasLeg = legs.UasLeg;
            return true;
        }

        return false;
    }

    public bool IsDialogConfirmed(string callId)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);

        if (!_dialogLegs.TryGetValue(callId, out var legs))
        {
            return false;
        }

        var (uacLeg, uasLeg) = legs;

        // Dialog is confirmed when both legs are in confirmed state
        return uacLeg.IsEstablished() && uasLeg.IsEstablished();
    }

    public SipRequest? RouteInDialogRequest(string callId, SipRequest request)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        ArgumentNullException.ThrowIfNull(request);

        if (!_dialogLegs.TryGetValue(callId, out var legs))
        {
            _logger.LogWarning("Dialog {CallId} not found for in-dialog request routing", callId);
            return null;
        }

        // Per RFC3261 Section 12.2:
        // ACK requests are not forwarded to the other leg; they acknowledge the 2xx response
        if (request.Method == SipMethod.Ack)
        {
            _logger.LogDebug(
                "ACK request for dialog {CallId} acknowledged (not forwarded to other leg)",
                callId);
            return null;  // ACK is consumed, not forwarded
        }

        // Per RFC3261 Section 12.2:
        // BYE requests terminate the dialog and are forwarded to the other leg
        if (request.Method == SipMethod.Bye)
        {
            var (uacLeg, uasLeg) = legs;
            
            // Transition both legs to Terminating state per RFC3261 Section 12.2.1.2
            uacLeg.TransitionToState(CallLegState.Terminating);
            uasLeg.TransitionToState(CallLegState.Terminating);
            
            _logger.LogDebug(
                "BYE request for dialog {CallId} forwarded to other leg; both legs transitioning to Terminating",
                callId);
            return request;  // BYE is forwarded to other leg
        }

        // Per RFC3261 Section 14:
        // re-INVITE requests allow mid-dialog offer/answer and are forwarded to the other leg
        if (request.Method == SipMethod.Invite)
        {
            _logger.LogDebug(
                "re-INVITE request for dialog {CallId} forwarded to other leg",
                callId);
            return request;  // re-INVITE is forwarded to other leg
        }

        // Other in-dialog requests (to be implemented)
        _logger.LogDebug(
            "In-dialog {Method} request for dialog {CallId} not yet handled",
            request.Method, callId);

        return null;  // Placeholder: not forwarding other methods yet
    }
}
