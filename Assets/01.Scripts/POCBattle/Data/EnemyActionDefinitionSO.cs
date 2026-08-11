using UnityEngine;

namespace PocBattle.Data
{
    /// <summary>
    /// Intent categories used by both preview presentation and enemy action execution data.
    /// </summary>
    public enum EnemyIntentType
    {
        Attack = 0,
        Shield = 1
    }

    /// <summary>
    /// Immutable preview data produced from the exact action asset that will later execute.
    /// </summary>
    public readonly struct EnemyIntentPart
    {
        /// <summary>Intent category.</summary>
        private readonly EnemyIntentType _type;

        /// <summary>Displayed numeric value.</summary>
        private readonly int _value;

        /// <summary>Gets intent category.</summary>
        public EnemyIntentType Type => _type;

        /// <summary>Gets preview amount.</summary>
        public int Value => _value;

        /// <summary>
        /// Creates one immutable intent part.
        /// </summary>
        public EnemyIntentPart(EnemyIntentType type, int value)
        {
            _type = type;
            _value = value;
        }
    }

    /// <summary>
    /// Receives enemy action effects without making data assets depend on runtime model classes.
    /// </summary>
    public interface IEnemyActionReceiver
    {
        /// <summary>Deals damage from one enemy to the player.</summary>
        void DamagePlayer(int enemyIndex, int amount);

        /// <summary>Grants shield to one enemy.</summary>
        void GrantEnemyShield(int enemyIndex, int amount);
    }

    /// <summary>
    /// Base action definition used for both next-turn preview and actual enemy execution.
    /// </summary>
    public abstract class EnemyActionDefinitionSO : ScriptableObject
    {
        /// <summary>
        /// Creates preview data from the same values that execution will use.
        /// </summary>
        public abstract EnemyIntentPart BuildIntent();

        /// <summary>
        /// Executes this enemy action against mutable battle state.
        /// </summary>
        public abstract void Execute(int enemyIndex, IEnemyActionReceiver receiver);
    }
}
