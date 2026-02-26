using System.Net;
using Drongo.Core.SIP.Messages;
using Drongo.Core.SIP.Timers;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.SIP.Transactions;

public sealed class InviteClientTransaction : ClientTransaction
{
    private SipRequest? _request;
    private TimeSpan _timerAInterval;
    private CancellationTokenSource? _cts;

    public InviteClientTransaction(
        string id,
        ITimerFactory timerFactory,
        ILogger logger) 
        : base(id, SipMethod.Invite, timerFactory, logger)
    {
    }

    public override async Task StartAsync(SipRequest request, EndPoint remoteEndPoint, CancellationToken ct = default)
    {
        _request = request;
        _remoteEndPoint = remoteEndPoint;
        _timerAInterval = _timerFactory.T1;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        
        TransitionTo(ClientTransactionState.Calling);
        
        LogStateTransition("Starting INVITE transaction");
        
        OnRequestSent(request, remoteEndPoint);
        
        StartTimerA();
        StartTimerB();
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
        else if (response.IsSuccess)
        {
            HandleSuccessResponse(response);
        }
        else
        {
            HandleFinalNon2xxResponse(response);
        }
    }

    private void HandleProvisionalResponse(SipResponse response)
    {
        if (_state != ClientTransactionState.Calling && _state != ClientTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring 1xx response in non-Calling/Proceeding state");
            return;
        }

        LogStateTransition("Received 1xx, transitioning to Proceeding");
        TransitionTo(ClientTransactionState.Proceeding);
    }

    private void HandleSuccessResponse(SipResponse response)
    {
        if (_state != ClientTransactionState.Calling && _state != ClientTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring 2xx response in non-Calling/Proceeding state");
            return;
        }

        LogStateTransition("Received 2xx, transitioning to Terminated");
        
        StopTimer(SipTimerNames.TimerA);
        StopTimer(SipTimerNames.TimerB);
        
        TransitionTo(ClientTransactionState.Terminated);
    }

    private void HandleFinalNon2xxResponse(SipResponse response)
    {
        if (_state != ClientTransactionState.Calling && _state != ClientTransactionState.Proceeding)
        {
            LogStateTransition("Ignoring 300-699 response in non-Calling/Proceeding state");
            return;
        }

        LogStateTransition("Received 300-699, transitioning to Completed");
        
        StopTimer(SipTimerNames.TimerA);
        StopTimer(SipTimerNames.TimerB);
        
        TransitionTo(ClientTransactionState.Completed);
        
        StartTimerD();
    }

    public override void Retransmit()
    {
        if (_state == ClientTransactionState.Calling || _state == ClientTransactionState.Proceeding)
        {
            LogStateTransition("Retransmitting INVITE request");
            OnRequestSent(_request!, _remoteEndPoint!);
        }
    }

    public override void TransportError()
    {
        if (_state == ClientTransactionState.Calling)
        {
            LogStateTransition("Transport error in Calling state");
            StopTimer(SipTimerNames.TimerA);
            StopTimer(SipTimerNames.TimerB);
            TransitionTo(ClientTransactionState.Terminated);
            FireTimeout();
        }
    }

    protected override void StartTimerA()
    {
        var timer = CreateTimer(SipTimerNames.TimerA);
        timer.Start(_timerAInterval, OnTimerAFires);
        _logger.LogDebug("Timer A started with interval {Interval}", _timerAInterval);
    }

    private void OnTimerAFires()
    {
        if (_state != ClientTransactionState.Calling)
            return;

        _logger.LogDebug("Timer A fired, retransmitting INVITE");
        Retransmit();
        
        _timerAInterval = _timerAInterval * 2;
        
        var timer = GetTimer(SipTimerNames.TimerA);
        timer?.Change(_timerAInterval);
        
        _logger.LogDebug("Timer A interval updated to {Interval}", _timerAInterval);
    }

    protected override void StartTimerB()
    {
        var timer = CreateTimer(SipTimerNames.TimerB);
        timer.Start(_timerFactory.TimerB, OnTimerBFires);
        _logger.LogDebug("Timer B started with interval {Interval}", _timerFactory.TimerB);
    }

    private void OnTimerBFires()
    {
        if (_state != ClientTransactionState.Calling)
            return;

        _logger.LogWarning("Timer B fired - transaction timeout");
        TransitionTo(ClientTransactionState.Terminated);
        FireTimeout();
    }

    protected override void StartTimerD()
    {
        var timer = CreateTimer(SipTimerNames.TimerD);
        timer.Start(_timerFactory.TimerD, OnTimerDFires);
        _logger.LogDebug("Timer D started with interval {Interval}", _timerFactory.TimerD);
    }

    private void OnTimerDFires()
    {
        if (_state != ClientTransactionState.Completed)
            return;

        _logger.LogDebug("Timer D fired, transitioning to Terminated");
        TransitionTo(ClientTransactionState.Terminated);
    }

    protected override void StartTimerE() => throw new InvalidOperationException("INVITE client transaction does not use Timer E");
    protected override void StartTimerF() => throw new InvalidOperationException("INVITE client transaction does not use Timer F");
    protected override void StartTimerK() => throw new InvalidOperationException("INVITE client transaction does not use Timer K");

    protected override void StopAllTimers()
    {
        StopTimer(SipTimerNames.TimerA);
        StopTimer(SipTimerNames.TimerB);
        StopTimer(SipTimerNames.TimerD);
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
