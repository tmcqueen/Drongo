using Drongo.Media;
using Xunit;

namespace Drongo.Core.Tests.Media;

/// <summary>
/// Tests for MediaBridge interface and implementation.
/// </summary>
public class MediaBridgeTests
{
    [Fact]
    public void IMediaBridge_InterfaceExists()
    {
        // RED: Test that IMediaBridge interface exists
        var type = typeof(IMediaBridge);
        
        Assert.NotNull(type);
    }

    [Fact]
    public void MediaBridge_AddSession_ReturnsSessionId()
    {
        // RED: Test adding a session to the bridge returns an ID
        var bridge = new MediaBridge();
        
        var sessionId = bridge.AddSession(new MockMediaSession());
        
        Assert.NotNull(sessionId);
        Assert.NotEmpty(sessionId);
    }

    [Fact]
    public void MediaBridge_AddMultipleSessions_ReturnsUniqueIds()
    {
        // RED: Test each added session gets unique ID
        var bridge = new MediaBridge();
        
        var id1 = bridge.AddSession(new MockMediaSession());
        var id2 = bridge.AddSession(new MockMediaSession());
        
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void MediaBridge_RemoveSession_ReturnsTrue()
    {
        // RED: Test removing an existing session returns true
        var bridge = new MediaBridge();
        
        var sessionId = bridge.AddSession(new MockMediaSession());
        var removed = bridge.RemoveSession(sessionId);
        
        Assert.True(removed);
    }

    [Fact]
    public void MediaBridge_RemoveInvalidSession_ReturnsFalse()
    {
        // RED: Test removing non-existent session returns false
        var bridge = new MediaBridge();
        
        var removed = bridge.RemoveSession("invalid-id");
        
        Assert.False(removed);
    }

    [Fact]
    public void MediaBridge_ConnectTwoSessions_CreatesBridge()
    {
        // RED: Test connecting two sessions creates a media bridge
        var bridge = new MediaBridge();
        
        var id1 = bridge.AddSession(new MockMediaSession());
        var id2 = bridge.AddSession(new MockMediaSession());
        
        var connected = bridge.Connect(id1, id2);
        
        Assert.True(connected);
    }

    [Fact]
    public void MediaBridge_Connect_InvalidSession_ReturnsFalse()
    {
        // RED: Test connecting with invalid session returns false
        var bridge = new MediaBridge();
        
        var id1 = bridge.AddSession(new MockMediaSession());
        var connected = bridge.Connect(id1, "invalid-id");
        
        Assert.False(connected);
    }

    [Fact]
    public void MediaBridge_Disconnect_RemovesBridge()
    {
        // RED: Test disconnecting sessions removes the bridge
        var bridge = new MediaBridge();
        
        var id1 = bridge.AddSession(new MockMediaSession());
        var id2 = bridge.AddSession(new MockMediaSession());
        bridge.Connect(id1, id2);
        
        var disconnected = bridge.Disconnect(id1, id2);
        
        Assert.True(disconnected);
    }

    [Fact]
    public void MediaBridge_GetConnectedSession_ReturnsPeer()
    {
        // RED: Test getting the connected peer session
        var bridge = new MediaBridge();
        
        var id1 = bridge.AddSession(new MockMediaSession());
        var id2 = bridge.AddSession(new MockMediaSession());
        bridge.Connect(id1, id2);
        
        var peer = bridge.GetConnectedSession(id1);
        
        Assert.Equal(id2, peer);
    }

    [Fact]
    public void MediaBridge_GetConnectedSession_NotConnected_ReturnsNull()
    {
        // RED: Test getting peer for unconnected session returns null
        var bridge = new MediaBridge();
        
        var id1 = bridge.AddSession(new MockMediaSession());
        
        var peer = bridge.GetConnectedSession(id1);
        
        Assert.Null(peer);
    }
}

/// <summary>
/// Mock media session for testing.
/// </summary>
public class MockMediaSession : IMediaSession
{
    public SessionState State => SessionState.Active;
    public int LocalPort => 5000;
    public int RemotePort => 5001;
    public string? LocalAddress => "10.0.0.1";
    public string? RemoteAddress => "10.0.0.2";

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task PlayAsync(ReadOnlyMemory<byte> audioData, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Stream> RecordAsync(CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream());
    public IAsyncEnumerable<DtmfEvent> ReceiveDtmfAsync(CancellationToken ct = default) => AsyncEnumerable.Empty<DtmfEvent>();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
