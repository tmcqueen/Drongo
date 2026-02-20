using System.Net;
using Drongo.Core.Messages;
using Drongo.Core.Timers;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Transactions;

public sealed class NonInviteServerTransaction : ServerTransaction
{
    private readonly Action<SipRequest, EndPoint>? _onRequestPassedToTu;

    public NonInviteServerTransaction(
        string id,
        SipMethod method,
        ITimerFactory timerFactory,
        ILogger logger,
        Action<SipRequest, EndPoint>? onRequestPassedToTu = null) 
        : base(id, method, timerFactory, logger)
    {
        _onRequestPassedToTu = onRequestPassedToTu;
    }

    public override void Start(SipRequest request, EndPoint remoteEndPoint)
    {
        _request = request;
        _remoteEndPoint = remoteEndPoint;
        
        TransitionTo(ServerTransactionState.Trying);
        
        LogStateTransition("Request received");
        
        _onRequestPassedToTu?.Invoke(request, remoteEndPoint);
    }

    public override Task<SipResponse> SendResponseAsync(SipResponse response, CancellationToken ct = default)
    {
        _lastResponse = response;
        
        if (_state == ServerTransactionState.Terminated)
        {
            _logger.LogWarning(
                "Attempted to send response {StatusCode} in Terminated state",
                response.StatusCode);
            return Task.FromResult(response);
        }

        if (response.IsProvisional)
        {
            return SendProvisionalResponse(response);
        }
        
        return SendFinalResponse(response);
    }

    private Task<SipResponse> SendProvisionalResponse(SipResponse response)
    {
        if (_state != ServerTransactionState.Trying && _state != ServerTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring 1xx response in non-Trying/Proceeding state");
            return Task.FromResult(response);
        }

        LogStateTransition("Sending 1xx response, transitioning to Proceeding");
        OnResponseSent(response, _remoteEndPoint!);
        TransitionTo(ServerTransactionState.Proceeding);
        
        return Task.FromResult(response);
    }

    private Task<SipResponse> SendFinalResponse(SipResponse response)
    {
        if (_state != ServerTransactionState.Trying && _state != ServerTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring final response in non-Trying/Proceeding state");
            return Task.FromResult(response);
        }

        LogStateTransition("Sending final response, transitioning to Completed");
        OnResponseSent(response, _remoteEndPoint!);
        TransitionTo(ServerTransactionState.Completed);
        
        StartTimerJ();
        
        return Task.FromResult(response);
    }

    public override void AckReceived()
    {
        LogStateTransition("ACK received - no action for Non-INVITE transaction");
    }

    public override void RetransmitRequest()
    {
        switch (_state)
        {
            case ServerTransactionState.Trying:
                LogStateTransition("Retransmit received in Trying - discarded");
                break;
                
            case ServerTransactionState.Proceeding:
                LogStateTransition("Retransmit received in Proceeding - retransmit last provisional");
                OnResponseSent(_lastResponse!, _remoteEndPoint!);
                break;
                
            case ServerTransactionState.Completed:
                LogStateTransition("Retransmit received in Completed - retransmit final response");
                OnResponseSent(_lastResponse!, _remoteEndPoint!);
                break;
                
            default:
                LogStateTransition($"Retransmit received in {_state} - ignored");
                break;
        }
    }

    protected override void StartTimerG()
    {
        throw new InvalidOperationException("Non-INVITE server transaction does not use Timer G");
    }

    protected override void StartTimerH()
    {
        throw new InvalidOperationException("Non-INVITE server transaction does not use Timer H");
    }

    protected override void StartTimerI()
    {
        throw new InvalidOperationException("Non-INVITE server transaction does not use Timer I");
    }

    protected override void StartTimerJ()
    {
        var timer = CreateTimer(SipTimerNames.TimerJ);
        timer.Start(_timerFactory.TimerJ, OnTimerJFires);
        _logger.LogDebug("Timer J started with interval {Interval}", _timerFactory.TimerJ);
    }

    private void OnTimerJFires()
    {
        if (_state != ServerTransactionState.Completed)
            return;

        _logger.LogDebug("Timer J fired, transitioning to Terminated");
        TransitionTo(ServerTransactionState.Terminated);
    }

    protected override void StopAllTimers()
    {
        StopTimer(SipTimerNames.TimerJ);
    }
}
