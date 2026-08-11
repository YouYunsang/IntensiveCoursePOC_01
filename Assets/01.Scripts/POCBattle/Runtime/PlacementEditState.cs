using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Handles click-select placement editing, empty-cell moves, and optional editable-block swaps.
    /// </summary>
    public sealed class PlacementEditState : BattleStateBase
    {
        /// <summary>Sentinel used when no block is selected.</summary>
        private const int NO_SELECTED_BLOCK_ID = -1;

        /// <summary>Currently selected editable runtime block id.</summary>
        private int _selectedBlockId;

        /// <summary>
        /// Creates placement edit state.
        /// </summary>
        public PlacementEditState(
            BattleRuntimeContext context,
            BattleBoardSettingsSO boardSettings,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
            : base(context, presentationSettings, eventChannel, publisher)
        {
            BoardSettings = boardSettings;
            _selectedBlockId = NO_SELECTED_BLOCK_ID;
        }

        /// <summary>Board gameplay settings used for swap policy.</summary>
        private BattleBoardSettingsSO BoardSettings { get; }

        /// <summary>
        /// Clears stale selection every time edit phase begins.
        /// </summary>
        public override void Enter()
        {
            SetSelection(NO_SELECTED_BLOCK_ID);
            Publisher.PublishTurnResources();
        }

        /// <summary>
        /// Clears visual selection when leaving edit phase.
        /// </summary>
        public override void Exit()
        {
            SetSelection(NO_SELECTED_BLOCK_ID);
        }

        /// <summary>
        /// Selects an editable block or performs one valid move/swap edit.
        /// </summary>
        public override void HandleBoardCellClicked(Vector2Int coordinate)
        {
            if (!Context.Board.IsInside(coordinate) || Context.Board.IsWall(coordinate) || coordinate == Context.Board.PlayerPosition)
            {
                return;
            }

            BlockRuntime clickedBlock = Context.Board.GetBlock(coordinate);
            if (_selectedBlockId == NO_SELECTED_BLOCK_ID)
            {
                if (clickedBlock != null && !clickedBlock.IsTrap)
                {
                    SetSelection(clickedBlock.Id);
                }

                return;
            }

            BlockRuntime selectedBlock = FindBlockById(_selectedBlockId);
            if (selectedBlock == null)
            {
                SetSelection(NO_SELECTED_BLOCK_ID);
                return;
            }

            if (clickedBlock != null && clickedBlock.Id == selectedBlock.Id)
            {
                SetSelection(NO_SELECTED_BLOCK_ID);
                return;
            }

            if (Context.RemainingEditCount <= 0 || (clickedBlock != null && clickedBlock.IsTrap))
            {
                return;
            }

            bool editSucceeded = clickedBlock == null
                ? Context.Board.TryMoveBlock(selectedBlock.Coordinate, coordinate)
                : BoardSettings.AllowBlockSwap && Context.Board.TrySwapBlocks(selectedBlock.Coordinate, coordinate);

            if (!editSucceeded)
            {
                return;
            }

            Context.TryConsumeEdit();
            Publisher.PublishBlockLayout(true);
            Publisher.PublishTurnResources();
            SetSelection(NO_SELECTED_BLOCK_ID);
        }

        /// <summary>
        /// Allows the player to enter movement even when edit counts remain.
        /// </summary>
        public override void HandleEndPhaseRequested()
        {
            RequestTransition(BattlePhase.Movement, 0f);
        }

        /// <summary>
        /// Finds a runtime block copy without exposing GameObject references to battle logic.
        /// </summary>
        private BlockRuntime FindBlockById(int blockId)
        {
            var blocks = Context.PlacementService.Blocks;
            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                if (blocks[blockIndex].Id == blockId)
                {
                    return blocks[blockIndex];
                }
            }

            return null;
        }

        /// <summary>
        /// Updates runtime selection and publishes it for block highlight presentation.
        /// </summary>
        private void SetSelection(int blockId)
        {
            _selectedBlockId = blockId;
            EventChannel.RaiseBlockSelectionChanged(blockId);
        }
    }
}
