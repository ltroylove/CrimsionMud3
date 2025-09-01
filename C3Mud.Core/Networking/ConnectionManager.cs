using System.Collections.Concurrent;
using System.Net;

namespace C3Mud.Core.Networking;

/// <summary>
/// Manages connection lifecycle, rate limiting, and DDoS protection
/// </summary>
public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, IConnectionDescriptor> _connections = new();
    private readonly ConcurrentDictionary<string, List<DateTime>> _connectionActivity = new();
    private readonly ConcurrentDictionary<string, int> _connectionsByHost = new();
    private readonly ConcurrentDictionary<string, List<TimeSpan>> _responseTimes = new();
    private readonly object _lockObject = new();
    
    private const int MaxConnectionsPerHost = 5;
    private const int RateLimitThreshold = 20; // Commands per minute
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StaleConnectionAge = TimeSpan.FromHours(1);

    public int ActiveConnectionCount => _connections.Count;
    public int MaxConnections { get; } = 250; // Support 250+ concurrent connections

    public async Task<bool> AddConnectionAsync(IConnectionDescriptor connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        // Check if we're at max connections
        if (ActiveConnectionCount >= MaxConnections)
        {
            return false;
        }

        // Check per-host connection limit
        if (!ShouldAllowConnection(connection.Host))
        {
            return false;
        }

        // Add connection
        lock (_lockObject)
        {
            if (_connections.TryAdd(connection.Id, connection))
            {
                // Track host connections
                _connectionsByHost.AddOrUpdate(
                    connection.Host,
                    1,
                    (host, count) => count + 1);

                // Initialize activity tracking
                _connectionActivity[connection.Id] = new List<DateTime>();
                
                return true;
            }
        }

        return false;
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            return;

        lock (_lockObject)
        {
            if (_connections.TryRemove(connectionId, out var connection))
            {
                // Update host count
                if (_connectionsByHost.TryGetValue(connection.Host, out var count))
                {
                    if (count <= 1)
                    {
                        _connectionsByHost.TryRemove(connection.Host, out _);
                    }
                    else
                    {
                        _connectionsByHost[connection.Host] = count - 1;
                    }
                }

                // Remove activity tracking
                _connectionActivity.TryRemove(connectionId, out _);

                // Close the connection if it's not already closed
                if (connection.IsConnected)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await connection.CloseAsync();
                        }
                        catch
                        {
                            // Ignore close errors
                        }
                    });
                }
            }
        }
    }

    public IConnectionDescriptor? GetConnection(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            return null;

        _connections.TryGetValue(connectionId, out var connection);
        return connection;
    }

    public IReadOnlyList<IConnectionDescriptor> GetAllConnections()
    {
        return _connections.Values.ToList();
    }

    public bool ShouldAllowConnection(string hostAddress)
    {
        if (string.IsNullOrEmpty(hostAddress))
            return false;

        // Always allow localhost
        if (hostAddress == "127.0.0.1" || hostAddress == "::1")
            return true;

        // Check per-host connection limit
        if (_connectionsByHost.TryGetValue(hostAddress, out var count))
        {
            return count < MaxConnectionsPerHost;
        }

        return true;
    }

    public void RecordActivity(string connectionId, string command)
    {
        if (string.IsNullOrEmpty(connectionId) || string.IsNullOrEmpty(command))
            return;

        var now = DateTime.UtcNow;
        
        if (_connectionActivity.TryGetValue(connectionId, out var activities))
        {
            lock (activities)
            {
                // Add current activity
                activities.Add(now);
                
                // Clean up old activities outside the rate limit window
                activities.RemoveAll(activity => now - activity > RateLimitWindow);
            }
        }
    }

    public void RecordResponseTime(string connectionId, TimeSpan responseTime)
    {
        if (string.IsNullOrEmpty(connectionId))
            return;

        var responseTimes = _responseTimes.GetOrAdd(connectionId, _ => new List<TimeSpan>());
        lock (responseTimes)
        {
            responseTimes.Add(responseTime);
            
            // Keep only last 100 response times per connection to prevent memory growth
            if (responseTimes.Count > 100)
            {
                responseTimes.RemoveAt(0);
            }
        }
    }

    public bool IsRateLimited(string connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            return false;

        if (_connectionActivity.TryGetValue(connectionId, out var activities))
        {
            lock (activities)
            {
                var now = DateTime.UtcNow;
                // Clean up old activities
                activities.RemoveAll(activity => now - activity > RateLimitWindow);
                
                // Check if over rate limit
                return activities.Count > RateLimitThreshold;
            }
        }

        return false;
    }

    public async Task CleanupConnectionsAsync()
    {
        var now = DateTime.UtcNow;
        var connectionsToRemove = new List<string>();

        // Find stale connections
        lock (_lockObject)
        {
            foreach (var kvp in _connections)
            {
                var connection = kvp.Value;
                
                if (!connection.IsConnected || 
                    (now - connection.ConnectedAt > StaleConnectionAge))
                {
                    connectionsToRemove.Add(kvp.Key);
                }
            }
        }

        // Remove stale connections
        foreach (var connectionId in connectionsToRemove)
        {
            await RemoveConnectionAsync(connectionId);
        }
    }

    public ConnectionStatistics GetStatistics()
    {
        var stats = new ConnectionStatistics
        {
            ActiveConnections = ActiveConnectionCount,
            ConnectionsByHost = new Dictionary<string, int>(_connectionsByHost),
            TotalConnectionsToday = ActiveConnectionCount, // Simplified for now
            RateLimitedConnections = 0,
            BlockedConnections = 0,
            AverageResponseTime = CalculateAverageResponseTime()
        };

        // Count rate limited connections
        foreach (var activities in _connectionActivity.Values)
        {
            lock (activities)
            {
                if (activities.Count > RateLimitThreshold)
                {
                    stats.RateLimitedConnections++;
                }
            }
        }

        return stats;
    }

    private TimeSpan CalculateAverageResponseTime()
    {
        var allResponseTimes = new List<TimeSpan>();
        
        foreach (var responseTimes in _responseTimes.Values)
        {
            lock (responseTimes)
            {
                allResponseTimes.AddRange(responseTimes);
            }
        }
        
        if (allResponseTimes.Count == 0)
        {
            return TimeSpan.FromMilliseconds(50); // Default when no data available
        }
        
        var averageTicks = (long)allResponseTimes.Average(t => t.Ticks);
        return TimeSpan.FromTicks(averageTicks);
    }
}