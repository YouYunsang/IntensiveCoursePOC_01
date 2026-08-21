using System;
using System.Collections;
using System.Collections.Generic;
using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Stage-battle composition root. It owns only one battle runtime/state machine and can be driven by RunController.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField, Tooltip("Board gameplay settings.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Player base combat and per-turn resource stats used only by standalone fallback mode.")]
        private PlayerBaseStatsSO _playerBaseStats;

        [SerializeField, Tooltip("Presentation tuning shared by battle states and views.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Standalone fallback starting deck. RunController supplies its persistent runtime deck during normal POC play.")]
        private DeckDefinitionSO _deck;

        [SerializeField, Tooltip("Standalone fallback encounter. RunController selects stage encounters during normal POC play.")]
        private EncounterDefinitionSO _encounter;

        [SerializeField, Tooltip("Shared ScriptableObject event hub for all battle/run GameObjects.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("When enabled, BattleController starts the legacy single battle by itself. Run/stage patch disables this in the POC scene.")]
        private bool _autoStartStandalone = true;

        /// <summary>Pure runtime models/services for the current stage battle.</summary>
        private BattleRuntimeContext _runtimeContext;

        /// <summary>Current explicit battle state machine.</summary>
        private BattleStateMachine _stateMachine;

        /// <summary>Runtime-to-presentation snapshot publisher.</summary>
        private BattlePresentationPublisher _publisher;

        /// <summary>Pending delayed phase transition coroutine, if one exists.</summary>
        private Coroutine _transitionCoroutine;

        /// <summary>
        /// Subscribes only to battle request events; no RunController reverse reference is stored.
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
            _eventChannel.CellEffectLayoutVisualCompleted += HandleCellEffectLayoutVisualCompleted;
            _eventChannel.RestartRequested += HandleLegacyRestartRequested;
        }

        /// <summary>Removes all event subscriptions when this battle root is disabled.</summary>
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
                _eventChannel.CellEffectLayoutVisualCompleted -= HandleCellEffectLayoutVisualCompleted;
                _eventChannel.RestartRequested -= HandleLegacyRestartRequested;
            }

            StopPendingTransition();
        }

        /// <summary>
        /// Preserves a safe standalone fallback until the run patch installer sets AutoStartStandalone to false.
        /// </summary>
        private void Start()
        {
            if (!_autoStartStandalone)
            {
                return;
            }

            ValidateStandaloneSerializedData();
            PlayerRunModel standaloneRun = new PlayerRunModel(_playerBaseStats, _deck);
            int randomSeed = _boardSettings.UseFixedRandomSeed ? _boardSettings.FixedRandomSeed : Environment.TickCount;
            BeginStage(standaloneRun, _encounter, randomSeed);
        }

        /// <summary>
        /// Starts one stage battle from persistent run progression. RunController is the only normal caller.
        /// </summary>
        public void BeginStage(PlayerRunModel runModel, EncounterDefinitionSO encounter, int randomSeed)
        {
            StageBattleSetup setup = new StageBattleSetup(encounter, Array.Empty<StageFieldEffectDefinitionSO>(), randomSeed);
            BeginStage(runModel, setup);
        }

        /// <summary>Starts one stage battle from an immutable stage setup package.</summary>
        public void BeginStage(PlayerRunModel runModel, StageBattleSetup stageSetup)
        {
            if (runModel == null)
            {
                throw new ArgumentNullException(nameof(runModel));
            }

            ValidateSharedSerializedData();
            StopPendingTransition();
            runModel.Player.ResetTransientForNewStage();

            _runtimeContext = new BattleRuntimeContext(_boardSettings, runModel, stageSetup);
            _publisher = new BattlePresentationPublisher(_runtimeContext, _presentationSettings, _eventChannel);
            _stateMachine = BuildStateMachine();

            _eventChannel.RaiseBattleResultChanged(BattleResult.None);
            _publisher.PublishStageFieldEffectLayout();
            _publisher.PublishEnemyPartySetup();
            _publisher.PublishAllEnemyPresentation();
            _publisher.PublishPlayerStatus();
            _publisher.PublishTurnEffects();
            _publisher.PublishPlayerPositionSync();
            ChangeStateAndProcessTransition(BattlePhase.PlayerTurnSetup);
        }

        /// <summary>
        /// Stops the current stage battle without producing a result so an external run-level debug transition can safely replace it.
        /// Pending delayed state changes are cancelled and late presentation callbacks become harmless until BeginStage creates a new runtime.
        /// </summary>
        public void AbortForExternalStageTransition()
        {
            StopPendingTransition();
            _stateMachine = null;
            _publisher = null;
            _runtimeContext = null;
        }

        /// <summary>Creates and registers explicit reusable state objects for the current stage battle.</summary>
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
                new BattleResultState(_runtimeContext, _presentationSettings, _eventChannel, _publisher, BattleResult.Victory));
            stateMachine.Register(
                BattlePhase.Defeat,
                new BattleResultState(_runtimeContext, _presentationSettings, _eventChannel, _publisher, BattleResult.Defeat));
            return stateMachine;
        }

        /// <summary>Changes state and consumes transition requests produced by the entered state.</summary>
        private void ChangeStateAndProcessTransition(BattlePhase nextPhase)
        {
            _stateMachine.ChangeState(nextPhase);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Consumes a state's one-way transition request and schedules presentation delay when needed.</summary>
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

        /// <summary>Waits presentation time before applying one automatic battle phase transition.</summary>
        private IEnumerator ChangeStateAfterDelay(BattlePhase nextPhase, float delay)
        {
            yield return new WaitForSeconds(delay);
            _transitionCoroutine = null;
            ChangeStateAndProcessTransition(nextPhase);
        }

        /// <summary>Stops any superseded delayed phase transition.</summary>
        private void StopPendingTransition()
        {
            if (_transitionCoroutine == null)
            {
                return;
            }

            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        /// <summary>Forwards cardinal input to the current battle state.</summary>
        private void HandleMoveInputRequested(Vector2Int direction)
        {
            _stateMachine?.CurrentState?.HandleMoveRequested(direction);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Forwards legacy clicked board coordinates to the current state.</summary>
        private void HandleBoardCellClicked(Vector2Int coordinate)
        {
            _stateMachine?.CurrentState?.HandleBoardCellClicked(coordinate);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Forwards placement drag begin.</summary>
        private void HandlePlacementDragBeginRequested(Vector2Int coordinate)
        {
            _stateMachine?.CurrentState?.HandlePlacementDragBeginRequested(coordinate);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Forwards placement drag drop.</summary>
        private void HandlePlacementDragDropRequested(Vector2Int coordinate)
        {
            _stateMachine?.CurrentState?.HandlePlacementDragDropRequested(coordinate);
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Forwards placement drag cancellation.</summary>
        private void HandlePlacementDragCancelRequested()
        {
            _stateMachine?.CurrentState?.HandlePlacementDragCancelRequested();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Forwards manual phase completion.</summary>
        private void HandleEndPhaseRequested()
        {
            _stateMachine?.CurrentState?.HandleEndPhaseRequested();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Forwards PlayerMovement visual completion.</summary>
        private void HandlePlayerMoveVisualCompleted()
        {
            _stateMachine?.CurrentState?.HandlePlayerMoveVisualCompleted();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Forwards authoritative block layout visual completion.</summary>
        private void HandleBlockLayoutVisualCompleted()
        {
            _stateMachine?.CurrentState?.HandleBlockLayoutVisualCompleted();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>Forwards authoritative cell-effect edit layout visual completion.</summary>
        private void HandleCellEffectLayoutVisualCompleted()
        {
            _stateMachine?.CurrentState?.HandleCellEffectLayoutVisualCompleted();
            ProcessCurrentStateTransitionRequest();
        }

        /// <summary>
        /// Keeps the old single-battle restart request functional only while standalone auto-start mode is active.
        /// </summary>
        private void HandleLegacyRestartRequested()
        {
            if (!_autoStartStandalone)
            {
                return;
            }

            PlayerRunModel standaloneRun = new PlayerRunModel(_playerBaseStats, _deck);
            int randomSeed = _boardSettings.UseFixedRandomSeed ? _boardSettings.FixedRandomSeed : Environment.TickCount;
            BeginStage(standaloneRun, _encounter, randomSeed);
        }

        /// <summary>Validates data shared by both standalone and run-managed battle startup.</summary>
        private void ValidateSharedSerializedData()
        {
            if (_boardSettings == null || _presentationSettings == null || _eventChannel == null)
            {
                throw new InvalidOperationException("BattleController is missing board, presentation, or event channel data.");
            }
        }

        /// <summary>Validates only legacy standalone fallback data.</summary>
        private void ValidateStandaloneSerializedData()
        {
            ValidateSharedSerializedData();
            if (_playerBaseStats == null || _deck == null || _encounter == null)
            {
                throw new InvalidOperationException("BattleController standalone mode is missing player stats, deck, or encounter data.");
            }
        }
    }
}
