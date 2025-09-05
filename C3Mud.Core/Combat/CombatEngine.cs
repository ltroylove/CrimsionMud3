using C3Mud.Core.Combat.Models;
using C3Mud.Core.Players;
using C3Mud.Core.World.Services;
using C3Mud.Core.World.Models;
using C3Mud.Core.Characters;
using C3Mud.Core.Characters.Models;

namespace C3Mud.Core.Combat;

/// <summary>
/// Implementation of the core combat engine based on CircleMUD/DikuMUD mechanics
/// Uses THAC0 system for hit calculations and standard damage formulas
/// </summary>
public class CombatEngine : ICombatEngine
{
    private readonly IWorldDatabase _worldDatabase;
    private readonly Random _random = new Random();

    public CombatEngine(IWorldDatabase worldDatabase)
    {
        _worldDatabase = worldDatabase ?? throw new ArgumentNullException(nameof(worldDatabase));
    }

    public async Task<bool> InitiateCombatAsync(IPlayer attacker, IPlayer defender)
    {
        // Check if attacker is not standing
        if (attacker.Position != PlayerPosition.Standing)
        {
            await attacker.SendMessageAsync("You must be standing to attack!");
            return false;
        }

        // Check if target is in the same room
        if (attacker.CurrentRoomVnum != defender.CurrentRoomVnum)
        {
            await attacker.SendMessageAsync("They aren't here.");
            return false;
        }

        // Check if target is already dead
        if (defender.HitPoints <= 0 || defender.Position == PlayerPosition.Dead)
        {
            await attacker.SendMessageAsync("They are already dead!");
            return false;
        }

        // Send combat messages
        await attacker.SendMessageAsync($"You attack {defender.Name}!");
        await defender.SendMessageAsync($"{attacker.Name} attacks you!");

        return true;
    }

    public Task<HitResult> CalculateHitAsync(ICharacter attacker, ICharacter defender, int? forceDiceRoll = null)
    {
        // Calculate THAC0 based on level (CircleMUD formula: 20 - (level-1))
        int thac0 = Math.Max(1, 20 - (attacker.Level - 1));
        
        // Mobs get +2 to hit bonus (original: if (IS_NPC(ch)) calc_thaco -= 2;)
        if (attacker.IsNPC)
        {
            thac0 -= 2;
        }
        
        // Calculate strength bonus to hit
        int strengthBonus = GetStrengthHitBonus(attacker.Strength);
        
        // Roll d20 (or use forced roll for testing)
        int diceRoll = forceDiceRoll ?? _random.Next(1, 21);
        
        // Critical hit/miss checks
        bool isCriticalHit = diceRoll == 20;
        bool isCriticalMiss = diceRoll == 1;
        
        // Calculate if hit succeeds
        // Need to roll (THAC0 - AC - bonuses) or less on d20 to hit
        int targetNumber = thac0 - defender.ArmorClass - strengthBonus;
        bool isHit = isCriticalHit || (!isCriticalMiss && diceRoll <= targetNumber);
        
        return Task.FromResult(new HitResult
        {
            IsHit = isHit,
            IsCriticalHit = isCriticalHit,
            IsCriticalMiss = isCriticalMiss,
            AttackerTHAC0 = thac0,
            StrengthBonus = strengthBonus,
            DiceRoll = diceRoll,
            TargetArmorClass = defender.ArmorClass
        });
    }
    
    private int GetStrengthHitBonus(int strength)
    {
        return strength switch
        {
            >= 18 => 1,   // Strength 18+ gives +1 to hit
            >= 16 => 0,   // Strength 16-17 gives no bonus
            >= 13 => 0,   // Strength 13-15 gives no bonus  
            >= 9 => 0,    // Strength 9-12 gives no bonus
            >= 6 => -1,   // Strength 6-8 gives -1 to hit
            _ => -2       // Strength 3-5 gives -2 to hit
        };
    }

