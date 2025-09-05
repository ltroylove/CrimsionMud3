using C3Mud.Core.Characters;
using C3Mud.Core.Players;

namespace C3Mud.Core.Combat.Models;

/// <summary>
/// Unified combatant wrapper for both players and mobs
/// Based on original unified char_data approach from Crimson-2-MUD
/// </summary>
public class CharacterCombatant : ICombatant
{
    private readonly ICharacter _character;
    private static readonly Random _random = new Random();

    public CharacterCombatant(ICharacter character)
    {
        _character = character ?? throw new ArgumentNullException(nameof(character));
    }

    public string Name => _character.Name;

    public int HitPoints => _character.HitPoints;

    public int MaxHitPoints => _character.MaxHitPoints;

    public int Initiative => _character.Dexterity + _random.Next(1, 11); // Dex + 1d10

    public bool IsAlive => _character.HitPoints > 0 && _character.Position != PlayerPosition.Dead;

    public bool CanAct => IsAlive && _character.Position >= PlayerPosition.Stunned;

    public ICombatant? CurrentTarget { get; set; }

    public void SetTarget(ICombatant? target)
    {
        CurrentTarget = target;
        
        // Update the underlying character's fighting reference
        if (target is CharacterCombatant charTarget)
        {
            _character.Fighting = charTarget._character;
        }
        else
        {
            _character.Fighting = null;
        }
    }

    public async Task ApplyDamageAsync(int damage)
    {
        _character.HitPoints = Math.Max(0, _character.HitPoints - damage);
        
        if (_character.HitPoints <= 0)
        {
            _character.Position = PlayerPosition.Dead;
        }
        
        await Task.CompletedTask;
    }

    public async Task<CombatAction> GetNextActionAsync()
    {
        // Players always attack (AI would be more complex for NPCs)
        // TODO: Add mob AI logic here for different attack types
        return await Task.FromResult(new CombatAction
        {
            ActionType = CombatActionType.Attack
        });
    }

    /// <summary>
    /// Get the underlying character
    /// </summary>
    public ICharacter Character => _character;
    
    /// <summary>
    /// Check if this is a player character
    /// </summary>
    public bool IsPlayer => _character.IsPC;
    
    /// <summary>
    /// Check if this is a mobile (NPC)
    /// </summary>
    public bool IsMobile => _character.IsMob;
}