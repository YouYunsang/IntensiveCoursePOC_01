using System.Collections.Generic;
using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Internal movement execution states used instead of multiple boolean flags.
    /// </summary>
    internal enum MovementExecutionState
    {
        Ready = 0,
        WaitingForSegmentVisual = 1
    }

    /// <summary>
    /// Resolves one manual move as a chain of slide segments. Direction-change cells redirect automatically without consuming extra moves.
    /// </summary>
    public sealed class MovementState : BattleStateBase
    {
        /// <summary>Current movement execution substate.</summary>
        private MovementExecutionState _executionState;

        /// <summary>Resolved segment waiting for PlayerMovement visual completion.</summary>
        private MoveResult _pendingMoveResult;

        /// <summary>Cell effects already activated during the current single manual movement input.</summary>
        private readonly HashSet<int> _usedCellEffectIds;

        /// <summary>Creates movement state.</summary>
        public MovementState(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
            : base(context, presentationSettings, eventChannel, publisher)
        {
            _executionState = MovementExecutionState.Ready;
            _usedCellEffectIds = new HashSet<int>();
        }

        /// <summary>Resets movement-chain state when movement phase begins.</summary>
        public override void Enter()
        {
            _executionState = MovementExecutionState.Ready;
            _usedCellEffectIds.Clear();
            EventChannel.RaiseCellEffectMoveUsageResetRequested();
            Publisher.PublishTurnResources();
        }

        /// <summary>
        /// Starts one manual movement. Only this initial valid segment consumes a move count; redirected continuation segments do not.
        /// </summary>
        public override void HandleMoveRequested(Vector2Int direction)
        {
            if (_executionState != MovementExecutionState.Ready || Context.RemainingMoveCount <= 0)
            {
                return;
            }

            _usedCellEffectIds.Clear();
            EventChannel.RaiseCellEffectMoveUsageResetRequested();
            MoveResult result = Context.MovementResolver.Resolve(direction, _usedCellEffectIds);
            StartSegment(result, result.IsValid);
        }

        /// <summary>Allows movement to end early only when no slide/invalid feedback tween is resolving.</summary>
        public override void HandleEndPhaseRequested()
        {
            if (_executionState == MovementExecutionState.Ready)
            {
                RequestTransition(BattlePhase.PlayerBattleResolve, 0f);
            }
        }

        /// <summary>
        /// Resolves block priority, cell redirection, death, and automatic continuation after each movement segment visual completes.
        /// </summary>
        public override void HandlePlayerMoveVisualCompleted()
        {
            if (_executionState != MovementExecutionState.WaitingForSegmentVisual)
            {
                return;
            }

            MoveResult completedResult = _pendingMoveResult;

            if (!completedResult.IsValid)
            {
                FinishMovementChain();
                return;
            }

            if (completedResult.StopReason == MoveStopReason.CellEffect && completedResult.HitCellEffect != null)
            {
                if (!ResolveBlockImmediatelyAheadBeforeDirectionChange(completedResult))
                {
                    return;
                }

                ContinueFromCellEffect(completedResult.HitCellEffect, completedResult.Direction);
                return;
            }

            if (completedResult.HitBlock != null)
            {
                ApplyBlockCollision(completedResult.HitBlock);
                if (!Context.Player.IsAlive)
                {
                    _executionState = MovementExecutionState.Ready;
                    RequestTransition(BattlePhase.Defeat, 0f);
                    return;
                }
            }

            FinishMovementChain();
        }

        /// <summary>
        /// Starts one resolved segment and optionally consumes the single manual move resource.
        /// </summary>
        private void StartSegment(MoveResult result, bool consumeManualMove)
        {
            _pendingMoveResult = result;
            _executionState = MovementExecutionState.WaitingForSegmentVisual;

            if (result.IsValid)
            {
                Context.Board.SetPlayerPosition(result.Destination);
                if (consumeManualMove)
                {
                    Context.TryConsumeMove();
                    Publisher.PublishTurnResources();
                }
            }

            EventChannel.RaisePlayerMoveVisualRequested(
                new PlayerMoveVisualCommand(result.IsValid, result.StartPosition, result.Destination, result.Direction));
        }

        /// <summary>
        /// If the cell effect is entered directly in front of a block, resolves that block once before changing direction.
        /// The redirected continuation then uses normal invalid-move rules, preventing the same adjacent block from being awarded twice.
        /// </summary>
        private bool ResolveBlockImmediatelyAheadBeforeDirectionChange(MoveResult completedResult)
        {
            Vector2Int forwardCoordinate = completedResult.Destination + completedResult.Direction;
            BlockRuntime priorityBlock = Context.Board.GetBlock(forwardCoordinate);
            if (priorityBlock == null)
            {
                return true;
            }

            ApplyBlockCollision(priorityBlock);
            if (Context.Player.IsAlive)
            {
                return true;
            }

            _executionState = MovementExecutionState.Ready;
            RequestTransition(BattlePhase.Defeat, 0f);
            return false;
        }

        /// <summary>
        /// Deactivates one cell effect for this manual move, resolves its polymorphic direction, and continues automatically.
        /// </summary>
        private void ContinueFromCellEffect(CellEffectRuntime cellEffect, Vector2Int incomingDirection)
        {
            _usedCellEffectIds.Add(cellEffect.Id);
            EventChannel.RaiseCellEffectTriggeredVisualRequested(cellEffect.Id);

            Vector2Int nextDirection = incomingDirection;
            if (cellEffect.Definition is ICellMovementDirectionEffect directionEffect)
            {
                nextDirection = directionEffect.ResolveNextDirection(incomingDirection, cellEffect.AssignedDirection);
            }

            Vector2Int adjacentCoordinate = Context.Board.PlayerPosition + nextDirection;
            if (!Context.Board.IsInside(adjacentCoordinate) || Context.Board.IsWall(adjacentCoordinate))
            {
                FinishMovementChain();
                return;
            }

            MoveResult nextResult = Context.MovementResolver.Resolve(nextDirection, _usedCellEffectIds);
            StartSegment(nextResult, false);
        }

        /// <summary>Applies one block exactly once and publishes all combat/presentation changes caused by it.</summary>
        private void ApplyBlockCollision(BlockRuntime block)
        {
            EventChannel.RaiseBlockHitVisualRequested(block.Id);
            int healthBeforeEffect = Context.Player.CurrentHealth;
            Context.BlockEffects.Apply(block);
            Publisher.PublishTurnEffects();
            Publisher.PublishPlayerStatus();

            if (Context.Player.CurrentHealth < healthBeforeEffect)
            {
                EventChannel.RaisePlayerHitVisualRequested();
            }
        }

        /// <summary>Ends the full manual movement chain and advances automatically only after all redirected segments are resolved.</summary>
        private void FinishMovementChain()
        {
            _executionState = MovementExecutionState.Ready;
            if (Context.RemainingMoveCount <= 0)
            {
                RequestTransition(BattlePhase.PlayerBattleResolve, 0f);
            }
        }
    }
}
