using Drongo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Dialogs;

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
    public string? RemoteTag { get; set; }
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
    }

    public void HandleResponse(SipResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        _logger.LogDebug(
            "Handling response {StatusCode} on leg {LocalTag}",
            response.StatusCode,
            LocalTag);

        // Update state based on response status code per RFC3261 Section 12
        _state = response.StatusCode switch
        {
            >= 100 and < 200 => CallLegState.ProvisionalResponse,
            >= 200 and < 300 => CallLegState.Confirmed,
            >= 300 => CallLegState.Failed,
            _ => _state
        };
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
    /// </summary>
    /// <param name="newState">The new state to transition to</param>
    internal void TransitionToState(CallLegState newState)
    {
        _logger.LogDebug("Leg {LocalTag} transitioning from {OldState} to {NewState}",
            LocalTag, _state, newState);
        _state = newState;
    }
}
