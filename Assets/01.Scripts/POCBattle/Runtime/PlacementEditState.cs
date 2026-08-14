using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>Explicit edit interaction states used instead of drag/selection boolean combinations.</summary>
    internal enum PlacementEditExecutionState
    {
        Idle = 0,
        Dragging = 1,
        Settling = 2
    }

    /// <summary>Logical types that can own one placement drag interaction.</summary>
    internal enum PlacementDragTargetType
    {
        None = 0,
        Block = 1,
        CellEffect = 2
    }

    /// <summary>Value-only current placement drag target.</summary>
    internal readonly struct PlacementDragTarget
    {
        private readonly PlacementDragTargetType _type;
        private readonly int _runtimeId;

        public PlacementDragTargetType Type => _type;
        public int RuntimeId => _runtimeId;
        public bool IsValid => _type != PlacementDragTargetType.None && _runtimeId >= 0;

        /// <summary>Creates one immutable drag target.</summary>
        public PlacementDragTarget(PlacementDragTargetType type, int runtimeId)
        {
            _type = type;
            _runtimeId = runtimeId;
        }

        /// <summary>Gets an empty drag target.</summary>
        public static PlacementDragTarget None => new PlacementDragTarget(PlacementDragTargetType.None, -1);
    }

    /// <summary>
    /// Handles press-hold-drag placement editing for both editable player blocks and player-owned cell effects.
    /// Logical coordinates remain unchanged until the pointer is released on a valid destination.
    /// </summary>
    public sealed class PlacementEditState : BattleStateBase
    {
        private PlacementEditExecutionState _executionState;
        private PlacementDragTarget _dragTarget;
        private PlacementDragTargetType _settlingTargetType;

        /// <summary>Creates placement edit state.</summary>
        public PlacementEditState(
            BattleRuntimeContext context,
            BattleBoardSettingsSO boardSettings,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
            : base(context, presentationSettings, eventChannel, publisher)
        {
            BoardSettings = boardSettings;
            ResetDragState();
        }

        /// <summary>Board gameplay settings used for block swap policy.</summary>
        private BattleBoardSettingsSO BoardSettings { get; }

        /// <summary>Resets stale drag state every time edit phase begins.</summary>
        public override void Enter()
        {
            ResetDragState();
            Publisher.PublishTurnResources();
        }

        /// <summary>Cancels an unfinished drag before another battle phase becomes active.</summary>
        public override void Exit()
        {
            CancelActiveDrag();
        }

        /// <summary>Begins a block drag first, otherwise a player-owned cell-effect drag, when one edit remains.</summary>
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
            if (block != null)
            {
                if (block.IsTrap)
                {
                    return;
                }

                _dragTarget = new PlacementDragTarget(PlacementDragTargetType.Block, block.Id);
                _executionState = PlacementEditExecutionState.Dragging;
                EventChannel.RaiseBlockDragVisualStarted(block.Id);
                return;
            }

            CellEffectRuntime cellEffect = Context.Board.GetCellEffect(coordinate);
            if (cellEffect == null)
            {
                return;
            }

            _dragTarget = new PlacementDragTarget(PlacementDragTargetType.CellEffect, cellEffect.Id);
            _executionState = PlacementEditExecutionState.Dragging;
            EventChannel.RaiseCellEffectDragVisualStarted(cellEffect.Id);
        }

        /// <summary>Commits the current typed drag when its destination rules pass, otherwise returns it to its authoritative origin.</summary>
        public override void HandlePlacementDragDropRequested(Vector2Int coordinate)
        {
            if (_executionState != PlacementEditExecutionState.Dragging || !_dragTarget.IsValid)
            {
                return;
            }

            bool editSucceeded = false;
            if (_dragTarget.Type == PlacementDragTargetType.Block)
            {
                BlockRuntime block = FindBlockById(_dragTarget.RuntimeId);
                editSucceeded = block != null && TryCommitBlockDrop(block, coordinate);
            }
            else if (_dragTarget.Type == PlacementDragTargetType.CellEffect)
            {
                CellEffectRuntime effect = FindCellEffectById(_dragTarget.RuntimeId);
                editSucceeded = effect != null && TryCommitCellEffectDrop(effect, coordinate);
            }

            CompleteDrag(editSucceeded);
        }

        /// <summary>Cancels the current drag when the pointer is released over OnGUI or outside the board.</summary>
        public override void HandlePlacementDragCancelRequested()
        {
            if (_executionState == PlacementEditExecutionState.Dragging)
            {
                CompleteDrag(false);
            }
        }

        /// <summary>Unlocks edit input after a block drop/swap visual settles.</summary>
        public override void HandleBlockLayoutVisualCompleted()
        {
            if (_executionState == PlacementEditExecutionState.Settling
                && _settlingTargetType == PlacementDragTargetType.Block)
            {
                ResetDragState();
            }
        }

        /// <summary>Unlocks edit input after a cell-effect drop/swap visual settles.</summary>
        public override void HandleCellEffectLayoutVisualCompleted()
        {
            if (_executionState == PlacementEditExecutionState.Settling
                && _settlingTargetType == PlacementDragTargetType.CellEffect)
            {
                ResetDragState();
            }
        }

        /// <summary>Allows the player to enter movement even when edit counts remain.</summary>
        public override void HandleEndPhaseRequested()
        {
            CancelActiveDrag();
            RequestTransition(BattlePhase.Movement, 0f);
        }

        /// <summary>Attempts one block move/swap while preserving trap, player, wall, and cell-effect restrictions.</summary>
        private bool TryCommitBlockDrop(BlockRuntime draggedBlock, Vector2Int destination)
        {
            if (Context.RemainingEditCount <= 0
                || !Context.Board.IsInside(destination)
                || Context.Board.IsWall(destination)
                || destination == Context.Board.PlayerPosition
                || destination == draggedBlock.Coordinate
                || Context.Board.GetCellEffect(destination) != null)
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

        /// <summary>Attempts one cell-effect move or cell-effect-to-cell-effect swap while preserving its assigned direction.</summary>
        private bool TryCommitCellEffectDrop(CellEffectRuntime draggedEffect, Vector2Int destination)
        {
            if (Context.RemainingEditCount <= 0
                || !Context.Board.IsInside(destination)
                || Context.Board.IsWall(destination)
                || destination == Context.Board.PlayerPosition
                || destination == draggedEffect.Coordinate
                || Context.Board.GetBlock(destination) != null)
            {
                return false;
            }

            CellEffectRuntime destinationEffect = Context.Board.GetCellEffect(destination);
            if (destinationEffect == null)
            {
                return Context.Board.TryMoveCellEffect(draggedEffect.Coordinate, destination);
            }

            return Context.Board.TrySwapCellEffects(draggedEffect.Coordinate, destination);
        }

        /// <summary>Finishes one typed interaction and publishes only the authoritative layer that changed or must return.</summary>
        private void CompleteDrag(bool editSucceeded)
        {
            PlacementDragTargetType completedType = _dragTarget.Type;
            _dragTarget = PlacementDragTarget.None;
            _executionState = PlacementEditExecutionState.Settling;
            _settlingTargetType = completedType;

            if (editSucceeded)
            {
                Context.TryConsumeEdit();
                Publisher.PublishTurnResources();
            }

            if (completedType == PlacementDragTargetType.Block)
            {
                Publisher.PublishBlockLayout(true);
                return;
            }

            if (completedType == PlacementDragTargetType.CellEffect)
            {
                Publisher.PublishCellEffectLayout(true);
                return;
            }

            ResetDragState();
        }

        /// <summary>Returns an unfinished held target to its authoritative layer without consuming an edit.</summary>
        private void CancelActiveDrag()
        {
            if (_executionState != PlacementEditExecutionState.Dragging || !_dragTarget.IsValid)
            {
                ResetDragState();
                return;
            }

            CompleteDrag(false);
        }

        /// <summary>Finds a stage-local runtime block copy by stable id.</summary>
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

        /// <summary>Finds a stage-local runtime cell effect by stable deck instance id.</summary>
        private CellEffectRuntime FindCellEffectById(int effectId)
        {
            var effects = Context.CellEffectPlacementService.Effects;
            for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                if (effects[effectIndex].Id == effectId)
                {
                    return effects[effectIndex];
                }
            }

            return null;
        }

        /// <summary>Clears the explicit edit interaction state.</summary>
        private void ResetDragState()
        {
            _executionState = PlacementEditExecutionState.Idle;
            _dragTarget = PlacementDragTarget.None;
            _settlingTargetType = PlacementDragTargetType.None;
        }
    }
}
