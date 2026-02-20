using System.Collections.Concurrent;
using System.Net;
using Drongo.Core.Messages;
using Drongo.Core.Timers;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Transactions;

public interface IClientTransactionFactory
{
    IClientTransaction? MatchTransaction(SipResponse response);
    IClientTransaction CreateTransaction(SipRequest request, EndPoint remoteEndPoint);
    void RemoveTransaction(string transactionId);
}

public sealed class ClientTransactionFactory : IClientTransactionFactory
{
    private readonly ITimerFactory _timerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, IClientTransaction> _transactions = new();
    private readonly ConcurrentDictionary<string, IClientTransaction> _inviteTransactions = new();

    public ClientTransactionFactory(ITimerFactory timerFactory, ILoggerFactory loggerFactory)
    {
        _timerFactory = timerFactory;
        _loggerFactory = loggerFactory;
    }

    public IClientTransaction? MatchTransaction(SipResponse response)
    {
        var branch = ExtractBranch(response.Via);
        var cseqMethod = ExtractCSeqMethod(response.CSeq);
        
        if (branch == null)
            return null;

        if (cseqMethod == SipMethod.Invite || cseqMethod == SipMethod.Ack)
        {
            return _inviteTransactions.TryGetValue(branch, out var tx) ? tx : null;
        }
        
        return _transactions.TryGetValue(branch, out var tx2) ? tx2 : null;
    }

    public IClientTransaction CreateTransaction(SipRequest request, EndPoint remoteEndPoint)
    {
        var branch = GenerateBranch();
        var key = branch;
        
        var logger = _loggerFactory.CreateLogger<ClientTransaction>();
        
        IClientTransaction transaction;
        if (request.Method == SipMethod.Invite)
        {
            transaction = new InviteClientTransaction(
                key,
                _timerFactory,
                logger);
            
            _inviteTransactions[key] = transaction;
        }
        else
        {
            transaction = new NonInviteClientTransaction(
                key,
                request.Method,
                _timerFactory,
                logger);
            
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

    private static SipMethod ExtractCSeqMethod(string? cseqHeader)
    {
        if (string.IsNullOrEmpty(cseqHeader))
            return SipMethod.Invite;

        var parts = cseqHeader.Split(' ', 2);
        if (parts.Length < 2)
            return SipMethod.Invite;

        try
        {
            return SipMethodExtensions.ParseMethod(parts[1].Trim());
        }
        catch
        {
            return SipMethod.Invite;
        }
    }

    private static string GenerateBranch()
    {
        return "z9hG4bK" + Guid.NewGuid().ToString("N")[..16];
    }
}
