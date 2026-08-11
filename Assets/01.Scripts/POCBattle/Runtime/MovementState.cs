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
        WaitingForVisual = 1
    }

    /// <summary>
    /// Resolves bounded ice-slide movement, waits for DOTween feedback, then applies collided block effects.
    /// </summary>
    public sealed class MovementState : BattleStateBase
    {
        /// <summary>Current movement execution substate.</summary>
        private MovementExecutionState _executionState;

        /// <summary>Resolved result waiting for PlayerMovement visual completion.</summary>
        private MoveResult _pendingMoveResult;

        /// <summary>
        /// Creates movement state.
        /// </summary>
        public MovementState(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
            : base(context, presentationSettings, eventChannel, publisher)
        {
            _executionState = MovementExecutionState.Ready;
        }

        /// <summary>
        /// Resets the movement execution substate when movement phase begins.
        /// </summary>
        public override void Enter()
        {
            _executionState = MovementExecutionState.Ready;
            Publisher.PublishTurnResources();
        }

        /// <summary>
        /// Resolves one move request if movement is idle and at least one move remains.
        /// </summary>
        public override void HandleMoveRequested(Vector2Int direction)
        {
            if (_executionState != MovementExecutionState.Ready || Context.RemainingMoveCount <= 0)
            {
                return;
            }

            MoveResult result = Context.MovementResolver.Resolve(direction);
            _pendingMoveResult = result;
            _executionState = MovementExecutionState.WaitingForVisual;

            if (result.IsValid)
            {
                Context.Board.SetPlayerPosition(result.Destination);
                Context.TryConsumeMove();
                Publisher.PublishTurnResources();
            }

            EventChannel.RaisePlayerMoveVisualRequested(
                new PlayerMoveVisualCommand(result.IsValid, result.StartPosition, result.Destination, result.Direction));
        }

        /// <summary>
        /// Allows movement to end early only when no slide/invalid feedback tween is currently resolving.
        /// </summary>
        public override void HandleEndPhaseRequested()
        {
            if (_executionState == MovementExecutionState.Ready)
            {
                RequestTransition(BattlePhase.PlayerBattleResolve, 0f);
            }
        }

        /// <summary>
        /// Applies the collided block after visual contact, then checks defeat or automatic move-count completion.
        /// </summary>
        public override void HandlePlayerMoveVisualCompleted()
        {
            if (_executionState != MovementExecutionState.WaitingForVisual)
            {
                return;
            }

            MoveResult completedResult = _pendingMoveResult;
            _executionState = MovementExecutionState.Ready;

            if (completedResult.IsValid && completedResult.HitBlock != null)
            {
                int healthBeforeEffect = Context.Player.CurrentHealth;
                Context.BlockEffects.Apply(completedResult.HitBlock);
                Publisher.PublishTurnEffects();
                Publisher.PublishPlayerStatus();

                if (Context.Player.CurrentHealth < healthBeforeEffect)
                {
                    EventChannel.RaisePlayerHitVisualRequested();
                }
            }

            if (!Context.Player.IsAlive)
            {
                RequestTransition(BattlePhase.Defeat, 0f);
                return;
            }

            if (completedResult.IsValid && Context.RemainingMoveCount <= 0)
            {
                RequestTransition(BattlePhase.PlayerBattleResolve, 0f);
            }
        }
    }
}
