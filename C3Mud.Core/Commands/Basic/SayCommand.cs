using C3Mud.Core.Players;
using C3Mud.Core.Services;

namespace C3Mud.Core.Commands.Basic;

/// <summary>
/// Say command - allows players to communicate with others in the same room
/// Based on original do_say() function from act.comm.c
/// </summary>
public class SayCommand : BaseCommand
{
    private static IRoomPlayerManager? _roomPlayerManager;
    
    /// <summary>
    /// Set the room player manager for dependency injection
    /// In a full implementation, this would be injected via constructor
    /// </summary>
    public static void SetRoomPlayerManager(IRoomPlayerManager roomPlayerManager)
    {
        _roomPlayerManager = roomPlayerManager;
    }
    
    public override string Name => "say";
    public override string[] Aliases => new[] { "'" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Resting;
    public override int MinimumLevel => 1;

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            await SendToPlayerAsync(player, "Say what?");
            return;
        }

        // Format the message for the speaker
        var speakerMessage = $"&YYou say, &W'{arguments}'&N";
        await SendToPlayerAsync(player, speakerMessage, formatted: true);

        // Broadcast to all other players in the room
        if (_roomPlayerManager != null)
        {
            var playersInRoom = _roomPlayerManager.GetPlayersInRoom(player.CurrentRoomVnum);
            var roomMessage = $"&Y{player.Name} says, &W'{arguments}'&N";
            
            foreach (var otherPlayer in playersInRoom)
            {
                if (otherPlayer != player && otherPlayer.IsConnected)
                {
                    await SendToPlayerAsync(otherPlayer, roomMessage, formatted: true);
                }
            }
        }
    }
}