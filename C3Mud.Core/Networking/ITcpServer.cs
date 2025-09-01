namespace C3Mud.Core.Networking;

/// <summary>
/// Represents the main TCP server that accepts connections, based on original comm.c functionality
/// </summary>
public interface ITcpServer
{
    /// <summary>
    /// Current server status
    /// </summary>
    ServerStatus Status { get; }
    
    /// <summary>
    /// Port the server is listening on
    /// </summary>
    int Port { get; }
    
    /// <summary>
    /// Number of currently connected clients
    /// </summary>
    int ActiveConnections { get; }
    
    /// <summary>
    /// Maximum number of concurrent connections allowed
    /// </summary>
    int MaxConnections { get; }
    
    /// <summary>
    /// Start the server listening on the specified port
    /// </summary>
    Task StartAsync(int port, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop the server gracefully, closing all connections
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event raised when a new client connects
    /// </summary>
    event EventHandler<ConnectionEventArgs> ClientConnected;
    
    /// <summary>
    /// Event raised when a client disconnects
    /// </summary>
    event EventHandler<ConnectionEventArgs> ClientDisconnected;
    
    /// <summary>
    /// Event raised when data is received from a client
    /// </summary>
    event EventHandler<DataReceivedEventArgs> DataReceived;
    
    /// <summary>
    /// Get all active connections
    /// </summary>
    IReadOnlyList<IConnectionDescriptor> GetActiveConnections();
    
    /// <summary>
    /// Get a specific connection by ID
    /// </summary>
    IConnectionDescriptor? GetConnection(string connectionId);
    
    /// <summary>
    /// Close a specific connection
    /// </summary>
    Task CloseConnectionAsync(string connectionId);
    
    /// <summary>
    /// Send data to all connected clients
    /// </summary>
    Task BroadcastAsync(string message, CancellationToken cancellationToken = default);
}

public enum ServerStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Error
}

public class ConnectionEventArgs : EventArgs
{
    public IConnectionDescriptor Connection { get; }
    
    public ConnectionEventArgs(IConnectionDescriptor connection)
    {
        Connection = connection;
    }
}

public class DataReceivedEventArgs : EventArgs
{
    public IConnectionDescriptor Connection { get; }
    public string Data { get; }
    
    public DataReceivedEventArgs(IConnectionDescriptor connection, string data)
    {
        Connection = connection;
        Data = data;
    }
}