using System;
using System.Collections.Generic;
using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// Owns ordered runtime enemies and front-target selection for future 1:N expansion.
    /// </summary>
    public sealed class EnemyPartyModel
    {
        /// <summary>Enemies ordered from front target to back target.</summary>
        private readonly List<EnemyCombatModel> _enemies;

        /// <summary>Gets ordered enemies.</summary>
        public IReadOnlyList<EnemyCombatModel> Enemies => _enemies;

        /// <summary>
        /// Creates one runtime enemy model per encounter definition.
        /// </summary>
        public EnemyPartyModel(EncounterDefinitionSO encounter)
        {
            if (encounter == null)
            {
                throw new ArgumentNullException(nameof(encounter));
            }

            _enemies = new List<EnemyCombatModel>(encounter.Enemies.Count);
            for (int enemyIndex = 0; enemyIndex < encounter.Enemies.Count; enemyIndex++)
            {
                EnemyDefinitionSO definition = encounter.Enemies[enemyIndex];
                if (definition != null)
                {
                    _enemies.Add(new EnemyCombatModel(definition, _enemies.Count));
                }
            }

            if (_enemies.Count == 0)
            {
                throw new InvalidOperationException("Encounter must contain at least one valid enemy definition.");
            }
        }

        /// <summary>
        /// Returns the first living enemy in configured front-to-back order.
        /// </summary>
        public EnemyCombatModel GetFrontAliveEnemy()
        {
            for (int enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
            {
                if (_enemies[enemyIndex].IsAlive)
                {
                    return _enemies[enemyIndex];
                }
            }

            return null;
        }

        /// <summary>
        /// Gets an enemy by stable party index.
        /// </summary>
        public EnemyCombatModel GetEnemy(int partyIndex)
        {
            return partyIndex >= 0 && partyIndex < _enemies.Count ? _enemies[partyIndex] : null;
        }

        /// <summary>
        /// Returns true only when no enemy remains alive.
        /// </summary>
        public bool AreAllDefeated()
        {
            return GetFrontAliveEnemy() == null;
        }

        /// <summary>
        /// Resets every living enemy shield at the beginning of the enemy turn.
        /// </summary>
        public void ResetLivingEnemyShields()
        {
            for (int enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
            {
                if (_enemies[enemyIndex].IsAlive)
                {
                    _enemies[enemyIndex].ResetShieldForOwnTurn();
                }
            }
        }

        /// <summary>
        /// Advances every living enemy pattern after its current action step has executed.
        /// </summary>
        public void AdvanceLivingPatterns()
        {
            for (int enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
            {
                if (_enemies[enemyIndex].IsAlive)
                {
                    _enemies[enemyIndex].AdvancePattern();
                }
            }
        }
    }
}
