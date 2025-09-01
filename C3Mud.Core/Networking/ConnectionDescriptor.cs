using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;

namespace C3Mud.Core.Networking;

/// <summary>
/// Represents a client connection descriptor that maps to the original C descriptor_data struct
/// </summary>
public class ConnectionDescriptor : IConnectionDescriptor, IDisposable
{
    internal readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly ITelnetProtocolHandler _telnetHandler;
    private readonly object _sendLock = new();
    private readonly StringBuilder _currentInputBuffer = new();
    private volatile bool _disposed = false;

    public string Id { get; }
    public int SocketDescriptor { get; }
    public string Host { get; }
    public DateTime ConnectedAt { get; }
    public ConnectionState ConnectionState { get; private set; }
    public bool IsConnected => !_disposed && _tcpClient.Connected && _stream != null;
    public string CurrentInput => _currentInputBuffer.ToString();
    public string LastInput { get; private set; } = string.Empty;
    public bool EchoEnabled { get; private set; } = true;

    public ConnectionDescriptor(TcpClient tcpClient, ITelnetProtocolHandler telnetHandler, string? host = null)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        _telnetHandler = telnetHandler ?? throw new ArgumentNullException(nameof(telnetHandler));
        
        // Only get stream if client is connected
        _stream = _tcpClient.Connected ? _tcpClient.GetStream() : null!;
        
        Id = Guid.NewGuid().ToString();
        SocketDescriptor = _tcpClient.Client?.Handle.ToInt32() ?? 0;
        Host = host ?? _tcpClient.Client?.RemoteEndPoint?.ToString()?.Split(':')[0] ?? "unknown";
        ConnectedAt = DateTime.UtcNow;
        ConnectionState = ConnectionState.GetName;

        // Send initial telnet negotiation if connected
        if (_tcpClient.Connected)
        {
            SendInitialNegotiation();
        }
    }

    public async Task SendAsync(string data, CancellationToken cancellationToken = default)
    {
        if (_disposed || !IsConnected || _stream == null)
        {
            // Silently ignore sends to closed connections
            return;
        }

        try
        {
            var processedData = _telnetHandler.ProcessOutgoingData(data, this);
            
            lock (_sendLock)
            {
                if (_disposed || !IsConnected || _stream == null)
                    return;

                _ = _stream.WriteAsync(processedData, cancellationToken);
            }
        }
        catch (Exception)
        {
            // Connection might have been closed, ignore the error
            await CloseAsync();
        }
    }

    public async Task SendWithColorAsync(string colorCode, string data, CancellationToken cancellationToken = default)
    {
        var coloredMessage = $"{colorCode}{data}&N"; // Add normal color at end
        await SendAsync(coloredMessage, cancellationToken);
    }

    public async Task CloseAsync()
    {
        if (_disposed)
            return;

        ConnectionState = ConnectionState.Closing;
        
        try
        {
            _stream?.Close();
            _tcpClient?.Close();
        }
        catch
        {
            // Ignore close errors
        }
        
        ConnectionState = ConnectionState.Closed;
        _disposed = true;
    }

    public void AppendInput(string input)
    {
        if (!string.IsNullOrEmpty(input))
        {
            _currentInputBuffer.Append(input);
        }
    }

    public string ExtractCompleteInput()
    {
        var input = _currentInputBuffer.ToString();
        if (input.Contains('\n') || input.Contains('\r'))
        {
            LastInput = input.Trim('\r', '\n');
            _currentInputBuffer.Clear();
            return LastInput;
        }
        return string.Empty;
    }

    public void SetConnectionState(ConnectionState newState)
    {
        ConnectionState = newState;
    }

    public void SetEcho(bool enabled)
    {
        EchoEnabled = enabled;
        var echoSequence = _telnetHandler.SetEcho(enabled);
        
        try
        {
            lock (_sendLock)
            {
                if (!_disposed && IsConnected && _stream != null)
                {
                    _stream.Write(echoSequence);
                }
            }
        }
        catch
        {
            // Ignore echo setting errors
        }
    }

    private void SendInitialNegotiation()
    {
        try
        {
            var negotiation = _telnetHandler.GetInitialNegotiation();
            lock (_sendLock)
            {
                if (!_disposed && IsConnected && _stream != null)
                {
                    _stream.Write(negotiation);
                }
            }
        }
        catch
        {
            // Ignore initial negotiation errors
        }
    }

    public void Dispose()
    {
        CloseAsync().Wait(1000);
        _stream?.Dispose();
        _tcpClient?.Dispose();
    }
}