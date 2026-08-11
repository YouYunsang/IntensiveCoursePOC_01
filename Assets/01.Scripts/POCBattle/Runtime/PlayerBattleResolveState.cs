using PocBattle.Core;
using PocBattle.Data;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Automatically converts collected attack/critical/shield effects into player battle results.
    /// </summary>
    public sealed class PlayerBattleResolveState : BattleStateBase
    {
        /// <summary>
        /// Creates automatic player battle resolution state.
        /// </summary>
        public PlayerBattleResolveState(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
            : base(context, presentationSettings, eventChannel, publisher)
        {
        }

        /// <summary>
        /// Damages the front living enemy, grants pending shield, and schedules enemy turn or victory.
        /// </summary>
        public override void Enter()
        {
            int finalDamage = Context.TurnEffects.CalculateFinalDamage(PresentationSettings.CriticalMultiplier);
            EnemyCombatModel targetEnemy = Context.EnemyParty.GetFrontAliveEnemy();

            if (targetEnemy != null && finalDamage > 0)
            {
                targetEnemy.ApplyDamage(finalDamage);
                EventChannel.RaiseEnemyHitVisualRequested(targetEnemy.PartyIndex);
                Publisher.PublishEnemyStatus(targetEnemy);
            }

            if (Context.TurnEffects.PendingShield > 0)
            {
                Context.Player.AddShield(Context.TurnEffects.PendingShield);
            }

            Publisher.PublishPlayerStatus();
            Publisher.PublishTurnEffects();

            BattlePhase nextPhase = Context.EnemyParty.AreAllDefeated() ? BattlePhase.Victory : BattlePhase.EnemyTurn;
            RequestTransition(nextPhase, PresentationSettings.PlayerResolveDelay);
        }
    }
}
