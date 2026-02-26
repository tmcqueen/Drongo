using System.Net;

namespace Drongo.Core.Hosting;

public interface IEndpointBuilder
{
    IPAddress Address { get; }
    int Port { get; }
    IEndpointBuilder MapEndpoint(IPAddress address, int port);
    IEndpointBuilder WithTransport<TTransport>() where TTransport : class;
    IEndpointBuilder WithTls(string? certificatePath = null, string? certificatePassword = null);
    IReadOnlyList<EndpointInfo> Build();
}
