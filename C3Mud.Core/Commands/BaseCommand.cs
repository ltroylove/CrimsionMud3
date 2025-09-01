using C3Mud.Core.Players;

namespace C3Mud.Core.Commands;

/// <summary>
/// Base class for all MUD commands
/// Provides common functionality and default implementations
/// </summary>
public abstract class BaseCommand : ICommand
{
    public abstract string Name { get; }
    public virtual string[] Aliases => Array.Empty<string>();
    public virtual PlayerPosition MinimumPosition => PlayerPosition.Standing;
    public virtual int MinimumLevel => 1;
    public virtual bool AllowMob => false;
    public virtual bool IsEnabled => true;

    public abstract Task ExecuteAsync(IPlayer player, string arguments, int commandId);

    /// <summary>
    /// Send a message to the player with optional formatting
    /// </summary>
    protected static async Task SendToPlayerAsync(IPlayer player, string message, bool formatted = false)
    {
        if (formatted)
            await player.SendFormattedMessageAsync(message);
        else
            await player.SendMessageAsync(message);
    }

    /// <summary>
    /// Validate that the player can execute this command
    /// </summary>
    protected bool CanExecute(IPlayer player)
    {
        if (!IsEnabled)
            return false;
            
        if (player.Level < MinimumLevel)
            return false;
            
        if (player.Position < MinimumPosition)
            return false;
            
        return true;
    }

    /// <summary>
    /// Get the position description for error messages
    /// </summary>
    protected static string GetPositionDescription(PlayerPosition position)
    {
        return position switch
        {
            PlayerPosition.Dead => "dead",
            PlayerPosition.MortallyWounded => "mortally wounded",
            PlayerPosition.Incapacitated => "incapacitated",
            PlayerPosition.Stunned => "stunned",
            PlayerPosition.Sleeping => "sleeping",
            PlayerPosition.Resting => "resting",
            PlayerPosition.Sitting => "sitting",
            PlayerPosition.Fighting => "fighting",
            PlayerPosition.Standing => "standing",
            _ => "in an unknown position"
        };
    }
}