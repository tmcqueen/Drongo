namespace Drongo.Media;

public interface IMediaSession : IAsyncDisposable
{
    SessionState State { get; }
    int LocalPort { get; }
    int RemotePort { get; }
    string? LocalAddress { get; }
    string? RemoteAddress { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task PlayAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default);
    Task<Stream> RecordAsync(CancellationToken ct = default);
    IAsyncEnumerable<DtmfEvent> ReceiveDtmfAsync(CancellationToken ct = default);
}

public enum SessionState
{
    Idle,
    Starting,
    Active,
    Stopping,
    Stopped
}

public record DtmfEvent(char Digit, int Duration, DateTimeOffset Timestamp);

public record MediaSessionOptions(
    int LocalPort = 0,
    string? RemoteAddress = null,
    int? RemotePort = null,
    string? LocalAddress = null);
