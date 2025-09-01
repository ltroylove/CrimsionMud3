namespace C3Mud.Core.Networking;

/// <summary>
/// Manages connection lifecycle, rate limiting, and DDoS protection
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Current number of active connections
    /// </summary>
    int ActiveConnectionCount { get; }
    
    /// <summary>
    /// Maximum allowed connections
    /// </summary>
    int MaxConnections { get; }
    
    /// <summary>
    /// Add a new connection to management
    /// </summary>
    Task<bool> AddConnectionAsync(IConnectionDescriptor connection);
    
    /// <summary>
    /// Remove a connection from management
    /// </summary>
    Task RemoveConnectionAsync(string connectionId);
    
    /// <summary>
    /// Get connection by ID
    /// </summary>
    IConnectionDescriptor? GetConnection(string connectionId);
    
    /// <summary>
    /// Get all active connections
    /// </summary>
    IReadOnlyList<IConnectionDescriptor> GetAllConnections();
    
    /// <summary>
    /// Check if a new connection from this IP should be allowed
    /// </summary>
    bool ShouldAllowConnection(string hostAddress);
    
    /// <summary>
    /// Record command activity for rate limiting
    /// </summary>
    void RecordActivity(string connectionId, string command);
    
    /// <summary>
    /// Record response time for performance monitoring
    /// </summary>
    void RecordResponseTime(string connectionId, TimeSpan responseTime);
    
    /// <summary>
    /// Check if connection is rate limited
    /// </summary>
    bool IsRateLimited(string connectionId);
    
    /// <summary>
    /// Clean up idle/stale connections
    /// </summary>
    Task CleanupConnectionsAsync();
    
    /// <summary>
    /// Get connection statistics
    /// </summary>
    ConnectionStatistics GetStatistics();
}

public class ConnectionStatistics
{
    public int ActiveConnections { get; set; }
    public int TotalConnectionsToday { get; set; }
    public int RateLimitedConnections { get; set; }
    public int BlockedConnections { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public Dictionary<string, int> ConnectionsByHost { get; set; } = new();
}