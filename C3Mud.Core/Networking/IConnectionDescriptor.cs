namespace C3Mud.Core.Networking;

/// <summary>
/// Represents a client connection descriptor that maps to the original C descriptor_data struct
/// </summary>
public interface IConnectionDescriptor
{
    /// <summary>
    /// Unique identifier for this connection
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Socket descriptor/handle for this connection
    /// </summary>
    int SocketDescriptor { get; }
    
    /// <summary>
    /// Client's hostname or IP address
    /// </summary>
    string Host { get; }
    
    /// <summary>
    /// When this connection was established
    /// </summary>
    DateTime ConnectedAt { get; }
    
    /// <summary>
    /// Current connection state (login, playing, etc.)
    /// </summary>
    ConnectionState ConnectionState { get; }
    
    /// <summary>
    /// Whether this connection is active
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Current input buffer for this connection
    /// </summary>
    string CurrentInput { get; }
    
    /// <summary>
    /// Last input received from this connection
    /// </summary>
    string LastInput { get; }
    
    /// <summary>
    /// Whether the connection has telnet echo enabled
    /// </summary>
    bool EchoEnabled { get; }
    
    /// <summary>
    /// Whether this connection has pending input to process
    /// </summary>
    bool HasPendingInput { get; }
    
    /// <summary>
    /// Telnet protocol handler for this connection
    /// </summary>
    ITelnetProtocolHandler TelnetHandler { get; }
    
    /// <summary>
    /// Send data to this connection asynchronously
    /// </summary>
    Task SendAsync(string data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send data to this connection
    /// </summary>
    Task SendDataAsync(string data);
    
    /// <summary>
    /// Send data with color codes to this connection
    /// </summary>
    Task SendWithColorAsync(string colorCode, string data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Read input from this connection
    /// </summary>
    Task<string> ReadInputAsync();
    
    /// <summary>
    /// Close this connection gracefully
    /// </summary>
    Task CloseAsync();
}

public enum ConnectionState
{
    /// <summary>
    /// Initial connection, waiting for name
    /// </summary>
    GetName = 0,
    
    /// <summary>
    /// Waiting for password
    /// </summary>
    GetPassword = 1,
    
    /// <summary>
    /// New player creation
    /// </summary>
    NewPlayerCreation = 2,
    
    /// <summary>
    /// Player is actively playing
    /// </summary>
    Playing = 3,
    
    /// <summary>
    /// Connection is being closed
    /// </summary>
    Closing = 4,
    
    /// <summary>
    /// Connection closed
    /// </summary>
    Closed = 5
}