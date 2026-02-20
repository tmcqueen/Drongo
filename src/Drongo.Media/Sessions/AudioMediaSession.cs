using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Drongo.Media;

public sealed class AudioMediaSession : IMediaSession
{
    private readonly ILogger<AudioMediaSession> _logger;
    private readonly MediaSessionOptions _options;
    private SessionState _state = SessionState.Idle;
    private int _localPort;
    private int _remotePort;
    private string? _localAddress;
    private string? _remoteAddress;
    private readonly ConcurrentQueue<DtmfEvent> _dtmfQueue = new();
    private readonly CancellationTokenSource _cts = new();

    public SessionState State => _state;
    public int LocalPort => _localPort;
    public int RemotePort => _remotePort;
    public string? LocalAddress => _localAddress;
    public string? RemoteAddress => _remoteAddress;

    public AudioMediaSession(MediaSessionOptions options, ILogger<AudioMediaSession> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_state != SessionState.Idle)
        {
            throw new InvalidOperationException($"Cannot start from state {_state}");
        }

        _state = SessionState.Starting;
        _localPort = _options.LocalPort > 0 ? _options.LocalPort : GetAvailablePort();
        _localAddress = _options.LocalAddress ?? "0.0.0.0";
        _remoteAddress = _options.RemoteAddress;
        _remotePort = _options.RemotePort ?? 0;

        _logger.LogInformation("Media session started on {LocalAddress}:{LocalPort}", _localAddress, _localPort);

        _state = SessionState.Active;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        if (_state != SessionState.Active)
        {
            return Task.CompletedTask;
        }

        _state = SessionState.Stopping;
        _cts.Cancel();

        _logger.LogInformation("Media session stopped");

        _state = SessionState.Stopped;
        return Task.CompletedTask;
    }

    public Task PlayAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default)
    {
        if (_state != SessionState.Active)
        {
            throw new InvalidOperationException("Cannot play when not active");
        }

        _logger.LogDebug("Playing {Bytes} bytes of audio", audioData.Length);
        return Task.CompletedTask;
    }

    public Task<Stream> RecordAsync(CancellationToken ct = default)
    {
        if (_state != SessionState.Active)
        {
            throw new InvalidOperationException("Cannot record when not active");
        }

        var stream = new MemoryStream();
        _logger.LogDebug("Recording started to stream");
        return Task.FromResult<Stream>(stream);
    }

    public async IAsyncEnumerable<DtmfEvent> ReceiveDtmfAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested && _state == SessionState.Active)
        {
            if (_dtmfQueue.TryDequeue(out var dtmf))
            {
                yield return dtmf;
            }
            await Task.Delay(50, ct);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_state == SessionState.Active)
        {
            StopAsync().GetAwaiter().GetResult();
        }
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }

    private static int GetAvailablePort()
    {
        return Random.Shared.Next(10000, 60000) | 1;
    }
}
