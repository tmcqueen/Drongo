using System.Net;
using Drongo.Core.Messages;

namespace Drongo.Core.Transactions;

public enum ServerTransactionState
{
    Trying,
    Proceeding,
    Completed,
    Confirmed,
    Terminated
}

public interface IServerTransaction
{
    string Id { get; }
    SipMethod Method { get; }
    ServerTransactionState State { get; }
    SipRequest? Request { get; }
    EndPoint? RemoteEndPoint { get; }
    
    Task<SipResponse> SendResponseAsync(SipResponse response, CancellationToken ct = default);
    void AckReceived();
    void RetransmitRequest();
    
    event Action<SipRequest, EndPoint>? RequestReceived;
    event Action<SipResponse, EndPoint>? ResponseSent;
    event Action? TransactionTimeout;
    event Action? TransactionTerminated;
}

public static class ServerTransactionStateExtensions
{
    public static bool IsInvite(this ServerTransactionState state) => state is 
        ServerTransactionState.Proceeding or 
        ServerTransactionState.Completed or 
        ServerTransactionState.Confirmed;
}
