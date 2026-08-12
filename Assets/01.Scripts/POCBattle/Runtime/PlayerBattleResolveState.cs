using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Automatically converts collected attack/critical/shield effects into player battle results.
    /// </summary>
    public sealed class PlayerBattleResolveState : BattleStateBase
    {
        /// <summary>Creates automatic player battle resolution state.</summary>
        public PlayerBattleResolveState(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
            : base(context, presentationSettings, eventChannel, publisher)
        {
        }

        /// <summary>
        /// Damages the front living enemy, publishes defeat/gold/death presentation exactly once, grants shield, and schedules next phase.
        /// </summary>
        public override void Enter()
        {
            int finalDamage = Context.TurnEffects.CalculateFinalDamage(PresentationSettings.CriticalMultiplier);
            EnemyCombatModel targetEnemy = Context.EnemyParty.GetFrontAliveEnemy();
            bool defeatedEnemyThisResolution = false;

            if (targetEnemy != null && finalDamage > 0)
            {
                bool wasAlive = targetEnemy.IsAlive;
                targetEnemy.ApplyDamage(finalDamage);
                EventChannel.RaiseEnemyHitVisualRequested(targetEnemy.PartyIndex);
                Publisher.PublishEnemyStatus(targetEnemy);

                if (wasAlive && !targetEnemy.IsAlive)
                {
                    defeatedEnemyThisResolution = true;
                    EventChannel.RaiseEnemyDefeated(targetEnemy.PartyIndex, targetEnemy.Definition.GoldReward);
                    EventChannel.RaiseEnemyDeathVisualRequested(targetEnemy.PartyIndex);
                }
            }

            if (Context.TurnEffects.PendingShield > 0)
            {
                Context.Player.AddShield(Context.TurnEffects.PendingShield);
            }

            Publisher.PublishPlayerStatus();
            Publisher.PublishTurnEffects();

            bool battleWon = Context.EnemyParty.AreAllDefeated();
            BattlePhase nextPhase = battleWon ? BattlePhase.Victory : BattlePhase.EnemyTurn;
            float delay = PresentationSettings.PlayerResolveDelay;
            if (battleWon && defeatedEnemyThisResolution)
            {
                delay = Mathf.Max(delay, PresentationSettings.EnemyDeathDuration);
            }

            RequestTransition(nextPhase, delay);
        }
    }
}
