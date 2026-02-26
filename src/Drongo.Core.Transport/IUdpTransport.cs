using System.Net;
using Drongo.Core.SIP.Messages;
using Drongo.Core.SIP.Parsing;

namespace Drongo.Core.Transport;

public interface IUdpTransport
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    Task SendResponseAsync(ReadOnlyMemory<byte> data, EndPoint remoteEndpoint);
    bool IsRunning { get; }
}

public interface ITransportMessageHandler
{
    Task HandleRequestAsync(SipRequest request, EndPoint remoteEndpoint);
    Task HandleResponseAsync(SipResponse response, EndPoint remoteEndpoint);
}

public interface IUdpTransportFactory
{
    IUdpTransport Create(int port, IPAddress? address = null);
}
