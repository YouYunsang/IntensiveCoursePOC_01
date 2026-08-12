using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Explicit edit interaction states used instead of drag/selection boolean combinations.
    /// </summary>
    internal enum PlacementEditExecutionState
    {
        Idle = 0,
        Dragging = 1,
        Settling = 2
    }

    /// <summary>
    /// Handles press-hold-drag placement editing, empty-cell drops, and optional editable-block swaps.
    /// Logical block coordinates remain unchanged until the pointer is released on a valid destination.
    /// </summary>
    public sealed class PlacementEditState : BattleStateBase
    {
        /// <summary>Sentinel used when no editable block is being dragged.</summary>
        private const int NO_DRAGGED_BLOCK_ID = -1;

        /// <summary>Current edit interaction substate.</summary>
        private PlacementEditExecutionState _executionState;

        /// <summary>Stable runtime id of the editable block currently held by the pointer.</summary>
        private int _draggedBlockId;

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
            _executionState = PlacementEditExecutionState.Idle;
            _draggedBlockId = NO_DRAGGED_BLOCK_ID;
        }

        /// <summary>Board gameplay settings used for swap policy.</summary>
        private BattleBoardSettingsSO BoardSettings { get; }

        /// <summary>
        /// Resets stale drag state every time edit phase begins.
        /// </summary>
        public override void Enter()
        {
            ResetDragState();
            Publisher.PublishTurnResources();
        }

        /// <summary>
        /// Cancels an unfinished drag before another battle phase becomes active.
        /// </summary>
        public override void Exit()
        {
            CancelActiveDrag();
        }

        /// <summary>
        /// Starts dragging only when the pressed coordinate contains an editable deck block and an edit remains.
        /// </summary>
        public override void HandlePlacementDragBeginRequested(Vector2Int coordinate)
        {
            if (_executionState != PlacementEditExecutionState.Idle || Context.RemainingEditCount <= 0)
            {
                return;
            }

            if (!Context.Board.IsInside(coordinate) || Context.Board.IsWall(coordinate) || coordinate == Context.Board.PlayerPosition)
            {
                return;
            }

            BlockRuntime block = Context.Board.GetBlock(coordinate);
            if (block == null || block.IsTrap)
            {
                return;
            }

            _draggedBlockId = block.Id;
            _executionState = PlacementEditExecutionState.Dragging;
            EventChannel.RaiseBlockDragVisualStarted(block.Id);
        }

        /// <summary>
        /// Commits one move/swap when the released coordinate is valid, otherwise returns the held block to its logical origin.
        /// </summary>
        public override void HandlePlacementDragDropRequested(Vector2Int coordinate)
        {
            if (_executionState != PlacementEditExecutionState.Dragging)
            {
                return;
            }

            BlockRuntime draggedBlock = FindBlockById(_draggedBlockId);
            if (draggedBlock == null)
            {
                CompleteDrag(false);
                return;
            }

            bool editSucceeded = TryCommitDrop(draggedBlock, coordinate);
            CompleteDrag(editSucceeded);
        }

        /// <summary>
        /// Cancels the current drag when the pointer is released over OnGUI or outside the board.
        /// </summary>
        public override void HandlePlacementDragCancelRequested()
        {
            if (_executionState == PlacementEditExecutionState.Dragging)
            {
                CompleteDrag(false);
            }
        }

        /// <summary>
        /// Re-enables edit input only after BoardView reports that the previous drop/swap layout animation has settled.
        /// </summary>
        public override void HandleBlockLayoutVisualCompleted()
        {
            if (_executionState == PlacementEditExecutionState.Settling)
            {
                _executionState = PlacementEditExecutionState.Idle;
            }
        }

        /// <summary>
        /// Allows the player to enter movement even when edit counts remain; an active drag is first returned to its origin.
        /// </summary>
        public override void HandleEndPhaseRequested()
        {
            CancelActiveDrag();
            RequestTransition(BattlePhase.Movement, 0f);
        }

        /// <summary>
        /// Attempts a logical drop without changing board state until every destination rule has passed.
        /// </summary>
        private bool TryCommitDrop(BlockRuntime draggedBlock, Vector2Int destination)
        {
            if (Context.RemainingEditCount <= 0
                || !Context.Board.IsInside(destination)
                || Context.Board.IsWall(destination)
                || destination == Context.Board.PlayerPosition
                || destination == draggedBlock.Coordinate)
            {
                return false;
            }

            BlockRuntime destinationBlock = Context.Board.GetBlock(destination);
            if (destinationBlock != null && destinationBlock.IsTrap)
            {
                return false;
            }

            if (destinationBlock == null)
            {
                return Context.Board.TryMoveBlock(draggedBlock.Coordinate, destination);
            }

            return BoardSettings.AllowBlockSwap
                   && Context.Board.TrySwapBlocks(draggedBlock.Coordinate, destination);
        }

        /// <summary>
        /// Finishes the interaction, consumes one edit only on a successful logical change, and publishes the authoritative layout.
        /// </summary>
        private void CompleteDrag(bool editSucceeded)
        {
            _draggedBlockId = NO_DRAGGED_BLOCK_ID;
            _executionState = PlacementEditExecutionState.Settling;

            if (editSucceeded)
            {
                Context.TryConsumeEdit();
                Publisher.PublishTurnResources();
            }

            // The same authoritative layout drives both a successful drop and a cancelled return-to-origin animation.
            Publisher.PublishBlockLayout(true);
        }

        /// <summary>
        /// Returns an unfinished held block to its authoritative board coordinate without consuming an edit.
        /// </summary>
        private void CancelActiveDrag()
        {
            if (_executionState == PlacementEditExecutionState.Dragging)
            {
                ResetDragState();
                Publisher.PublishBlockLayout(true);
                return;
            }

            ResetDragState();
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
        /// Clears the explicit edit interaction state.
        /// </summary>
        private void ResetDragState()
        {
            _executionState = PlacementEditExecutionState.Idle;
            _draggedBlockId = NO_DRAGGED_BLOCK_ID;
        }
    }
}
