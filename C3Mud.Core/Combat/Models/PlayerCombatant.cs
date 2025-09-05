using C3Mud.Core.Players;

namespace C3Mud.Core.Combat.Models;

/// <summary>
/// Combatant wrapper for player characters
/// </summary>
public class PlayerCombatant : ICombatant
{
    private readonly IPlayer _player;
    private static readonly Random _random = new Random();

    public PlayerCombatant(IPlayer player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
    }

    public string Name => _player.Name;

    public int HitPoints => _player.HitPoints;

    public int MaxHitPoints => _player.MaxHitPoints;

    public int Initiative => _player.Dexterity + _random.Next(1, 11); // Dex + 1d10

    public bool IsAlive => _player.HitPoints > 0 && _player.Position != PlayerPosition.Dead;

    public bool CanAct => IsAlive && _player.Position >= PlayerPosition.Stunned;

    public ICombatant? CurrentTarget { get; set; }

    public void SetTarget(ICombatant? target)
    {
        CurrentTarget = target;
    }

    public async Task ApplyDamageAsync(int damage)
    {
        _player.HitPoints = Math.Max(0, _player.HitPoints - damage);
        
        if (_player.HitPoints <= 0)
        {
            _player.Position = PlayerPosition.Dead;
        }
        
        await Task.CompletedTask;
    }

    public async Task<CombatAction> GetNextActionAsync()
    {
        // For now, players always attack (AI would be more complex for NPCs)
        return await Task.FromResult(new CombatAction
        {
            ActionType = CombatActionType.Attack
        });
    }

    /// <summary>
    /// Get the underlying player
    /// </summary>
    public IPlayer Player => _player;
}