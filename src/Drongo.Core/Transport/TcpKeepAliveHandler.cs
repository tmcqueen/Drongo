namespace Drongo.Core.Transport;

/// <summary>
/// Handles TCP keep-alive per RFC 3261 §18.3.
/// Sends CRLF keep-alives at configurable intervals to keep connections alive.
/// Block 3: Keep-Alive & Lifecycle - CRLF keep-alive, configurable intervals, graceful shutdown.
/// </summary>
public class TcpKeepAliveHandler : IDisposable
{
    private readonly ITcpConnection _connection;
    private readonly TimeSpan _keepAliveInterval;
    private CancellationTokenSource? _cts;
    private Task? _keepAliveTask;
    private DateTime _lastActivityTime;
    private bool _disposed;

    public TcpKeepAliveHandler(ITcpConnection connection, TimeSpan keepAliveInterval)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _keepAliveInterval = keepAliveInterval;
        _lastActivityTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Starts the keep-alive timer and background task.
    /// </summary>
    public async Task StartAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TcpKeepAliveHandler));

        if (_cts != null)
            return; // Already started

        _cts = new CancellationTokenSource();
        _lastActivityTime = DateTime.UtcNow;
        _keepAliveTask = RunKeepAliveLoopAsync(_cts.Token);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops the keep-alive timer.
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_keepAliveTask != null)
            {
                try
                {
                    await _keepAliveTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }
            _cts.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// Records activity to reset the keep-alive timer.
    /// </summary>
    public void RecordActivity()
    {
        _lastActivityTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Background task that periodically sends CRLF keep-alives.
    /// </summary>
    private async Task RunKeepAliveLoopAsync(CancellationToken ct)
    {
        var keepAliveByte = new byte[] { (byte)'\r', (byte)'\n' };

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_keepAliveInterval, ct);

                if (ct.IsCancellationRequested)
                    break;

                // Only send keep-alive if no other activity
                var timeSinceLastActivity = DateTime.UtcNow - _lastActivityTime;
                if (timeSinceLastActivity >= _keepAliveInterval && _connection.IsConnected)
                {
                    await _connection.SendAsync(keepAliveByte);
                    _lastActivityTime = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Connection might be closed, exit gracefully
                break;
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopAsync().GetAwaiter().GetResult();
        }
    }
}
