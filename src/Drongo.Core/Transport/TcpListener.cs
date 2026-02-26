using System.Net;
using System.Net.Sockets;

namespace Drongo.Core.Transport;

/// <summary>
/// TCP listener that accepts incoming connections for SIP messages.
/// Block 1: Connection Management - Accept TCP connections, maintain per-connection state, handle lifecycle.
/// </summary>
public class TcpListener : ITcpListenerService
{
    private System.Net.Sockets.TcpListener? _listener;
    private readonly int _port;
    private bool _started;
    private bool _stopped;
    private bool _disposed;

    public TcpListener(int port)
    {
        _port = port;
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
        return new TcpConnection(client);
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
