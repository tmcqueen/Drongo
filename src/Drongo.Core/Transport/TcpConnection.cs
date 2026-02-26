using System.Net;
using System.Net.Sockets;

namespace Drongo.Core.Transport;

/// <summary>
/// Wraps a System.Net.Sockets.TcpClient as an ITcpConnection.
/// </summary>
internal class TcpConnection : ITcpConnection
{
    private readonly NetworkStream _stream;
    private readonly System.Net.Sockets.TcpClient _client;
    private bool _disposed;
    private bool _remoteClosedConnection;

    public TcpConnection(System.Net.Sockets.TcpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stream = client.GetStream();
    }

    public EndPoint RemoteEndpoint => _client.Client.RemoteEndPoint!;

    public bool IsConnected => !_disposed && !_remoteClosedConnection;

    public async Task<int> ReceiveAsync(byte[] buffer)
    {
        if (_disposed || _remoteClosedConnection)
            return 0;

        try
        {
            var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                // Remote closed the connection (EOF)
                _remoteClosedConnection = true;
            }
            return bytesRead;
        }
        catch (IOException)
        {
            _remoteClosedConnection = true;
            return 0;
        }
    }

    public async Task SendAsync(byte[] data)
    {
        if (_disposed || !_client.Connected)
            throw new InvalidOperationException("Connection is not active");

        await _stream.WriteAsync(data, 0, data.Length);
        await _stream.FlushAsync();
    }

    public async Task CloseAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _stream?.Dispose();
            _client?.Close();
            _client?.Dispose();
        }
    }

    public void Dispose()
    {
        CloseAsync().GetAwaiter().GetResult();
    }
}
