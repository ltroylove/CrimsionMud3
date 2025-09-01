using C3Mud.Core.Players;

namespace C3Mud.Core.Commands.Basic;

/// <summary>
/// Score command - shows player statistics and status
/// Based on original do_score() function from act.informative.c
/// </summary>
public class ScoreCommand : BaseCommand
{
    public override string Name => "score";
    public override string[] Aliases => new[] { "sc", "stat" };
    public override PlayerPosition MinimumPosition => PlayerPosition.Sleeping;
    public override int MinimumLevel => 1;

    public override async Task ExecuteAsync(IPlayer player, string arguments, int commandId)
    {
        await ShowScore(player);
    }

    private static async Task ShowScore(IPlayer player)
    {
        var scoreDisplay = $@"&W
{player.Name}'s Character Statistics:
=====================================

&YName:&N     {player.Name}
&YLevel:&N    {player.Level}
&YPosition:&N {GetPositionName(player.Position)}

&GHit Points:&N    100/100   (&GFull health&N)
&BMana Points:&N   50/50     (&BFull mana&N)  
&YMove Points:&N   100/100   (&YFull moves&N)

&RExperience:&N    0
&CAlignment:&N     Neutral (0)

&WConnection Status:&N {(player.IsConnected ? "&GConnected&N" : "&RDisconnected&N")}

&KNote: Full character statistics will be available once the
character system is fully implemented.&N";

        await SendToPlayerAsync(player, scoreDisplay, formatted: true);
    }

    private static string GetPositionName(PlayerPosition position)
    {
        return position switch
        {
            PlayerPosition.Dead => "&rDead&N",
            PlayerPosition.MortallyWounded => "&rMortally wounded&N",
            PlayerPosition.Incapacitated => "&rIncapacitated&N",
            PlayerPosition.Stunned => "&yStunned&N",
            PlayerPosition.Sleeping => "&bSleeping&N",
            PlayerPosition.Resting => "&cResting&N",
            PlayerPosition.Sitting => "&WSitting&N",
            PlayerPosition.Fighting => "&RFighting&N",
            PlayerPosition.Standing => "&GStanding&N",
            _ => "&KUnknown&N"
        };
    }
}