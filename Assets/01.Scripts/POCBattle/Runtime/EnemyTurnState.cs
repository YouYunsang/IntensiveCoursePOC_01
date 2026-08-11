using PocBattle.Core;
using PocBattle.Data;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Executes all living enemies' deterministic current pattern step and publishes the next intent afterward.
    /// </summary>
    public sealed class EnemyTurnState : BattleStateBase
    {
        /// <summary>
        /// Creates enemy turn state.
        /// </summary>
        public EnemyTurnState(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
            : base(context, presentationSettings, eventChannel, publisher)
        {
        }

        /// <summary>
        /// Resets enemy shields, executes current actions, advances patterns, and schedules the next player turn or defeat.
        /// </summary>
        public override void Enter()
        {
            int playerHealthBeforeTurn = Context.Player.CurrentHealth;
            Context.EnemyTurnResolver.ExecuteEnemyTurn();
            Publisher.PublishPlayerStatus();
            Publisher.PublishAllEnemyPresentation();

            if (Context.Player.CurrentHealth < playerHealthBeforeTurn)
            {
                EventChannel.RaisePlayerHitVisualRequested();
            }

            BattlePhase nextPhase = Context.Player.IsAlive ? BattlePhase.PlayerTurnSetup : BattlePhase.Defeat;
            RequestTransition(nextPhase, PresentationSettings.EnemyResolveDelay);
        }
    }
}
