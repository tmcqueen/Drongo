using Drongo.Core.SIP.Messages;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.SIP.Dialogs;

/// <summary>
/// Represents one side (leg) of a B2BUA dialog (either UAC or UAS).
/// Per RFC3261 Section 12, tracks state, sequence numbers, and routes for dialog participants.
/// </summary>
public sealed class CallLeg : ICallLeg
{
    private readonly ILogger<CallLeg> _logger;
    private CallLegState _state;
    private long _localSequenceNumber;
    private long _remoteSequenceNumber;

    public CallLegState State => _state;
    public string CallId { get; }
    public string LocalTag { get; }
    /// <summary>
    /// Remote tag from the peer, established during dialog creation per RFC3261.
    /// Restricted to internal set to prevent external corruption of dialog identity.
    /// </summary>
    public string? RemoteTag { get; internal set; }
    public SipUri LocalUri { get; }
    public SipUri RemoteUri { get; }
    public bool IsSecure { get; }
    public long LocalSequenceNumber => _localSequenceNumber;
    public long RemoteSequenceNumber => _remoteSequenceNumber;

    /// <summary>
    /// Create a new call leg.
    /// </summary>
    /// <param name="callId">The Call-ID for this leg's dialog</param>
    /// <param name="localTag">Local tag for this leg (unique identifier on this side)</param>
    /// <param name="localUri">This endpoint's URI</param>
    /// <param name="remoteUri">The remote endpoint's URI</param>
    /// <param name="isSecure">Whether this is a secure connection</param>
    /// <param name="logger">Logger for diagnostics</param>
    public CallLeg(
        string callId,
        string localTag,
        SipUri localUri,
        SipUri remoteUri,
        bool isSecure,
        ILogger<CallLeg> logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        ArgumentException.ThrowIfNullOrEmpty(localTag);
        ArgumentNullException.ThrowIfNull(localUri);
        ArgumentNullException.ThrowIfNull(remoteUri);
        ArgumentNullException.ThrowIfNull(logger);

        CallId = callId;
        LocalTag = localTag;
        LocalUri = localUri;
        RemoteUri = remoteUri;
        IsSecure = isSecure;
        _logger = logger;
        _state = CallLegState.Initial;
        _localSequenceNumber = 1;
        _remoteSequenceNumber = 0;
    }

    public void HandleRequest(SipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _logger.LogDebug("Handling request {Method} on leg {LocalTag}", request.Method, LocalTag);

        // Per RFC3261 Section 12: INVITE transitions leg from Initial to Inviting
        if (request.Method == SipMethod.Invite)
        {
            if (IsValidTransition(_state, CallLegState.Inviting))
            {
                _state = CallLegState.Inviting;
                _logger.LogDebug("Leg {LocalTag} transitioned to Inviting state on INVITE", LocalTag);
            }
        }
    }

    public void HandleResponse(SipResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        _logger.LogDebug(
            "Handling response {StatusCode} on leg {LocalTag}",
            response.StatusCode,
            LocalTag);

        // Determine target state based on response status code per RFC3261 Section 12
        var targetState = response.StatusCode switch
        {
            >= 100 and < 200 => CallLegState.ProvisionalResponse,
            >= 200 and < 300 => CallLegState.Confirmed,
            >= 300 => CallLegState.Failed,
            _ => _state
        };

        // Only transition if it's a valid forward transition
        if (IsValidTransition(_state, targetState))
        {
            _state = targetState;
        }
        else if (targetState != _state)
        {
            // Log warning for invalid transition (e.g., late provisional after confirmed)
            _logger.LogWarning(
                "Attempted invalid state transition from {CurrentState} to {TargetState} on leg {LocalTag}",
                _state, targetState, LocalTag);
        }
    }

    public long GetNextSequenceNumber()
    {
        return ++_localSequenceNumber;
    }

    public bool IsEstablished()
    {
        return _state == CallLegState.Confirmed;
    }

    /// <summary>
    /// Update the remote sequence number from a received message.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number from the remote peer</param>
    internal void UpdateRemoteSequenceNumber(long sequenceNumber)
    {
        if (sequenceNumber > _remoteSequenceNumber)
        {
            _remoteSequenceNumber = sequenceNumber;
        }
    }

    /// <summary>
    /// Transition the leg to a specific state.
    /// Per RFC3261, only forward state transitions are allowed.
    /// </summary>
    /// <param name="newState">The new state to transition to</param>
    /// <exception cref="InvalidOperationException">Thrown if transition is invalid or backward</exception>
    internal void TransitionToState(CallLegState newState)
    {
        if (!IsValidTransition(_state, newState))
        {
            throw new InvalidOperationException(
                $"Invalid state transition from {_state} to {newState} on leg {LocalTag}");
        }

        _logger.LogDebug("Leg {LocalTag} transitioning from {OldState} to {NewState}",
            LocalTag, _state, newState);
        _state = newState;
    }

    /// <summary>
    /// Validates if a state transition is legal per RFC3261 Section 12.
    /// Prevents backward transitions and invalid state progressions.
    /// </summary>
    /// <param name="currentState">The current state</param>
    /// <param name="targetState">The desired target state</param>
    /// <returns>True if transition is valid, false otherwise</returns>
    private static bool IsValidTransition(CallLegState currentState, CallLegState targetState)
    {
        // Same state is always valid (no-op)
        if (currentState == targetState)
            return true;

        // Valid forward transitions per RFC3261 Section 12
        return (currentState, targetState) switch
        {
            // Initial transitions
            (CallLegState.Initial, CallLegState.Inviting) => true,

            // Early dialog (1xx responses)
            (CallLegState.Initial, CallLegState.ProvisionalResponse) => true,
            (CallLegState.Inviting, CallLegState.ProvisionalResponse) => true,

            // Dialog confirmation (2xx responses)
            (CallLegState.Initial, CallLegState.Confirmed) => true,
            (CallLegState.Inviting, CallLegState.Confirmed) => true,
            (CallLegState.ProvisionalResponse, CallLegState.Confirmed) => true,

            // Dialog failure (3xx-6xx responses)
            (CallLegState.Initial, CallLegState.Failed) => true,
            (CallLegState.Inviting, CallLegState.Failed) => true,
            (CallLegState.ProvisionalResponse, CallLegState.Failed) => true,

            // Dialog termination
            (CallLegState.Confirmed, CallLegState.Terminating) => true,
            (CallLegState.Terminating, CallLegState.Terminated) => true,

            // All other transitions are invalid (including backward transitions)
            _ => false
        };
    }
}
