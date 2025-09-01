using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace C3Mud.Core.Networking;

/// <summary>
/// Main TCP server implementation that accepts connections, based on original comm.c functionality
/// </summary>
public class TcpServer : ITcpServer, IDisposable
{
    private readonly IConnectionManager _connectionManager;
    private readonly ITelnetProtocolHandler _telnetHandler;
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _acceptTask;
    private readonly ConcurrentDictionary<string, Task> _clientTasks = new();
    private volatile bool _disposed = false;

    public ServerStatus Status { get; private set; } = ServerStatus.Stopped;
    public int Port { get; private set; }
    public int ActiveConnections => _connectionManager.ActiveConnectionCount;
    public int MaxConnections => _connectionManager.MaxConnections;

    public event EventHandler<ConnectionEventArgs>? ClientConnected;
    public event EventHandler<ConnectionEventArgs>? ClientDisconnected;
    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    public TcpServer(IConnectionManager? connectionManager = null, ITelnetProtocolHandler? telnetHandler = null)
    {
        _connectionManager = connectionManager ?? new ConnectionManager();
        _telnetHandler = telnetHandler ?? new TelnetProtocolHandler();
    }

    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (Status == ServerStatus.Running)
        {
            throw new InvalidOperationException("Server is already running");
        }

        try
        {
            Status = ServerStatus.Starting;
            Port = port;

            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token).Token;

            _acceptTask = AcceptClientsAsync(combinedToken);

            Status = ServerStatus.Running;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            Status = ServerStatus.Error;
            throw new InvalidOperationException($"Port {port} is already in use", ex);
        }
        catch (Exception)
        {
            Status = ServerStatus.Error;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Status != ServerStatus.Running)
        {
            return;
        }

        Status = ServerStatus.Stopping;

        try
        {
            // Stop accepting new connections
            _cancellationTokenSource?.Cancel();
            _tcpListener?.Stop();

            // Wait for accept task to complete
            if (_acceptTask != null)
            {
                try
                {
                    await _acceptTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when canceling
                }
            }

            // Close all active connections
            var connections = GetActiveConnections();
            var closeTasks = connections.Select(conn => _connectionManager.RemoveConnectionAsync(conn.Id));
            await Task.WhenAll(closeTasks);

            // Wait for client tasks to complete
            var clientTasksArray = _clientTasks.Values.ToArray();
            await Task.WhenAll(clientTasksArray.Select(async task =>
            {
                try
                {
                    await task;
                }
                catch
                {
                    // Ignore client task exceptions during shutdown
                }
            }));

            _clientTasks.Clear();
            Status = ServerStatus.Stopped;
        }
        catch (Exception)
        {
            Status = ServerStatus.Error;
            throw;
        }
    }

    public IReadOnlyList<IConnectionDescriptor> GetActiveConnections()
    {
        return _connectionManager.GetAllConnections();
    }

    public IConnectionDescriptor? GetConnection(string connectionId)
    {
        return _connectionManager.GetConnection(connectionId);
    }

    public async Task CloseConnectionAsync(string connectionId)
    {
        await _connectionManager.RemoveConnectionAsync(connectionId);
    }

    public async Task BroadcastAsync(string message, CancellationToken cancellationToken = default)
    {
        var connections = GetActiveConnections();
        var tasks = connections.Select(conn => conn.SendAsync(message, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _tcpListener != null)
        {
            try
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                
                if (cancellationToken.IsCancellationRequested)
                {
                    tcpClient.Close();
                    break;
                }

                // Handle client connection in background task
                var clientTask = HandleClientAsync(tcpClient, cancellationToken);
                var taskId = Guid.NewGuid().ToString();
                _clientTasks[taskId] = clientTask;

                // Clean up completed tasks
                _ = clientTask.ContinueWith(_ => _clientTasks.TryRemove(taskId, out _), TaskScheduler.Default);
            }
            catch (ObjectDisposedException)
            {
                // Listener was stopped
                break;
            }
            catch (InvalidOperationException)
            {
                // Listener was stopped
                break;
            }
            catch (SocketException)
            {
                // Network error, continue accepting
                continue;
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        ConnectionDescriptor? connection = null;

        try
        {
            var clientEndpoint = tcpClient.Client.RemoteEndPoint?.ToString();
            var hostAddress = clientEndpoint?.Split(':')[0] ?? "unknown";

            // Check if connection should be allowed
            if (!_connectionManager.ShouldAllowConnection(hostAddress))
            {
                tcpClient.Close();
                return;
            }

            // Create connection descriptor
            connection = new ConnectionDescriptor(tcpClient, _telnetHandler, hostAddress);

            // Add to connection manager
            var added = await _connectionManager.AddConnectionAsync(connection);
            if (!added)
            {
                await connection.CloseAsync();
                return;
            }

            // Raise connection event
            ClientConnected?.Invoke(this, new ConnectionEventArgs(connection));

            // Handle client data
            await HandleClientDataAsync(connection, cancellationToken);
        }
        catch (Exception)
        {
            // Connection error occurred
        }
        finally
        {
            if (connection != null)
            {
                ClientDisconnected?.Invoke(this, new ConnectionEventArgs(connection));
                await _connectionManager.RemoveConnectionAsync(connection.Id);
            }

            tcpClient.Close();
        }
    }

    private async Task HandleClientDataAsync(ConnectionDescriptor connection, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var stream = connection._tcpClient.GetStream();

        try
        {
            while (connection.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                
                if (bytesRead == 0)
                {
                    // Client disconnected
                    break;
                }

                // Process incoming data through telnet handler
                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);

                var result = _telnetHandler.ProcessIncomingData(data, connection);

                // Send any negotiation responses
                if (result.NegotiationResponse != null)
                {
                    await stream.WriteAsync(result.NegotiationResponse, cancellationToken);
                }

                // Handle complete commands
                if (result.IsComplete && !string.IsNullOrEmpty(result.Text))
                {
                    connection.AppendInput(result.Text);
                    var completeInput = connection.ExtractCompleteInput();
                    
                    if (!string.IsNullOrEmpty(completeInput))
                    {
                        // Record activity for rate limiting
                        _connectionManager.RecordActivity(connection.Id, completeInput);

                        // Check rate limiting
                        if (!_connectionManager.IsRateLimited(connection.Id))
                        {
                            // Raise data received event
                            DataReceived?.Invoke(this, new DataReceivedEventArgs(connection, completeInput));
                        }
                        else
                        {
                            // Send rate limit message
                            await connection.SendAsync("Please slow down - you are being rate limited.\r\n");
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(result.Text))
                {
                    // Incomplete command, buffer it
                    connection.AppendInput(result.Text);
                }

                // Check if connection should be closed
                if (result.ShouldClose)
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            // Connection error, will be cleaned up in finally block
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            StopAsync().Wait(5000); // Wait up to 5 seconds for graceful shutdown
        }
        catch
        {
            // Ignore shutdown errors during dispose
        }

        _tcpListener?.Stop();
        _cancellationTokenSource?.Dispose();
        
        if (_connectionManager is IDisposable disposableManager)
        {
            disposableManager.Dispose();
        }
    }
}