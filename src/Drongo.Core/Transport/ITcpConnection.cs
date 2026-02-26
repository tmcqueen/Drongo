using System.Net;

namespace Drongo.Core.Transport;

/// <summary>
/// Represents an active TCP connection for receiving/sending SIP messages.
/// </summary>
public interface ITcpConnection : IDisposable
{
    /// <summary>
    /// Gets the remote endpoint address.
    /// </summary>
    EndPoint RemoteEndpoint { get; }

    /// <summary>
    /// Gets whether the connection is still active.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Receives data from the connection.
    /// </summary>
    /// <param name="buffer">Buffer to read into</param>
    /// <returns>Number of bytes read, 0 on EOF</returns>
    Task<int> ReceiveAsync(byte[] buffer);

    /// <summary>
    /// Sends data on the connection.
    /// </summary>
    Task SendAsync(byte[] data);

    /// <summary>
    /// Closes the connection.
    /// </summary>
    Task CloseAsync();
}
