using System.Net;
using Drongo.Core.Messages;
using Drongo.Core.Timers;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Transactions;

public sealed class NonInviteClientTransaction : ClientTransaction
{
    private SipRequest? _request;
    private TimeSpan _timerEInterval;
    private CancellationTokenSource? _cts;

    public NonInviteClientTransaction(
        string id,
        SipMethod method,
        ITimerFactory timerFactory,
        ILogger logger) 
        : base(id, method, timerFactory, logger)
    {
    }

    public override async Task StartAsync(SipRequest request, EndPoint remoteEndPoint, CancellationToken ct = default)
    {
        _request = request;
        _remoteEndPoint = remoteEndPoint;
        _timerEInterval = _timerFactory.T1;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        
        TransitionTo(ClientTransactionState.Trying);
        
        LogStateTransition("Starting Non-INVITE transaction");
        
        OnRequestSent(request, remoteEndPoint);
        
        StartTimerF();
    }

    public override void ReceiveResponse(SipResponse response)
    {
        if (_state == ClientTransactionState.Terminated)
        {
            LogStateTransition("Ignoring response in Terminated state");
            return;
        }

        OnResponseReceived(response);

        if (response.IsProvisional)
        {
            HandleProvisionalResponse(response);
        }
        else
        {
            HandleFinalResponse(response);
        }
    }

    private void HandleProvisionalResponse(SipResponse response)
    {
        if (_state != ClientTransactionState.Trying && _state != ClientTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring 1xx response in non-Trying/Proceeding state");
            return;
        }

        LogStateTransition("Received 1xx, transitioning to Proceeding");
        
        if (_state == ClientTransactionState.Trying)
        {
            StartTimerE();
        }
        
        TransitionTo(ClientTransactionState.Proceeding);
    }

    private void HandleFinalResponse(SipResponse response)
    {
        if (_state != ClientTransactionState.Trying && _state != ClientTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring final response in non-Trying/Proceeding state");
            return;
        }

        LogStateTransition("Received final response, transitioning to Completed");
        
        StopTimer(SipTimerNames.TimerE);
        StopTimer(SipTimerNames.TimerF);
        
        TransitionTo(ClientTransactionState.Completed);
        
        StartTimerK();
    }

    public override void Retransmit()
    {
        if (_state == ClientTransactionState.Trying || _state == ClientTransactionState.Proceeding)
        {
            LogStateTransition("Retransmitting request");
            OnRequestSent(_request!, _remoteEndPoint!);
        }
    }

    public override void TransportError()
    {
        if (_state == ClientTransactionState.Trying || _state == ClientTransactionState.Proceeding)
        {
            LogStateTransition("Transport error");
            StopTimer(SipTimerNames.TimerE);
            StopTimer(SipTimerNames.TimerF);
            TransitionTo(ClientTransactionState.Terminated);
            FireTimeout();
        }
    }

    protected override void StartTimerA() => throw new InvalidOperationException("Non-INVITE client transaction does not use Timer A");
    protected override void StartTimerB() => throw new InvalidOperationException("Non-INVITE client transaction does not use Timer B");
    protected override void StartTimerD() => throw new InvalidOperationException("Non-INVITE client transaction does not use Timer D");

    protected override void StartTimerE()
    {
        var timer = CreateTimer(SipTimerNames.TimerE);
        timer.Start(_timerEInterval, OnTimerEFires);
        _logger.LogDebug("Timer E started with interval {Interval}", _timerEInterval);
    }

    private void OnTimerEFires()
    {
        if (_state != ClientTransactionState.Trying && _state != ClientTransactionState.Proceeding)
            return;

        _logger.LogDebug("Timer E fired, retransmitting request");
        Retransmit();
        
        if (_state == ClientTransactionState.Trying)
        {
            _timerEInterval = _timerEInterval * 2;
            if (_timerEInterval > _timerFactory.T2)
                _timerEInterval = _timerFactory.T2;
        }
        
        GetTimer(SipTimerNames.TimerE)?.Change(_timerEInterval);

        _logger.LogDebug("Timer E interval updated to {Interval}", _timerEInterval);
    }

    protected override void StartTimerF()
    {
        var timer = CreateTimer(SipTimerNames.TimerF);
        timer.Start(_timerFactory.TimerF, OnTimerFFires);
        _logger.LogDebug("Timer F started with interval {Interval}", _timerFactory.TimerF);
    }

    private void OnTimerFFires()
    {
        if (_state != ClientTransactionState.Trying)
            return;

        _logger.LogWarning("Timer F fired - transaction timeout");
        TransitionTo(ClientTransactionState.Terminated);
        FireTimeout();
    }

    protected override void StartTimerK()
    {
        var timer = CreateTimer(SipTimerNames.TimerK);
        timer.Start(_timerFactory.TimerK, OnTimerKFires);
        _logger.LogDebug("Timer K started with interval {Interval}", _timerFactory.TimerK);
    }

    private void OnTimerKFires()
    {
        if (_state != ClientTransactionState.Completed)
            return;

        _logger.LogDebug("Timer K fired, transitioning to Terminated");
        TransitionTo(ClientTransactionState.Terminated);
    }

    protected override void StopAllTimers()
    {
        StopTimer(SipTimerNames.TimerE);
        StopTimer(SipTimerNames.TimerF);
        StopTimer(SipTimerNames.TimerK);
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
