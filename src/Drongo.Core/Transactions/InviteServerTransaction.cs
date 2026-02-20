using System.Net;
using Drongo.Core.Messages;
using Drongo.Core.Timers;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Transactions;

public sealed class InviteServerTransaction : ServerTransaction
{
    private readonly Action<SipRequest, EndPoint>? _onRequestPassedToTu;
    private TimeSpan _timerGInterval;

    public InviteServerTransaction(
        string id,
        ITimerFactory timerFactory,
        ILogger logger,
        Action<SipRequest, EndPoint>? onRequestPassedToTu = null) 
        : base(id, SipMethod.Invite, timerFactory, logger)
    {
        _onRequestPassedToTu = onRequestPassedToTu;
    }

    public override void Start(SipRequest request, EndPoint remoteEndPoint)
    {
        _request = request;
        _remoteEndPoint = remoteEndPoint;
        _timerGInterval = _timerFactory.T1;
        
        TransitionTo(ServerTransactionState.Proceeding);
        
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
        
        if (response.IsSuccess)
        {
            return SendSuccessResponse(response);
        }
        
        return SendFinalNon2xxResponse(response);
    }

    private Task<SipResponse> SendProvisionalResponse(SipResponse response)
    {
        if (_state != ServerTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring provisional response in non-Proceeding state");
            return Task.FromResult(response);
        }

        LogStateTransition("Sending 1xx response");
        OnResponseSent(response, _remoteEndPoint!);
        
        return Task.FromResult(response);
    }

    private Task<SipResponse> SendSuccessResponse(SipResponse response)
    {
        if (_state != ServerTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring 2xx response in non-Proceeding state");
            return Task.FromResult(response);
        }

        LogStateTransition("Sending 2xx response, transitioning to Terminated");
        OnResponseSent(response, _remoteEndPoint!);
        TransitionTo(ServerTransactionState.Terminated);
        
        return Task.FromResult(response);
    }

    private Task<SipResponse> SendFinalNon2xxResponse(SipResponse response)
    {
        if (_state != ServerTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring 3xx-6xx response in non-Proceeding state");
            return Task.FromResult(response);
        }

        LogStateTransition("Sending 300-699 response, transitioning to Completed");
        OnResponseSent(response, _remoteEndPoint!);
        TransitionTo(ServerTransactionState.Completed);
        
        StartTimerG();
        
        return Task.FromResult(response);
    }

    public override void AckReceived()
    {
        if (_state == ServerTransactionState.Completed)
        {
            LogStateTransition("ACK received, transitioning to Confirmed");
            StopTimer(SipTimerNames.TimerG);
            StartTimerI();
            TransitionTo(ServerTransactionState.Confirmed);
        }
        else
        {
            LogStateTransition("ACK received in non-Completed state");
        }
    }

    public override void RetransmitRequest()
    {
        if (_state == ServerTransactionState.Proceeding)
        {
            LogStateTransition("Retransmit of request received in Proceeding");
        }
    }

    protected override void StartTimerG()
    {
        var timer = CreateTimer(SipTimerNames.TimerG);
        timer.Start(_timerGInterval, OnTimerGFires);
        _logger.LogDebug("Timer G started with interval {Interval}", _timerGInterval);
    }

    private void OnTimerGFires()
    {
        if (_state != ServerTransactionState.Completed)
            return;

        _logger.LogDebug("Timer G fired, retransmitting response");
        OnResponseSent(_lastResponse!, _remoteEndPoint!);
        
        _timerGInterval = _timerGInterval * 2;
        if (_timerGInterval > _timerFactory.T2)
            _timerGInterval = _timerFactory.T2;
        
        var timer = _timers["G"];
        timer.Change(_timerGInterval);
        
        _logger.LogDebug("Timer G interval updated to {Interval}", _timerGInterval);
    }

    protected override void StartTimerH()
    {
        var timer = CreateTimer(SipTimerNames.TimerH);
        timer.Start(_timerFactory.TimerH, OnTimerHFires);
        _logger.LogDebug("Timer H started with interval {Interval}", _timerFactory.TimerH);
    }

    private void OnTimerHFires()
    {
        if (_state != ServerTransactionState.Completed)
            return;

        _logger.LogWarning("Timer H fired - ACK never received, terminating transaction");
        TransitionTo(ServerTransactionState.Terminated);
        FireTimeout();
    }

    protected override void StartTimerI()
    {
        var timer = CreateTimer(SipTimerNames.TimerI);
        timer.Start(_timerFactory.TimerI, OnTimerIFires);
        _logger.LogDebug("Timer I started with interval {Interval}", _timerFactory.TimerI);
    }

    private void OnTimerIFires()
    {
        if (_state != ServerTransactionState.Confirmed)
            return;

        _logger.LogDebug("Timer I fired, transitioning to Terminated");
        TransitionTo(ServerTransactionState.Terminated);
    }

    protected override void StartTimerJ()
    {
        throw new InvalidOperationException("INVITE server transaction does not use Timer J");
    }

    protected override void StopAllTimers()
    {
        StopTimer(SipTimerNames.TimerG);
        StopTimer(SipTimerNames.TimerH);
        StopTimer(SipTimerNames.TimerI);
    }
}
