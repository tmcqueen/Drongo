using System.Net;
using System.Net.Sockets;

namespace Drongo.Core.Hosting;

public sealed class EndpointBuilder : IEndpointBuilder
{
    private readonly List<EndpointBuilder> _builders = new();
    private readonly IPAddress _address;
    private readonly int _port;
    private Type? _transportType;
    private string? _tlsCertificatePath;
    private string? _tlsCertificatePassword;

    public IPAddress Address => _address;
    public int Port => _port;

    public EndpointBuilder(IPAddress address, int port)
    {
        _address = address;
        _port = port;
    }

    public IEndpointBuilder WithTransport<TTransport>() where TTransport : class
    {
        _transportType = typeof(TTransport);
        foreach (var builder in _builders)
        {
            builder.WithTransport<TTransport>();
        }
        return this;
    }

    public IEndpointBuilder WithTls(string? certificatePath = null, string? certificatePassword = null)
    {
        _tlsCertificatePath = certificatePath;
        _tlsCertificatePassword = certificatePassword;
        _transportType = typeof(System.Net.Sockets.Socket); // TLS implies TCP
        foreach (var builder in _builders)
        {
            builder.WithTransport<System.Net.Sockets.Socket>();
        }
        return this;
    }

    public IEndpointBuilder MapEndpoint(IPAddress address, int port)
    {
        var builder = new EndpointBuilder(address, port);
        _builders.Add(builder);
        return this;
    }

    public IReadOnlyList<EndpointInfo> Build()
    {
        var protocol = _transportType != null 
            ? ProtocolType.Tcp
            : ProtocolType.Udp;
        
        var endpoints = new List<EndpointInfo>
        {
            new EndpointInfo(protocol, _address, _port)
        };
        
        foreach (var builder in _builders)
        {
            endpoints.AddRange(builder.Build());
        }
        
        return endpoints.AsReadOnly();
    }
}
