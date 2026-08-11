using System.Collections.Generic;
using PocBattle.Data;

namespace PocBattle.Core
{
    /// <summary>
    /// Executes deterministic enemy pattern steps through a narrow action receiver.
    /// </summary>
    public sealed class EnemyTurnResolver : IEnemyActionReceiver
    {
        /// <summary>Player damaged by enemy attacks.</summary>
        private readonly PlayerCombatModel _player;

        /// <summary>Ordered enemy party used for shield actions.</summary>
        private readonly EnemyPartyModel _enemyParty;

        /// <summary>
        /// Creates an enemy turn resolver for one battle runtime.
        /// </summary>
        public EnemyTurnResolver(PlayerCombatModel player, EnemyPartyModel enemyParty)
        {
            _player = player;
            _enemyParty = enemyParty;
        }

        /// <summary>
        /// Resets enemy shields, executes each living enemy's current step, then advances patterns.
        /// </summary>
        public void ExecuteEnemyTurn()
        {
            _enemyParty.ResetLivingEnemyShields();
            IReadOnlyList<EnemyCombatModel> enemies = _enemyParty.Enemies;

            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                EnemyCombatModel enemy = enemies[enemyIndex];
                if (!enemy.IsAlive || !_player.IsAlive)
                {
                    continue;
                }

                EnemyPatternStep step = enemy.GetCurrentPatternStep();
                if (step == null)
                {
                    continue;
                }

                IReadOnlyList<EnemyActionDefinitionSO> actions = step.Actions;
                for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
                {
                    EnemyActionDefinitionSO action = actions[actionIndex];
                    if (action != null && _player.IsAlive)
                    {
                        action.Execute(enemy.PartyIndex, this);
                    }
                }
            }

            _enemyParty.AdvanceLivingPatterns();
        }

        /// <summary>
        /// Applies normal attack damage to the player through shield first.
        /// </summary>
        public void DamagePlayer(int enemyIndex, int amount)
        {
            _player.ApplyDamage(amount);
        }

        /// <summary>
        /// Grants shield to the acting enemy if it still exists and is alive.
        /// </summary>
        public void GrantEnemyShield(int enemyIndex, int amount)
        {
            EnemyCombatModel enemy = _enemyParty.GetEnemy(enemyIndex);
            if (enemy != null && enemy.IsAlive)
            {
                enemy.AddShield(amount);
            }
        }
    }
}
