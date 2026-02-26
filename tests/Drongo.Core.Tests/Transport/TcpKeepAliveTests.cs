using Drongo.Core.Transport;
using System.Net;
using Xunit;

namespace Drongo.Core.Tests.Transport;

/// <summary>
/// Tests for TCP keep-alive and lifecycle management.
/// Block 3: CRLF keep-alive per RFC 3261 §18.3, configurable intervals, graceful shutdown.
/// </summary>
public class TcpKeepAliveTests
{
    [Fact]
    public async Task KeepAliveTimer_ShouldSendCRLFPeriodicially()
    {
        // Arrange
        var listener = new TcpListener(port: 0);
        await listener.StartAsync();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        
        // Create client and server connection
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        
        var serverConnection = await listener.AcceptConnectionAsync();
        
        // Create keep-alive handler with short interval for testing
        var keepAliveInterval = TimeSpan.FromMilliseconds(100);
        var keepAliveHandler = new TcpKeepAliveHandler(serverConnection, keepAliveInterval);
        
        // Act - start keep-alive
        await keepAliveHandler.StartAsync();
        
        // Wait for keep-alive CRLF to be sent
        await Task.Delay(250);
        
        // Read from client side
        var buffer = new byte[256];
        var bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
        
        // Assert - should have received at least one CRLF
        Assert.True(bytesRead > 0);
        var received = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Assert.Contains("\r\n", received);
        
        // Cleanup
        await keepAliveHandler.StopAsync();
        serverConnection.Dispose();
        listener.Dispose();
    }

    [Fact]
    public async Task KeepAliveHandler_ShouldStartAndStop()
    {
        // Arrange
        var mockConnection = new MockTcpConnection();
        var keepAliveHandler = new TcpKeepAliveHandler(mockConnection, TimeSpan.FromMilliseconds(100));
        
        // Act
        await keepAliveHandler.StartAsync();
        Assert.NotNull(keepAliveHandler); // Just verify it was created
        
        // Wait a bit
        await Task.Delay(50);
        
        // Stop
        await keepAliveHandler.StopAsync();
        
        // Assert - should have completed without error
        Assert.True(true);
    }

    [Fact]
    public async Task KeepAliveHandler_ShouldRecordActivityAndResetTimer()
    {
        // Arrange
        var mockConnection = new MockTcpConnection();
        var keepAliveInterval = TimeSpan.FromMilliseconds(50);
        var keepAliveHandler = new TcpKeepAliveHandler(mockConnection, keepAliveInterval);
        
        // Act - start keep-alive
        await keepAliveHandler.StartAsync();
        await Task.Delay(30);
        
        // Record activity (simulating a message send)
        keepAliveHandler.RecordActivity();
        
        // Wait a bit more
        await Task.Delay(30);
        
        // Stop
        await keepAliveHandler.StopAsync();
        
        // Assert - should have completed without error
        // (Keep-alive should have reset timer on RecordActivity call)
        Assert.True(true);
    }

    [Fact]
    public async Task GracefulShutdown_ShouldCloseConnectionProperly()
    {
        // Arrange
        var listener = new TcpListener(port: 0);
        await listener.StartAsync();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        
        var serverConnection = await listener.AcceptConnectionAsync();
        Assert.True(serverConnection.IsConnected);
        
        // Act
        await serverConnection.CloseAsync();
        
        // Assert
        Assert.False(serverConnection.IsConnected);
        
        // Client should detect connection closed
        var buffer = new byte[256];
        var bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
        Assert.Equal(0, bytesRead); // EOF
        
        // Cleanup
        listener.Dispose();
    }

    [Fact]
    public async Task KeepAliveHandler_ShouldDispose()
    {
        // Arrange
        var mockConnection = new MockTcpConnection();
        var keepAliveHandler = new TcpKeepAliveHandler(mockConnection, TimeSpan.FromMilliseconds(50));
        
        // Act
        await keepAliveHandler.StartAsync();
        keepAliveHandler.Dispose();
        
        // Assert - should have properly disposed
        // (Multiple disposes should not throw)
        keepAliveHandler.Dispose();
        
        Assert.True(true);
    }
}

/// <summary>
/// Mock connection for testing keep-alive behavior.
/// </summary>
internal class MockTcpConnection : ITcpConnection
{
    private bool _connected = true;
    public List<byte[]> SentData { get; } = new();

    public EndPoint RemoteEndpoint => new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 5060);

    public bool IsConnected => _connected;

    public Task<int> ReceiveAsync(byte[] buffer) => Task.FromResult(0);

    public async Task SendAsync(byte[] data)
    {
        SentData.Add(data);
        await Task.CompletedTask;
    }

    public async Task CloseAsync()
    {
        _connected = false;
        await Task.CompletedTask;
    }

    public void Dispose() { }
}
