using System.Collections.Concurrent;
using System.Net;
using Drongo.Core.SIP.Messages;
using Drongo.Core.SIP.Timers;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.SIP.Transactions;

public interface IServerTransactionFactory
{
    IServerTransaction? MatchTransaction(SipRequest request, EndPoint remoteEndPoint);
    IServerTransaction CreateTransaction(SipRequest request, EndPoint remoteEndPoint);
    void RemoveTransaction(string transactionId);
}

public sealed class ServerTransactionFactory : IServerTransactionFactory
{
    private readonly ITimerFactory _timerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, IServerTransaction> _transactions = new();
    private readonly ConcurrentDictionary<string, IServerTransaction> _inviteTransactions = new();

    public ServerTransactionFactory(ITimerFactory timerFactory, ILoggerFactory loggerFactory)
    {
        _timerFactory = timerFactory;
        _loggerFactory = loggerFactory;
    }

    public IServerTransaction? MatchTransaction(SipRequest request, EndPoint remoteEndPoint)
    {
        var branch = ExtractBranch(request.Via);
        
        if (request.Method == SipMethod.Ack)
        {
            return MatchAckTransaction(branch, request);
        }

        ArgumentNullException.ThrowIfNull(branch, nameof(branch));

        var key = GetTransactionKey(branch, request.Method, remoteEndPoint);
        
        if (request.Method == SipMethod.Invite)
        {
            return _inviteTransactions.TryGetValue(key, out var tx) ? tx : null;
        }
        
        return _transactions.TryGetValue(key, out var tx2) ? tx2 : null;
    }

    public IServerTransaction CreateTransaction(SipRequest request, EndPoint remoteEndPoint)
    {
        var branch = ExtractBranch(request.Via) ?? GenerateBranch();
        var key = GetTransactionKey(branch, request.Method, remoteEndPoint);
        
        var logger = _loggerFactory.CreateLogger<ServerTransaction>();
        
        IServerTransaction transaction;
        if (request.Method == SipMethod.Invite)
        {
            transaction = new InviteServerTransaction(
                key,
                _timerFactory,
                logger,
                OnRequestPassedToTu);
            
            _inviteTransactions[key] = transaction;
        }
        else
        {
            transaction = new NonInviteServerTransaction(
                key,
                request.Method,
                _timerFactory,
                logger,
                OnRequestPassedToTu);
            
            _transactions[key] = transaction;
        }

        transaction.TransactionTerminated += () => RemoveTransaction(key);
        
        return transaction;
    }

    public void RemoveTransaction(string transactionId)
    {
        _transactions.TryRemove(transactionId, out _);
        _inviteTransactions.TryRemove(transactionId, out _);
    }

    private void OnRequestPassedToTu(SipRequest request, EndPoint remoteEndPoint)
    {
    }

    private IServerTransaction? MatchAckTransaction(string? branch, SipRequest request)
    {
        if (branch == null)
            return null;

        return _inviteTransactions.TryGetValue(branch, out var tx) ? tx : null;
    }

    private static string? ExtractBranch(string? viaHeader)
    {
        if (string.IsNullOrEmpty(viaHeader))
            return null;

        var parameters = viaHeader.Split(';');
        foreach (var param in parameters)
        {
            var kv = param.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals("branch", StringComparison.OrdinalIgnoreCase))
            {
                return kv[1];
            }
        }

        return null;
    }

    private static string GenerateBranch()
    {
        return "z9hG4bK" + Guid.NewGuid().ToString("N")[..16];
    }

    private static string GetTransactionKey(string branch, SipMethod method, EndPoint remoteEndPoint)
    {
        return $"{branch}:{remoteEndPoint}:{method}";
    }
}
