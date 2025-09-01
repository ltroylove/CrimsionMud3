using C3Mud.Core.Networking;

namespace C3Mud.Core.Players;

/// <summary>
/// Basic player implementation
/// Based on original char_data structure from structs.h
/// </summary>
public class Player : IPlayer
{
    public string Id { get; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public PlayerPosition Position { get; set; } = PlayerPosition.Standing;
    public bool IsConnected => Connection?.IsConnected ?? false;
    public IConnectionDescriptor? Connection { get; set; }

    public Player(string id)
    {
        Id = id;
    }

    public Player(string id, IConnectionDescriptor connection) : this(id)
    {
        Connection = connection;
    }

    public async Task SendMessageAsync(string message)
    {
        if (Connection != null && IsConnected)
        {
            await Connection.SendDataAsync(message + "\r\n");
        }
    }

    public async Task SendFormattedMessageAsync(string message)
    {
        if (Connection != null && IsConnected)
        {
            // Process color codes through the telnet handler
            var processedMessage = Connection.TelnetHandler.ProcessColorCodes(message, Connection);
            await Connection.SendDataAsync(processedMessage + "\r\n");
        }
    }

    public async Task DisconnectAsync(string reason = "Goodbye!")
    {
        if (Connection != null && IsConnected)
        {
            await SendMessageAsync(reason);
            await Connection.CloseAsync();
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is Player other && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return $"Player[{Id}]: {Name} (Level {Level})";
    }
}