    public Task<DamageResult> CalculateDamageAsync(ICharacter attacker, ICharacter defender, bool isCritical)
    {
        var weapon = attacker.GetWieldedWeapon();
        
        int baseDamage;
        int weaponBonus = 0;
        string weaponName;
        
        if (weapon != null && weapon.ObjectType == ObjectType.WEAPON)
        {
            // Weapon damage: Values[0] = dice sides, Values[1] = dice count, Values[2] = bonus
            int diceSides = weapon.Values[0];
            int diceCount = weapon.Values[1];
            weaponBonus = weapon.Values[2];
            weaponName = weapon.Name;
            
            // Roll weapon damage dice
            baseDamage = 0;
            for (int i = 0; i < diceCount; i++)
            {
                baseDamage += _random.Next(1, diceSides + 1);
            }
        }
        else if (attacker.IsMob && attacker is Characters.Mobile mob)
        {
            // Mob natural attacks - use damage dice from template
            var damageDice = mob.GetDamageDice();
            int diceCount = damageDice.count;
            int diceSides = damageDice.sides;
            baseDamage = 0;
            for (int i = 0; i < diceCount; i++)
            {
                baseDamage += _random.Next(1, diceSides + 1);
            }
            weaponName = "natural attacks";
        }
        else
        {
            // Unarmed combat - bare hands do 1d2 damage
            baseDamage = _random.Next(1, 3);
            weaponName = "bare hands";
        }
        
        // Calculate strength damage bonus
        int strengthBonus = GetStrengthDamageBonus(attacker.Strength);
        
        // Calculate total damage
        int totalDamage = baseDamage + strengthBonus + weaponBonus;
        
        // Apply critical hit multiplier
        if (isCritical)
        {
            totalDamage *= 2;
        }
        
        // Minimum damage is 1
        totalDamage = Math.Max(1, totalDamage);
        
        return Task.FromResult(new DamageResult
        {
            BaseDamage = baseDamage,
            StrengthBonus = strengthBonus,
            WeaponBonus = weaponBonus,
            TotalDamage = totalDamage,
            IsCritical = isCritical,
            WeaponName = weaponName
        });
    }
    
    private int GetStrengthDamageBonus(int strength)
    {
        return strength switch
        {
            >= 18 => 2,   // Strength 18+ gives +2 damage
            >= 16 => 1,   // Strength 16-17 gives +1 damage
            >= 13 => 0,   // Strength 13-15 gives no bonus
            >= 9 => 0,    // Strength 9-12 gives no bonus
            >= 6 => -1,   // Strength 6-8 gives -1 damage
            _ => -2       // Strength 3-5 gives -2 damage
        };
    }

    public async Task<CombatRoundResult> ExecuteCombatRoundAsync(List<ICombatant> combatants)
    {
        // Sort combatants by dexterity for initiative order (highest first)
        var initiativeOrder = combatants.OrderByDescending(c => c.Initiative).ToList();
        
        var attackResults = new List<AttackResult>();
        bool combatEnded = false;
        
        // Process each combatant's attack in initiative order
        foreach (var combatant in initiativeOrder)
        {
            if (combatEnded) break;
            if (!combatant.CanAct) continue;
            
            // Get or assign target using proper targeting logic
            var target = GetValidTarget(combatant, combatants);
            if (target == null) continue;
            
            // Get the underlying characters (works for both players and mobs)
            var attackerChar = ((CharacterCombatant)combatant).Character;
            var targetChar = ((CharacterCombatant)target).Character;
            
            // Calculate hit
            var hitResult = await CalculateHitAsync(attackerChar, targetChar);
            
            AttackResult attackResult;
            
            if (hitResult.IsHit)
            {
                // Calculate damage
                var damageResult = await CalculateDamageAsync(attackerChar, targetChar, hitResult.IsCriticalHit);
                
                // Apply damage
                await target.ApplyDamageAsync(damageResult.TotalDamage);
                
                // Check for death
                if (target.HitPoints <= 0)
                {
                    combatEnded = true;
                }
                
                attackResult = new AttackResult
                {
                    Attacker = combatant,
                    Target = target,
                    Hit = hitResult,
                    Damage = damageResult
                };
            }
            else
            {
                // Miss
                attackResult = new AttackResult
                {
                    Attacker = combatant,
                    Target = target,
                    Hit = hitResult,
                    Damage = null
                };
            }
            
            attackResults.Add(attackResult);
        }
        
        return new CombatRoundResult
        {
            RoundNumber = 1,
            InitiativeOrder = initiativeOrder,
            AttackResults = attackResults,
            CombatEnded = combatEnded
        };
    }

