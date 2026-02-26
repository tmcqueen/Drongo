using System.Collections.Concurrent;
using System.Net;
using Drongo.Core.SIP.Messages;
using Drongo.Core.SIP.Timers;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.SIP.Transactions;

public abstract class ServerTransaction : IServerTransaction
{
    protected readonly ITimerFactory _timerFactory;
    protected readonly ILogger _logger;
    protected readonly ConcurrentDictionary<string, Drongo.Core.SIP.Timers.ITimer> _timers = new();
    
    protected ServerTransactionState _state = ServerTransactionState.Terminated;
    protected SipRequest? _request;
    protected SipResponse? _lastResponse;
    protected EndPoint? _remoteEndPoint;
    protected readonly string _id;
    protected readonly SipMethod _method;

    public string Id => _id;
    public SipMethod Method => _method;
    public ServerTransactionState State => _state;
    public SipRequest? Request => _request;
    public EndPoint? RemoteEndPoint => _remoteEndPoint;

    public event Action<SipRequest, EndPoint>? RequestReceived;
    public event Action<SipResponse, EndPoint>? ResponseSent;
    public event Action? TransactionTimeout;
    public event Action? TransactionTerminated;

    protected ServerTransaction(
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

    public abstract void Start(SipRequest request, EndPoint remoteEndPoint);

    public abstract Task<SipResponse> SendResponseAsync(SipResponse response, CancellationToken ct = default);

    public abstract void AckReceived();

    public abstract void RetransmitRequest();

    protected abstract void StartTimerG();
    protected abstract void StartTimerH();
    protected abstract void StartTimerI();
    protected abstract void StartTimerJ();
    protected abstract void StopAllTimers();

    protected void TransitionTo(ServerTransactionState newState)
    {
        var oldState = _state;
        _state = newState;
        
        _logger.LogDebug(
            "Server transaction {TransactionId} transitioned from {OldState} to {NewState}",
            _id, oldState, newState);
        
        if (newState == ServerTransactionState.Terminated)
        {
            StopAllTimers();
            TransactionTerminated?.Invoke();
        }
    }

    protected void FireTimeout() => TransactionTimeout?.Invoke();

    protected void OnRequestReceived(SipRequest request, EndPoint remoteEndPoint)
    {
        RequestReceived?.Invoke(request, remoteEndPoint);
    }

    protected void OnResponseSent(SipResponse response, EndPoint remoteEndPoint)
    {
        ResponseSent?.Invoke(response, remoteEndPoint);
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
            "Server transaction {TransactionId} ({Method}): {Event} in state {State}",
            _id, _method, eventName, _state);
    }
}
