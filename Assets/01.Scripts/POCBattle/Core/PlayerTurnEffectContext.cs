using System;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Stores only effects collected during the current player movement phase.
    /// </summary>
    public sealed class PlayerTurnEffectContext : IBlockEffectReceiver
    {
        /// <summary>Player model used only for immediate trap damage.</summary>
        private readonly PlayerCombatModel _player;

        /// <summary>Accumulated attack for the current turn.</summary>
        private int _attack;

        /// <summary>Shield waiting to be granted during player battle resolution.</summary>
        private int _pendingShield;

        /// <summary>Number of repeated x1.5 critical multipliers collected this turn.</summary>
        private int _criticalStacks;

        /// <summary>Gets accumulated attack.</summary>
        public int Attack => _attack;

        /// <summary>Gets pending shield.</summary>
        public int PendingShield => _pendingShield;

        /// <summary>Gets critical stack count.</summary>
        public int CriticalStacks => _criticalStacks;

        /// <summary>
        /// Creates a current-turn effect accumulator.
        /// </summary>
        public PlayerTurnEffectContext(PlayerCombatModel player)
        {
            _player = player;
        }

        /// <summary>
        /// Clears all temporary collected effects at the start of each player turn.
        /// </summary>
        public void Reset()
        {
            _attack = 0;
            _pendingShield = 0;
            _criticalStacks = 0;
        }

        /// <summary>
        /// Adds attack from a collected attack block.
        /// </summary>
        public void AddAttack(int amount)
        {
            _attack += Mathf.Max(0, amount);
        }

        /// <summary>
        /// Adds shield that will be granted during battle resolution.
        /// </summary>
        public void AddPendingShield(int amount)
        {
            _pendingShield += Mathf.Max(0, amount);
        }

        /// <summary>
        /// Adds one or more repeated critical multiplier stacks.
        /// </summary>
        public void AddCriticalStacks(int amount)
        {
            _criticalStacks += Mathf.Max(0, amount);
        }

        /// <summary>
        /// Applies trap damage immediately to player HP.
        /// </summary>
        public void ApplyImmediateDamage(int amount)
        {
            _player.ApplyPureDamage(amount);
        }

        /// <summary>
        /// Calculates final attack using repeated critical multiplication and rounds to the nearest integer.
        /// </summary>
        public int CalculateFinalDamage(float criticalMultiplier)
        {
            if (_attack <= 0)
            {
                return 0;
            }

            float multiplier = Mathf.Pow(criticalMultiplier, _criticalStacks);
            double rawDamage = _attack * multiplier;
            return (int)Math.Round(rawDamage, MidpointRounding.AwayFromZero);
        }
    }
}
