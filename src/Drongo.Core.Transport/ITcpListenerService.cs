using System.Net;

namespace Drongo.Core.Transport;

/// <summary>
/// Listens for incoming TCP connections and accepts them.
/// Block 1: Connection Management - Accept TCP connections, maintain per-connection state, handle lifecycle.
/// </summary>
public interface ITcpListenerService : IDisposable
{
    /// <summary>
    /// Gets the local endpoint this listener is bound to.
    /// </summary>
    EndPoint LocalEndpoint { get; }

    /// <summary>
    /// Starts listening for incoming connections.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stops listening and closes all connections.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Accepts the next incoming connection.
    /// Throws InvalidOperationException if listener is not started or has been stopped.
    /// </summary>
    Task<ITcpConnection> AcceptConnectionAsync();
}
