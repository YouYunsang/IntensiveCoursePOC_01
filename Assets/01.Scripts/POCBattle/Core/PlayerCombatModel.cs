using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Mutable player combat and upgradeable turn-resource state persisted across all stages in one run.
    /// </summary>
    public sealed class PlayerCombatModel
    {
        /// <summary>Maximum health copied from base stats.</summary>
        private readonly int _maxHealth;

        /// <summary>Current remaining health.</summary>
        private int _currentHealth;

        /// <summary>Current temporary shield.</summary>
        private int _shield;

        /// <summary>Upgradeable maximum moves available each player turn.</summary>
        private int _maxMoveCount;

        /// <summary>Upgradeable maximum placement edits available each player turn.</summary>
        private int _maxEditCount;

        /// <summary>Gets maximum health.</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>Gets current health.</summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>Gets current shield.</summary>
        public int Shield => _shield;

        /// <summary>Gets upgradeable moves per turn.</summary>
        public int MaxMoveCount => _maxMoveCount;

        /// <summary>Gets upgradeable edits per turn.</summary>
        public int MaxEditCount => _maxEditCount;

        /// <summary>Gets whether the player is still alive.</summary>
        public bool IsAlive => _currentHealth > 0;

        /// <summary>
        /// Creates runtime combat state from immutable player base data.
        /// </summary>
        public PlayerCombatModel(PlayerBaseStatsSO baseStats)
        {
            _maxHealth = baseStats.MaxHealth;
            _currentHealth = _maxHealth;
            _shield = 0;
            _maxMoveCount = baseStats.BaseMoveCount;
            _maxEditCount = baseStats.BaseEditCount;
        }

        /// <summary>
        /// Clears stage-transient combat values while preserving HP and upgraded per-turn stats between stages.
        /// </summary>
        public void ResetTransientForNewStage()
        {
            _shield = 0;
        }

        /// <summary>
        /// Clears remaining shield at the beginning of the player's next turn.
        /// </summary>
        public void ResetShieldForOwnTurn()
        {
            _shield = 0;
        }

        /// <summary>
        /// Adds shield that persists until the next player turn begins.
        /// </summary>
        public void AddShield(int amount)
        {
            _shield += Mathf.Max(0, amount);
        }

        /// <summary>
        /// Restores HP immediately without exceeding maximum health.
        /// </summary>
        public void Heal(int amount)
        {
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + Mathf.Max(0, amount));
        }

        /// <summary>
        /// Applies normal enemy damage through shield before HP.
        /// </summary>
        public void ApplyDamage(int amount)
        {
            int remainingDamage = Mathf.Max(0, amount);
            int absorbedDamage = Mathf.Min(_shield, remainingDamage);
            _shield -= absorbedDamage;
            remainingDamage -= absorbedDamage;
            _currentHealth = Mathf.Max(0, _currentHealth - remainingDamage);
        }

        /// <summary>
        /// Applies immediate trap damage directly to HP during movement.
        /// </summary>
        public void ApplyPureDamage(int amount)
        {
            _currentHealth = Mathf.Max(0, _currentHealth - Mathf.Max(0, amount));
        }

        /// <summary>
        /// Changes the maximum move count for future turns, supporting later upgrades.
        /// </summary>
        public void ModifyMaxMoveCount(int delta)
        {
            _maxMoveCount = Mathf.Max(1, _maxMoveCount + delta);
        }

        /// <summary>
        /// Changes the maximum edit count for future turns, supporting later upgrades.
        /// </summary>
        public void ModifyMaxEditCount(int delta)
        {
            _maxEditCount = Mathf.Max(0, _maxEditCount + delta);
        }
    }
}
