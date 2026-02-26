using System.Collections.Concurrent;
using System.Net;
using Drongo.Core.SIP.Messages;
using Drongo.Core.SIP.Timers;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.SIP.Transactions;

public abstract class ClientTransaction : IClientTransaction
{
    protected readonly ITimerFactory _timerFactory;
    protected readonly ILogger _logger;
    protected readonly ConcurrentDictionary<string, Drongo.Core.SIP.Timers.ITimer> _timers = new();
    
    protected ClientTransactionState _state = ClientTransactionState.Terminated;
    protected EndPoint? _remoteEndPoint;
    protected readonly string _id;
    protected readonly SipMethod _method;

    public string Id => _id;
    public SipMethod Method => _method;
    public ClientTransactionState State => _state;
    public EndPoint? RemoteEndPoint => _remoteEndPoint;

    public event Action<SipRequest, EndPoint>? RequestSent;
    public event Action<SipResponse>? ResponseReceived;
    public event Action? TransactionTimeout;
    public event Action? TransactionTerminated;

    protected ClientTransaction(
        string id,
        SipMethod method,
        ITimerFactory timerFactory,
        ILogger logger)
    {
        _id = id;
        _method = method;
        _timerFactory = timerFactory;
        _logger = logger;
    }

    public abstract Task StartAsync(SipRequest request, EndPoint remoteEndPoint, CancellationToken ct = default);

    public abstract void ReceiveResponse(SipResponse response);

    public abstract void Retransmit();

    public abstract void TransportError();

    protected abstract void StartTimerA();
    protected abstract void StartTimerB();
    protected abstract void StartTimerD();
    protected abstract void StartTimerE();
    protected abstract void StartTimerF();
    protected abstract void StartTimerK();
    protected abstract void StopAllTimers();

    protected void TransitionTo(ClientTransactionState newState)
    {
        var oldState = _state;
        _state = newState;
        
        _logger.LogDebug(
            "Client transaction {TransactionId} transitioned from {OldState} to {NewState}",
            _id, oldState, newState);
        
        if (newState == ClientTransactionState.Terminated)
        {
            StopAllTimers();
            TransactionTerminated?.Invoke();
        }
    }

    protected void FireTimeout() => TransactionTimeout?.Invoke();

    protected void OnRequestSent(SipRequest request, EndPoint remoteEndPoint)
    {
        RequestSent?.Invoke(request, remoteEndPoint);
    }

    protected void OnResponseReceived(SipResponse response)
    {
        ResponseReceived?.Invoke(response);
    }

    public void Dispose()
    {
        StopAllTimers();
    }

    protected Drongo.Core.SIP.Timers.ITimer CreateTimer(string name)
    {
        var timer = _timerFactory.Create();
        _timers[name] = timer;
        return timer;
    }

    protected void StopTimer(string name)
    {
        if (_timers.TryRemove(name, out var timer))
        {
            timer.Stop();
            timer.Dispose();
        }
    }

    protected Drongo.Core.SIP.Timers.ITimer? GetTimer(string name)
    {
        return _timers.TryGetValue(name, out var timer) ? timer : null;
    }

    protected void LogStateTransition(string eventName)
    {
        _logger.LogTrace(
            "Client transaction {TransactionId} ({Method}): {Event} in state {State}",
            _id, _method, eventName, _state);
    }
}
