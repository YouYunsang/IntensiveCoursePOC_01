using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Immutable state transition request consumed by BattleController after a state method finishes.
    /// </summary>
    public readonly struct BattleTransitionRequest
    {
        /// <summary>Requested next phase.</summary>
        private readonly BattlePhase _nextPhase;

        /// <summary>Optional presentation delay before the phase change.</summary>
        private readonly float _delay;

        /// <summary>Gets requested next phase.</summary>
        public BattlePhase NextPhase => _nextPhase;

        /// <summary>Gets requested transition delay.</summary>
        public float Delay => _delay;

        /// <summary>
        /// Creates one immutable transition request value.
        /// </summary>
        public BattleTransitionRequest(BattlePhase nextPhase, float delay)
        {
            _nextPhase = nextPhase;
            _delay = delay;
        }
    }

    /// <summary>
    /// Base class for explicit battle states; unsupported inputs default to no-op instead of bool flag combinations.
    /// </summary>
    public abstract class BattleStateBase
    {
        /// <summary>Pure mutable runtime context.</summary>
        protected readonly BattleRuntimeContext Context;

        /// <summary>Presentation-only tuning data.</summary>
        protected readonly BattlePresentationSettingsSO PresentationSettings;

        /// <summary>Event hub for cross-GameObject communication.</summary>
        protected readonly BattleEventChannelSO EventChannel;

        /// <summary>Snapshot publisher shared by states.</summary>
        protected readonly BattlePresentationPublisher Publisher;

        /// <summary>Pending value-only transition request, avoiding a reverse reference from state to controller/state machine.</summary>
        private BattleTransitionRequest? _transitionRequest;

        /// <summary>
        /// Creates one state with runtime services and no reverse controller/state-machine reference.
        /// </summary>
        protected BattleStateBase(
            BattleRuntimeContext context,
            BattlePresentationSettingsSO presentationSettings,
            BattleEventChannelSO eventChannel,
            BattlePresentationPublisher publisher)
        {
            Context = context;
            PresentationSettings = presentationSettings;
            EventChannel = eventChannel;
            Publisher = publisher;
            _transitionRequest = null;
        }

        /// <summary>
        /// Called when this state becomes active.
        /// </summary>
        public virtual void Enter()
        {
        }

        /// <summary>
        /// Called before this state is replaced.
        /// </summary>
        public virtual void Exit()
        {
        }

        /// <summary>
        /// Handles a cardinal movement input when meaningful for this state.
        /// </summary>
        public virtual void HandleMoveRequested(Vector2Int direction)
        {
        }

        /// <summary>
        /// Handles one clicked board coordinate when meaningful for this state.
        /// </summary>
        public virtual void HandleBoardCellClicked(Vector2Int coordinate)
        {
        }

        /// <summary>
        /// Handles placement-edit left-button press from a logical source coordinate.
        /// </summary>
        public virtual void HandlePlacementDragBeginRequested(Vector2Int coordinate)
        {
        }

        /// <summary>
        /// Handles placement-edit left-button release over a valid logical destination coordinate.
        /// </summary>
        public virtual void HandlePlacementDragDropRequested(Vector2Int coordinate)
        {
        }

        /// <summary>
        /// Handles cancellation when a held placement pointer is released outside a valid board drop target.
        /// </summary>
        public virtual void HandlePlacementDragCancelRequested()
        {
        }

        /// <summary>
        /// Handles manual early phase completion when supported.
        /// </summary>
        public virtual void HandleEndPhaseRequested()
        {
        }

        /// <summary>
        /// Handles completion of an authoritative block layout/drop tween when meaningful for this state.
        /// </summary>
        public virtual void HandleBlockLayoutVisualCompleted()
        {
        }

        /// <summary>
        /// Handles completion of an authoritative cell-effect edit layout/drop tween when meaningful for this state.
        /// </summary>
        public virtual void HandleCellEffectLayoutVisualCompleted()
        {
        }

        /// <summary>
        /// Handles completion of PlayerMovement DOTween feedback when meaningful for this state.
        /// </summary>
        public virtual void HandlePlayerMoveVisualCompleted()
        {
        }

        /// <summary>
        /// Stores a one-way value request that the owning controller consumes after the current state call returns.
        /// </summary>
        protected void RequestTransition(BattlePhase nextPhase, float delay)
        {
            _transitionRequest = new BattleTransitionRequest(nextPhase, delay);
        }

        /// <summary>
        /// Consumes and clears the current transition request, if one exists.
        /// </summary>
        public bool TryConsumeTransitionRequest(out BattleTransitionRequest request)
        {
            if (_transitionRequest.HasValue)
            {
                request = _transitionRequest.Value;
                _transitionRequest = null;
                return true;
            }

            request = default;
            return false;
        }
    }
}
