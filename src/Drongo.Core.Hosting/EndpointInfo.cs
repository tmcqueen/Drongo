using System.Net;
using System.Net.Sockets;

namespace Drongo.Core.Hosting;

public sealed record EndpointInfo(ProtocolType Protocol, IPAddress Address, int Port)
{
    public override string ToString() => $"{Protocol} {Address}:{Port}";
}
