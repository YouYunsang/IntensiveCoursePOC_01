using PocBattle.Core;
using PocBattle.Data;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Resets player-turn resources, clears player shield, and re-randomizes every deck/trap block.
    /// </summary>
    public sealed class PlayerTurnSetupState : BattleStateBase
    {
        /// <summary>
        /// Creates player turn setup state.
        /// </summary>
        public PlayerTurnSetupState(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
            : base(context, presentationSettings, eventChannel, publisher)
        {
        }

        /// <summary>
        /// Executes all deterministic player-turn-start rules then enters placement edit.
        /// </summary>
        public override void Enter()
        {
            Context.Player.ResetShieldForOwnTurn();
            Context.TurnEffects.Reset();
            Context.ResetTurnResources();
            Context.PlacementService.PlaceAllBlocksForNewTurn();
            Publisher.PublishPlayerStatus();
            Publisher.PublishTurnResources();
            Publisher.PublishTurnEffects();
            Publisher.PublishBlockLayout(true);
            Publisher.PublishPlayerPositionSync();
            // Keep edit input disabled until the shuffle presentation reaches authoritative cell centers.
            RequestTransition(BattlePhase.PlacementEdit, PresentationSettings.BlockMoveDuration);
        }
    }
}
