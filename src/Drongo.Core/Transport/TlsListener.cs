using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace Drongo.Core.Transport;

public interface ITlsListenerService : IDisposable
{
    EndPoint LocalEndpoint { get; }
    Task StartAsync();
    Task StopAsync();
    Task<ITcpConnection> AcceptConnectionAsync();
}

public class TlsListener : ITlsListenerService
{
    private System.Net.Sockets.TcpListener? _listener;
    private readonly X509Certificate2 _certificate;
    private readonly int _port;
    private readonly bool _clientCertificateRequired;
    private readonly Dictionary<string, X509Certificate2> _sniCertificates;
    private bool _started;
    private bool _stopped;
    private bool _disposed;

    public TlsListener(X509Certificate2 certificate, int port, bool clientCertificateRequired = false, Dictionary<string, X509Certificate2>? sniCertificates = null)
    {
        _certificate = certificate;
        _port = port;
        _clientCertificateRequired = clientCertificateRequired;
        _sniCertificates = sniCertificates ?? new Dictionary<string, X509Certificate2>();
    }

    public EndPoint LocalEndpoint
    {
        get
        {
            if (_listener == null)
                throw new InvalidOperationException("Listener not started");
            return _listener.LocalEndpoint;
        }
    }

    public async Task StartAsync()
    {
        if (_started || _stopped || _disposed)
            throw new InvalidOperationException("Listener already started or stopped");

        _listener = new System.Net.Sockets.TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _started = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!_started || _stopped)
            throw new InvalidOperationException("Listener not started");

        _stopped = true;
        _listener?.Stop();
        await Task.CompletedTask;
    }

    public async Task<ITcpConnection> AcceptConnectionAsync()
    {
        if (!_started || _stopped)
            throw new InvalidOperationException("Listener not started");

        var client = await _listener!.AcceptTcpClientAsync();
        
        var sslStream = new SslStream(
            client.GetStream(),
            false,
            (sender, certificate, chain, errors) => true
        );
        
        await sslStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
        {
            ServerCertificate = _certificate,
            ClientCertificateRequired = _clientCertificateRequired
        });
        
        return new TlsConnection(client, sslStream);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _listener?.Stop();
            _listener?.Dispose();
        }
    }
}

public class TlsConnection : ITcpConnection
{
    private readonly TcpClient _client;
    private readonly SslStream _sslStream;
    private bool _isConnected;

    public TlsConnection(TcpClient client, SslStream sslStream)
    {
        _client = client;
        _sslStream = sslStream;
        _isConnected = true;
    }

    public EndPoint RemoteEndpoint => _client.Client.RemoteEndPoint ?? throw new InvalidOperationException("Not connected");
    public bool IsConnected => _isConnected;

    public async Task<int> ReceiveAsync(byte[] buffer)
    {
        if (!_isConnected)
            return 0;

        try
        {
            var bytesRead = await _sslStream.ReadAsync(buffer);
            if (bytesRead == 0)
            {
                _isConnected = false;
            }
            return bytesRead;
        }
        catch (Exception)
        {
            _isConnected = false;
            return 0;
        }
    }

    public async Task SendAsync(byte[] data)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Connection is not connected");

        await _sslStream.WriteAsync(data);
    }

    public Task CloseAsync()
    {
        _isConnected = false;
        _sslStream.Close();
        _client.Close();
        return Task.CompletedTask;
    }

    public void Close()
    {
        CloseAsync().Wait();
    }

    public void Dispose()
    {
        Close();
        _sslStream.Dispose();
        _client.Dispose();
    }
}
