using Drongo.Core.Transport;
using System.Net;
using Xunit;

namespace Drongo.Core.Tests.Transport;

/// <summary>
/// Tests for TCP listener connection management.
/// Block 1: Accept TCP connections, maintain per-connection state, handle lifecycle.
/// </summary>
public class TcpListenerTests
{
    [Fact]
    public async Task AcceptConnection_ShouldAcceptIncomingTcpConnection()
    {
        // Arrange
        var listener = new TcpListener(port: 0); // 0 = any available port
        await listener.StartAsync();
        
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        
        // Act - connect from client
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        
        var connection = await listener.AcceptConnectionAsync();
        
        // Assert
        Assert.NotNull(connection);
        Assert.True(connection.IsConnected);
        
        // Cleanup
        connection.Dispose();
        listener.Dispose();
    }

    [Fact]
    public async Task ConnectionState_ShouldMaintainPerConnectionReceiveBuffer()
    {
        // Arrange
        var listener = new TcpListener(port: 0);
        await listener.StartAsync();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        
        // Act
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        
        var connection = await listener.AcceptConnectionAsync();
        
        // Send data from client
        var testData = System.Text.Encoding.UTF8.GetBytes("INVITE sip:bob@example.com SIP/2.0\r\n");
        await client.GetStream().WriteAsync(testData, 0, testData.Length);
        await client.GetStream().FlushAsync();
        
        // Read from connection
        var buffer = new byte[256];
        var bytesRead = await connection.ReceiveAsync(buffer);
        
        // Assert
        Assert.True(bytesRead > 0);
        var received = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Assert.Equal("INVITE sip:bob@example.com SIP/2.0\r\n", received);
        
        // Cleanup
        connection.Dispose();
        listener.Dispose();
    }

    [Fact]
    public async Task ConnectionLifecycle_ShouldHandleConnectAndClose()
    {
        // Arrange
        var listener = new TcpListener(port: 0);
        await listener.StartAsync();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        
        // Act - connect
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync("127.0.0.1", port);
        
        var connection = await listener.AcceptConnectionAsync();
        Assert.True(connection.IsConnected);
        
        // Close client
        client.Close();
        
        // Try to read - should get 0 bytes (EOF) or throw
        var buffer = new byte[256];
        var bytesRead = await connection.ReceiveAsync(buffer);
        
        // Assert
        Assert.Equal(0, bytesRead); // EOF
        Assert.False(connection.IsConnected);
        
        // Cleanup
        connection.Dispose();
        listener.Dispose();
    }

    [Fact]
    public async Task StopAsync_ShouldGracefullyShutdownListener()
    {
        // Arrange
        var listener = new TcpListener(port: 0);
        await listener.StartAsync();
        
        // Act
        await listener.StopAsync();
        
        // Assert - should not be able to accept new connections
        await Assert.ThrowsAsync<System.InvalidOperationException>(
            async () => await listener.AcceptConnectionAsync()
        );
        
        // Cleanup
        listener.Dispose();
    }
}
