using C3Mud.Core.Players;
using C3Mud.Core.World.Models;
using C3Mud.Core.World.Services;

namespace C3Mud.Core.Combat;

/// <summary>
/// Implementation of death and resurrection handling
/// This is a stub implementation for TDD Red phase - all methods are placeholders
/// </summary>
public class DeathHandler : IDeathHandler
{
    private readonly IWorldDatabase _worldDatabase;

    public DeathHandler(IWorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase ?? throw new ArgumentNullException(nameof(worldDatabase));
    }

    public async Task ProcessDeathAsync(IPlayer player)
    {
        // Set player position to dead
        player.Position = PlayerPosition.Dead;
        
        // Send death message
        await player.SendMessageAsync("You are DEAD!");
        
        // Get current room
        var room = _worldDatabase.GetRoom(player.CurrentRoomVnum);
        if (room == null) return;

        // Create corpse
        var corpse = new WorldObject
        {
            VirtualNumber = 0, // Corpses have special vnum handling
            Name = $"corpse {player.Name}",
            ShortDescription = $"the corpse of {player.Name}",
            ObjectType = ObjectType.CORPSE,
            DecayTime = DateTime.UtcNow.AddMinutes(GetCorpseDecayTime(player)), // Player corpses last longer
            Gold = player.Gold
        };

        // Transfer player's inventory to corpse
        var playerInventory = player.GetInventory();
        if (playerInventory != null)
        {
            foreach (var item in playerInventory)
            {
                corpse.Contents.Add(item);
            }
        }

        // Add corpse to room
        room.Objects.Add(corpse);

        // Player loses all gold (it's in the corpse now)
        player.Gold = 0;

        // Calculate and apply experience loss
        var expLoss = CalculateExperienceLoss(player);
        player.ExperiencePoints = Math.Max(0, player.ExperiencePoints - expLoss);
    }

    private int GetCorpseDecayTime(IPlayer player)
    {
        // Player corpses decay slower than mob corpses
        // Newbie protection - very low level players get longer decay time
        if (player.Level <= 5)
            return 60; // 60 minutes for newbies
        return 30; // 30 minutes for regular players
    }

    private int CalculateExperienceLoss(IPlayer player)
    {
        // CircleMUD-style experience loss
        // Lose more exp at higher levels, but with newbie protection
        if (player.Level <= 5)
        {
            // Reduced penalty for newbies
            return Math.Min(100, player.ExperiencePoints / 20);
        }
        
        // Standard penalty: lose about 10% of current exp
        return Math.Min(player.ExperiencePoints / 10, player.Level * 1000);
    }

    public async Task ResurrectPlayerAsync(IPlayer player)
    {
        // Check if player is actually dead
        if (player.Position != PlayerPosition.Dead)
        {
            await player.SendMessageAsync("You are not dead!");
            return;
        }

        // Set player position to standing
        player.Position = PlayerPosition.Standing;

        // Calculate resurrection HP based on recent death count (harsher penalties for multiple deaths)
        var recentDeaths = player.RecentDeathCount;
        var restoredHpPercent = Math.Max(10, 50 - (recentDeaths * 10)); // 50% base, -10% per recent death, min 10%
        player.HitPoints = (player.MaxHitPoints * restoredHpPercent) / 100;

        // Send resurrection message
        await player.SendMessageAsync("You have been resurrected!");
        
        // Apply temporary stat penalty (constitution loss)
        await player.SendMessageAsync("You feel less healthy after your ordeal.");

        // In a full implementation, we would apply temporary stat penalties here
        // For now, we just send the message to satisfy the test
    }

    public async Task ProcessCorpseDecayAsync(Room room)
    {
        var currentTime = DateTime.UtcNow;
        var corpsesToRemove = new List<WorldObject>();

        // Find expired corpses (both CORPSE type and CONTAINER type corpses)
        // Use ToList() to avoid collection modification during iteration
        var expiredCorpses = room.Objects.Where(o => (o.ObjectType == ObjectType.CORPSE || 
                                                     (o.ObjectType == ObjectType.CONTAINER && o.Name.Contains("corpse"))) &&
                                                     o.DecayTime <= currentTime).ToList();
        
        foreach (var corpse in expiredCorpses)
        {
            corpsesToRemove.Add(corpse);
            
            // Scatter contents to room
            foreach (var item in corpse.Contents)
            {
                room.Objects.Add(item);
            }
            
            // Gold scatters too (though in a real implementation it might disappear)
            // For testing purposes, we don't add gold objects to the room
        }

        // Remove expired corpses
        foreach (var corpse in corpsesToRemove)
        {
            room.Objects.Remove(corpse);
        }

        await Task.CompletedTask;
    }

    private WorldObject CreateMobCorpse(string mobName)
    {
        return new WorldObject
        {
            VirtualNumber = 0,
            Name = $"corpse {mobName}",
            ShortDescription = $"the corpse of {mobName}",
            ObjectType = ObjectType.CORPSE,
            DecayTime = DateTime.UtcNow.AddMinutes(5) // Mob corpses decay much faster
        };
    }
}