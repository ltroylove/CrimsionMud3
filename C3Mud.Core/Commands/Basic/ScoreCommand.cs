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
        var playerData = player.LegacyPlayerFileData;
        
        if (!playerData.HasValue)
        {
            // If no legacy data, show basic info
            var basicDisplay = $@"&W
{player.Name}'s Character Statistics:
=====================================

&YName:&N     {player.Name}
&YLevel:&N    {player.Level}
&YPosition:&N {GetPositionName(player.Position)}

&GHit Points:&N    {player.HitPoints}/{player.MaxHitPoints}
&RExperience:&N    {player.ExperiencePoints}
&wGold:&N         {player.Gold}

&WConnection Status:&N {(player.IsConnected ? "&GConnected&N" : "&RDisconnected&N")}&N";

            await SendToPlayerAsync(player, basicDisplay, formatted: true);
            return;
        }

        var data = playerData.Value;
        var points = data.Points;
        
        // Format alignment description
        var alignmentText = GetAlignmentText(data.Alignment);
        
        // Format hit point status
        var hitStatus = GetVitalStatus(points.Hit, points.MaxHit, "health");
        var manaStatus = GetVitalStatus(points.Mana, points.MaxMana, "mana");
        var moveStatus = GetVitalStatus(points.Move, points.MaxMove, "moves");
        
        var scoreDisplay = $@"&W
{player.Name}'s Character Statistics:
=====================================

&YName:&N     {player.Name}
&YLevel:&N    {player.Level}
&YPosition:&N {GetPositionName(player.Position)}

&GHit Points:&N    {points.Hit}/{points.MaxHit}   {hitStatus}
&BMana Points:&N   {points.Mana}/{points.MaxMana}     {manaStatus}  
&YMove Points:&N   {points.Move}/{points.MaxMove}   {moveStatus}

&RExperience:&N    {points.Experience}
&CAlignment:&N     {alignmentText} ({data.Alignment})
&YArmor Class:&N   {points.Armor}
&wGold:&N         {points.Gold}
&WBank:&N         {points.Bank}

&WConnection Status:&N {(player.IsConnected ? "&GConnected&N" : "&RDisconnected&N")}&N";

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
    
    private static string GetAlignmentText(int alignment)
    {
        return alignment switch
        {
            >= 350 => "&WSaintly&N",
            >= 100 => "&wGood&N",
            >= 50 => "&WKindly&N",
            >= -49 => "&KNeutral&N",
            >= -99 => "&rMean&N",
            >= -349 => "&REvil&N",
            _ => "&rSatanic&N"
        };
    }
    
    private static string GetVitalStatus(short current, short max, string type)
    {
        var percentage = max > 0 ? (double)current / max : 0;
        
        return percentage switch
        {
            >= 1.0 => $"(&GFull {type}&N)",
            >= 0.8 => $"(&gGood {type}&N)",
            >= 0.6 => $"(&YFair {type}&N)",
            >= 0.4 => $"(&yPoor {type}&N)",
            >= 0.2 => $"(&rBad {type}&N)",
            _ => $"(&RAwful {type}&N)"
        };
    }
}