    public async Task<bool> AttemptFleeAsync(IPlayer player)
    {
        // Check if player is in combat (simplified check - if they have < max HP, assume in combat)
        if (!player.IsInCombat)
        {
            await player.SendMessageAsync("You aren't fighting anyone!");
            return false;
        }
        
        // Get current room to check for exits
        var room = _worldDatabase.GetRoom(player.CurrentRoomVnum);
        if (room == null || !room.Exits.Any())
        {
            await player.SendMessageAsync("You can't escape!");
            return false;
        }
        
        // Simple flee success calculation (based on dexterity)
        int fleeChance = Math.Min(95, 50 + (player.Dexterity - 13) * 5);
        bool fleeSuccessful = _random.Next(1, 101) <= fleeChance;
        
        if (fleeSuccessful)
        {
            // Pick a random exit to flee through
            var availableExits = room.GetAvailableExits();
            var randomExit = availableExits[_random.Next(availableExits.Count)];
            var exit = room.GetExit(randomExit);
            
            if (exit != null)
            {
                // Move player to the exit destination
                player.CurrentRoomVnum = exit.TargetRoomVnum;
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Get a valid target for the combatant using proper targeting logic
    /// </summary>
    /// <param name="attacker">The combatant looking for a target</param>
    /// <param name="allCombatants">All combatants in the fight</param>
    /// <returns>Valid target or null if none available</returns>
    private ICombatant? GetValidTarget(ICombatant attacker, List<ICombatant> allCombatants)
    {
        // First, check if current target is still valid
        if (attacker.CurrentTarget != null && 
            IsValidTarget(attacker.CurrentTarget, allCombatants))
        {
            return attacker.CurrentTarget;
        }

        // Current target is invalid, find a new one
        var availableTargets = allCombatants
            .Where(c => c != attacker && IsValidTarget(c, allCombatants))
            .ToList();

        if (!availableTargets.Any())
        {
            attacker.SetTarget(null);
            return null;
        }

        // Targeting priority logic:
        // 1. Target someone who is already attacking this combatant (counter-attack)
        // 2. Target someone who doesn't have anyone attacking them (spread damage)
        // 3. Target the weakest opponent (lowest HP percentage)
        
        // Priority 1: Counter-attack
        var counterTarget = availableTargets.FirstOrDefault(t => t.CurrentTarget == attacker);
        if (counterTarget != null)
        {
            attacker.SetTarget(counterTarget);
            return counterTarget;
        }

        // Priority 2: Target someone not being attacked
        var unengagedTargets = availableTargets
            .Where(t => !allCombatants.Any(c => c.CurrentTarget == t))
            .ToList();
        
        if (unengagedTargets.Any())
        {
            var target = unengagedTargets
                .OrderBy(t => (double)t.HitPoints / t.MaxHitPoints) // Weakest first
                .First();
            attacker.SetTarget(target);
            return target;
        }

        // Priority 3: Target the weakest available opponent
        var weakestTarget = availableTargets
            .OrderBy(t => (double)t.HitPoints / t.MaxHitPoints)
            .First();
        
        attacker.SetTarget(weakestTarget);
        return weakestTarget;
    }

    /// <summary>
    /// Check if a combatant is a valid target
    /// </summary>
    /// <param name="target">Potential target</param>
    /// <param name="allCombatants">All combatants in the fight</param>
    /// <returns>True if target is valid</returns>
    private bool IsValidTarget(ICombatant target, List<ICombatant> allCombatants)
    {
        return allCombatants.Contains(target) && 
               target.IsAlive && 
               target.HitPoints > 0;
    }
}