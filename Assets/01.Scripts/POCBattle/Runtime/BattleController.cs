using System;
using System.Collections;
using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Battle composition root: owns pure runtime state and forwards cross-GameObject event requests into the state machine.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField, Tooltip("Board gameplay settings.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Player base combat and per-turn resource stats.")]
        private PlayerBaseStatsSO _playerBaseStats;

        [SerializeField, Tooltip("Presentation tuning shared by battle states and views.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Player deck whose complete block list is placed every player turn.")]
        private DeckDefinitionSO _deck;

        [SerializeField, Tooltip("Current POC encounter definition.")]
        private EncounterDefinitionSO _encounter;

        [SerializeField, Tooltip("Shared ScriptableObject event hub for all battle GameObjects.")]
        private BattleEventChannelSO _eventChannel;

        /// <summary>Pure runtime models/services for the current battle session.</summary>
        private BattleRuntimeContext _runtimeContext;

        /// <summary>Current explicit battle state machine.</summary>
        private BattleStateMachine _stateMachine;

        /// <summary>Runtime-to-presentation snapshot publisher.</summary>
        private BattlePresentationPublisher _publisher;

        /// <summary>Pending delayed phase transition coroutine, if one exists.</summary>
        private Coroutine _transitionCoroutine;

        /// <summary>
        /// Subscribes only to event-channel requests; no other GameObject reference is required.
        /// </summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.MoveInputRequested += HandleMoveInputRequested;
            _eventChannel.BoardCellClicked += HandleBoardCellClicked;
            _eventChannel.PlacementDragBeginRequested += HandlePlacementDragBeginRequested;
            _eventChannel.PlacementDragDropRequested += HandlePlacementDragDropRequested;
            _eventChannel.PlacementDragCancelRequested += HandlePlacementDragCancelRequested;
            _eventChannel.EndPhaseRequested += HandleEndPhaseRequested;
            _eventChannel.PlayerMoveVisualCompleted += HandlePlayerMoveVisualCompleted;
            _eventChannel.BlockLayoutVisualCompleted += HandleBlockLayoutVisualCompleted;
            _eventChannel.RestartRequested += HandleRestartRequested;
        }

        /// <summary>
        /// Removes all event subscriptions when this battle root is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.MoveInputRequested -= HandleMoveInputRequested;
                _eventChannel.BoardCellClicked -= HandleBoardCellClicked;
                _eventChannel.PlacementDragBeginRequested -= HandlePlacementDragBeginRequested;
                _eventChannel.PlacementDragDropRequested -= HandlePlacementDragDropRequested;
                _eventChannel.PlacementDragCancelRequested -= HandlePlacementDragCancelRequested;
                _eventChannel.EndPhaseRequested -= HandleEndPhaseRequested;
                _eventChannel.PlayerMoveVisualCompleted -= HandlePlayerMoveVisualCompleted;
                _eventChannel.BlockLayoutVisualCompleted -= HandleBlockLayoutVisualCompleted;
                _eventChannel.RestartRequested -= HandleRestartRequested;
            }

            StopPendingTransition();
        }

        /// <summary>
        /// Creates the first runtime battle after every scene object has had a chance to subscribe in OnEnable.
        /// </summary>
        private void Start()
        {
            InitializeBattle();
        }

        /// <summary>
        /// Validates required data, builds pure runtime objects, registers states, and begins the first player turn.
        /// </summary>
        private void InitializeBattle()
        {
            ValidateSerializedData();
            StopPendingTransition();

            int randomSeed = _boardSettings.UseFixedRandomSeed ? _boardSettings.FixedRandomSeed : Environment.TickCount;
            _runtimeContext = new BattleRuntimeContext(_boardSettings, _playerBaseStats, _deck, _encounter, randomSeed);
            _publisher = new BattlePresentationPublisher(_runtimeContext, _presentationSettings, _eventChannel);
            _stateMachine = BuildStateMachine();

            _eventChannel.RaiseBattleResultChanged(BattleResult.None);
            _publisher.PublishEnemyPartySetup();
            _publisher.PublishAllEnemyPresentation();
            _publisher.PublishPlayerStatus();
            _publisher.PublishTurnEffects();
            _publisher.PublishPlayerPositionSync();
            ChangeStateAndProcessTransition(BattlePhase.PlayerTurnSetup);
        }

        /// <summary>
        /// Creates and registers explicit state objects that reference the runtime context only through one-way ownership.
        /// </summary>
        private BattleStateMachine BuildStateMachine()
        {
            BattleStateMachine stateMachine = new BattleStateMachine(_eventChannel.RaisePhaseChanged);
            stateMachine.Register(
                BattlePhase.PlayerTurnSetup,
                new PlayerTurnSetupState(_runtimeContext, _presentationSettings, _eventChannel, _publisher));
            stateMachine.Register(
                BattlePhase.PlacementEdit,
                new PlacementEditState(_runtimeContext, _boardSettings, _presentationSettings, _eventChannel, _publisher));
            stateMachine.Register(
                BattlePhase.Movement,
                new MovementState(_runtimeContext, _presentationSettings, _eventChannel, _publisher));
            stateMachine.Register(
                BattlePhase.PlayerBattleResolve,
                new PlayerBattleResolveState(_runtimeContext, _presentationSettings, _eventChannel, _publisher));
            stateMachine.Register(
                BattlePhase.EnemyTurn,
                new EnemyTurnState(_runtimeContext, _presentationSettings, _eventChannel, _publisher));
            stateMachine.Register(
                BattlePhase.Victory,
                new BattleResultState(
                    _runtimeContext,
                    _presentationSettings,
                    _eventChannel,
                    _publisher,
                    BattleResult.Victory));
            stateMachine.Register(
                BattlePhase.Defeat,
                new BattleResultState(
                    _runtimeContext,
                    _presentationSettings,
                    _eventChannel,
                    _publisher,
                    BattleResult.Defeat));
            return stateMachine;
        }

        /// <summary>
        /// Changes state and immediately consumes any value-only transition request produced by the entered state.
        /// </summary>
        private void ChangeStateAndProcessTransition(BattlePhase nextPhase)
        {
            _stateMachine.ChangeState(nextPhase);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Consumes a state's value-only transition request and applies it immediately or schedules it after presentation delay.
        /// </summary>
        private void ProcessCurrentStateTransitionRequest()
        {
            BattleStateBase currentState = _stateMachine?.CurrentState;
            if (currentState == null || !currentState.TryConsumeTransitionRequest(out BattleTransitionRequest request))
            {
                return;
            }

            StopPendingTransition();
            if (request.Delay <= 0f)
            {
                ChangeStateAndProcessTransition(request.NextPhase);
                return;
            }

            _transitionCoroutine = StartCoroutine(ChangeStateAfterDelay(request.NextPhase, request.Delay));
        }

        /// <summary>
        /// Waits presentation time before applying an automatic battle phase transition.
        /// </summary>
        private IEnumerator ChangeStateAfterDelay(BattlePhase nextPhase, float delay)
        {
            yield return new WaitForSeconds(delay);
            _transitionCoroutine = null;
            ChangeStateAndProcessTransition(nextPhase);
        }

        /// <summary>
        /// Stops a previous delayed transition during restart or when a new transition supersedes it.
        /// </summary>
        private void StopPendingTransition()
        {
            if (_transitionCoroutine == null)
            {
                return;
            }

            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        /// <summary>
        /// Forwards cardinal player input into the currently active state.
        /// </summary>
        private void HandleMoveInputRequested(Vector2Int direction)
        {
            _stateMachine?.CurrentState?.HandleMoveRequested(direction);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Forwards edit pointer coordinates into the currently active state.
        /// </summary>
        private void HandleBoardCellClicked(Vector2Int coordinate)
        {
            _stateMachine?.CurrentState?.HandleBoardCellClicked(coordinate);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Forwards placement-edit press into the currently active state.
        /// </summary>
        private void HandlePlacementDragBeginRequested(Vector2Int coordinate)
        {
            _stateMachine?.CurrentState?.HandlePlacementDragBeginRequested(coordinate);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Forwards placement-edit drop into the currently active state.
        /// </summary>
        private void HandlePlacementDragDropRequested(Vector2Int coordinate)
        {
            _stateMachine?.CurrentState?.HandlePlacementDragDropRequested(coordinate);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Forwards placement-edit drag cancellation into the currently active state.
        /// </summary>
        private void HandlePlacementDragCancelRequested()
        {
            _stateMachine?.CurrentState?.HandlePlacementDragCancelRequested();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Forwards manual phase completion into the currently active state.
        /// </summary>
        private void HandleEndPhaseRequested()
        {
            _stateMachine?.CurrentState?.HandleEndPhaseRequested();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Forwards PlayerMovement tween completion into the currently active state.
        /// </summary>
        private void HandlePlayerMoveVisualCompleted()
        {
            _stateMachine?.CurrentState?.HandlePlayerMoveVisualCompleted();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Forwards block layout tween completion so PlacementEditState can unlock the next drag only after visuals settle.
        /// </summary>
        private void HandleBlockLayoutVisualCompleted()
        {
            _stateMachine?.CurrentState?.HandleBlockLayoutVisualCompleted();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Rebuilds pure battle runtime state while reusing already-instantiated view objects where possible.
        /// </summary>
        private void HandleRestartRequested()
        {
            InitializeBattle();
        }

        /// <summary>
        /// Fails fast when the editor installer or manual scene setup is missing required data references.
        /// </summary>
        private void ValidateSerializedData()
        {
            if (_boardSettings == null
                || _playerBaseStats == null
                || _presentationSettings == null
                || _deck == null
                || _encounter == null
                || _eventChannel == null)
            {
                throw new InvalidOperationException("BattleController is missing one or more required ScriptableObject references.");
            }
        }
    }
}
