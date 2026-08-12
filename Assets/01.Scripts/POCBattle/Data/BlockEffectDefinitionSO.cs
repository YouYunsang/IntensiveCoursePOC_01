using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Receives block effects without forcing data assets to depend on battle runtime implementations.
    /// </summary>
    public interface IBlockEffectReceiver
    {
        /// <summary>Adds attack power to the current player turn.</summary>
        void AddAttack(int amount);

        /// <summary>Adds shield that will be granted during the automatic battle phase.</summary>
        void AddPendingShield(int amount);

        /// <summary>Adds one or more critical multiplier stacks.</summary>
        void AddCriticalStacks(int amount);

        /// <summary>Immediately reduces player HP without waiting for battle resolution.</summary>
        void ApplyImmediateDamage(int amount);

        /// <summary>Immediately restores player HP without exceeding maximum HP.</summary>
        void ApplyImmediateHeal(int amount);
    }

    /// <summary>
    /// Base ScriptableObject for composable block effects.
    /// </summary>
    public abstract class BlockEffectDefinitionSO : ScriptableObject
    {
        /// <summary>
        /// Applies this effect through the provided runtime receiver.
        /// </summary>
        /// <param name="receiver">Runtime receiver that owns mutable combat state.</param>
        public abstract void Apply(IBlockEffectReceiver receiver);
    }
}
