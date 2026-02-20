using System.Net;
using Drongo.Core.Messages;

namespace Drongo.Core.Transactions;

public enum ClientTransactionState
{
    Calling,
    Trying,
    Proceeding,
    Completed,
    Terminated
}

public interface IClientTransaction
{
    string Id { get; }
    SipMethod Method { get; }
    ClientTransactionState State { get; }
    EndPoint? RemoteEndPoint { get; }
    
    Task StartAsync(SipRequest request, EndPoint remoteEndPoint, CancellationToken ct = default);
    void ReceiveResponse(SipResponse response);
    void Retransmit();
    void TransportError();
    
    event Action<SipRequest, EndPoint>? RequestSent;
    event Action<SipResponse>? ResponseReceived;
    event Action? TransactionTimeout;
    event Action? TransactionTerminated;
}
