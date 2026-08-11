using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Mutable combat state for one enemy while its definition remains immutable.
    /// </summary>
    public sealed class EnemyCombatModel
    {
        /// <summary>Immutable enemy definition.</summary>
        private readonly EnemyDefinitionSO _definition;

        /// <summary>Stable party index used for front-target order and presentation.</summary>
        private readonly int _partyIndex;

        /// <summary>Current HP.</summary>
        private int _currentHealth;

        /// <summary>Current temporary shield.</summary>
        private int _shield;

        /// <summary>Current deterministic pattern step index.</summary>
        private int _patternIndex;

        /// <summary>Gets enemy definition.</summary>
        public EnemyDefinitionSO Definition => _definition;

        /// <summary>Gets party index.</summary>
        public int PartyIndex => _partyIndex;

        /// <summary>Gets current health.</summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>Gets current shield.</summary>
        public int Shield => _shield;

        /// <summary>Gets current pattern index.</summary>
        public int PatternIndex => _patternIndex;

        /// <summary>Gets whether this enemy is alive.</summary>
        public bool IsAlive => _currentHealth > 0;

        /// <summary>
        /// Creates runtime enemy state at full health and pattern step zero.
        /// </summary>
        public EnemyCombatModel(EnemyDefinitionSO definition, int partyIndex)
        {
            _definition = definition;
            _partyIndex = partyIndex;
            _currentHealth = definition.MaxHealth;
            _shield = 0;
            _patternIndex = 0;
        }

        /// <summary>
        /// Clears shield at the beginning of this enemy's own next turn.
        /// </summary>
        public void ResetShieldForOwnTurn()
        {
            _shield = 0;
        }

        /// <summary>
        /// Adds temporary enemy shield.
        /// </summary>
        public void AddShield(int amount)
        {
            _shield += Mathf.Max(0, amount);
        }

        /// <summary>
        /// Applies player damage through shield before HP.
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
        /// Returns the deterministic pattern step that will execute on the next enemy turn.
        /// </summary>
        public EnemyPatternStep GetCurrentPatternStep()
        {
            return _definition.Pattern != null ? _definition.Pattern.GetStep(_patternIndex) : null;
        }

        /// <summary>
        /// Advances to the next deterministic pattern step after execution.
        /// </summary>
        public void AdvancePattern()
        {
            if (_definition.Pattern == null || _definition.Pattern.StepCount == 0)
            {
                return;
            }

            _patternIndex++;
            _patternIndex = _definition.Pattern.ResolveStepIndex(_patternIndex);
        }
    }
}
