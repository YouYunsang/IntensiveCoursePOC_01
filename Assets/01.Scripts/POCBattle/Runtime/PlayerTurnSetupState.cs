using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

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
            Context.CellEffectPlacementService.ClearForNewTurn();
            Context.PlacementService.PlaceAllBlocksForNewTurn();
            Context.CellEffectPlacementService.PlaceAllForNewTurn();
            Publisher.PublishPlayerStatus();
            Publisher.PublishTurnResources();
            Publisher.PublishTurnEffects();
            Publisher.PublishBlockLayout(true);
            Publisher.PublishCellEffectLayout();
            Publisher.PublishPlayerPositionSync();
            // Keep edit input disabled until the longest board placement tween reaches authoritative cell centers.
            float layoutSettleDuration = Mathf.Max(PresentationSettings.BlockMoveDuration, PresentationSettings.BlockDropDuration);
            RequestTransition(BattlePhase.PlacementEdit, layoutSettleDuration);
        }
    }
}
