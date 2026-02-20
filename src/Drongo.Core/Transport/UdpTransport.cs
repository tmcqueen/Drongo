using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Drongo.Core.Messages;
using Drongo.Core.Parsing;
using Microsoft.Extensions.Logging;

namespace Drongo.Core.Transport;

public sealed class UdpTransport : IUdpTransport, IAsyncDisposable, IDisposable
{
    private readonly IUdpTransportFactory _factory;
    private readonly ISipParser _parser;
    private readonly ILogger<UdpTransport> _logger;
    private readonly int _port;
    private readonly IPAddress? _address;
    private readonly int _receiveBufferSize;
    private readonly Channel<(SipRequest? Request, SipResponse? Response, EndPoint? RemoteEndpoint)> _messageChannel;

    private Socket? _socket;
    private readonly List<SocketAsyncEventArgs> _receiveArgs = new();
    private readonly byte[] _buffer;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;
    private Task? _dispatchLoop;
    private bool _isRunning;
    private bool _disposed;

    public bool IsRunning => _isRunning;

    public UdpTransport(
        IUdpTransportFactory factory,
        ISipParser parser,
        ILogger<UdpTransport> logger,
        int port,
        IPAddress? address = null,
        int receiveBufferSize = 65536)
    {
        _factory = factory;
        _parser = parser;
        _logger = logger;
        _port = port;
        _address = address ?? IPAddress.Any;
        _receiveBufferSize = receiveBufferSize;
        _buffer = new byte[receiveBufferSize];
        _messageChannel = Channel.CreateBounded<(SipRequest?, SipResponse?, EndPoint?)>(100);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (_isRunning)
            return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        
        ArgumentNullException.ThrowIfNull(_address, nameof(_address));

        var endpoint = new IPEndPoint(_address, _port);
        _socket.Bind(endpoint);

        _logger.LogInformation("UDP transport listening on {Endpoint}", endpoint);

        var receiveCount = Math.Max(1, Environment.ProcessorCount);
        for (int i = 0; i < receiveCount; i++)
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(_buffer, 0, _buffer.Length);
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            args.Completed += OnReceiveCompleted;
            _receiveArgs.Add(args);
            
            if (!_socket.ReceiveFromAsync(args))
            {
                _ = ProcessReceive(args);
            }
        }

        _receiveLoop = ReceiveLoopAsync(_cts.Token);
        _dispatchLoop = DispatchLoopAsync(_cts.Token);

        _isRunning = true;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _socket != null)
        {
            try
            {
                await Task.Delay(100, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task DispatchLoopAsync(CancellationToken ct)
    {
        await foreach (var (request, response, remoteEndpoint) in _messageChannel.Reader.ReadAllAsync(ct))
        {
            try
            {
                if (request != null)
                {
                    _logger.LogDebug("Dispatching request {Method} from {Endpoint}", request.Method, remoteEndpoint);
                }
                else if (response != null)
                {
                    _logger.LogDebug("Dispatching response {StatusCode} to {Endpoint}", response.StatusCode, remoteEndpoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching message");
            }
        }
    }

    private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        _ = ProcessReceive(e);
    }

    private Task ProcessReceive(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
        {
            var data = new ReadOnlySequence<byte>(_buffer, 0, e.BytesTransferred);
            
            if (data.First.Span[0] == 'S' && data.First.Span.Length >= 4)
            {
                var firstLine = data.First.Span[..4];
                if (firstLine.StartsWith("SIP/"u8))
                {
                    var result = _parser.ParseResponse(data);
                    if (result.IsSuccess && result.Response != null)
                    {
                        _ = _messageChannel.Writer.WriteAsync((null, result.Response, e.RemoteEndPoint));
                    }
                }
                else
                {
                    var result = _parser.ParseRequest(data);
                    if (result.IsSuccess && result.Request != null)
                    {
                        _ = _messageChannel.Writer.WriteAsync((result.Request, null, e.RemoteEndPoint));
                    }
                }
            }
        }

        if (_socket != null && _isRunning)
        {
            e.SetBuffer(0, _buffer.Length);
            if (!_socket.ReceiveFromAsync(e))
            {
                return ProcessReceive(e);
            }
        }

        return Task.CompletedTask;
    }

    public async Task SendResponseAsync(ReadOnlyMemory<byte> data, EndPoint remoteEndpoint)
    {
        if (_socket == null || !_isRunning)
            throw new InvalidOperationException("Transport not running");

        await _socket.SendToAsync(data, SocketFlags.None, remoteEndpoint);
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _cts?.Cancel();

        if (_socket != null)
        {
            _socket.Close();
            _socket.Dispose();
            _socket = null;
        }

        _messageChannel.Writer.Complete();

        if (_receiveLoop != null)
            await _receiveLoop;
        if (_dispatchLoop != null)
            await _dispatchLoop;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        await StopAsync();
        _cts?.Dispose();
        foreach (var args in _receiveArgs)
        {
            args.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        try
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
    }
}

public sealed class UdpTransportFactory : IUdpTransportFactory
{
    private readonly ISipParser _parser;
    private readonly ILoggerFactory _loggerFactory;

    public UdpTransportFactory(ISipParser parser, ILoggerFactory loggerFactory)
    {
        _parser = parser;
        _loggerFactory = loggerFactory;
    }

    public IUdpTransport Create(int port, IPAddress? address = null)
    {
        var logger = _loggerFactory.CreateLogger<UdpTransport>();
        return new UdpTransport(this, _parser, logger, port, address);
    }
}
