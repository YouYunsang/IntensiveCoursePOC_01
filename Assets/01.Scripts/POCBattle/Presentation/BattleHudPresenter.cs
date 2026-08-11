using PocBattle.Core;
using PocBattle.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace PocBattle.Presentation
{
    /// <summary>
    /// MV presenter that maps battle event snapshots into simple POC UI text/buttons; the View never reads battle models directly.
    /// </summary>
    public sealed class BattleHudPresenter : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared event hub used instead of direct BattleController access.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Text displaying current high-level phase.")]
        private Text _phaseText;

        [SerializeField, Tooltip("Text displaying player HP and current shield.")]
        private Text _playerStatusText;

        [SerializeField, Tooltip("Text displaying remaining movement and edit resources.")]
        private Text _resourceText;

        [SerializeField, Tooltip("Text displaying collected attack, pending shield, critical stacks, and final damage preview.")]
        private Text _effectText;

        [SerializeField, Tooltip("Button used to manually finish Placement Edit or Movement.")]
        private Button _endPhaseButton;

        [SerializeField, Tooltip("Label shown inside the end-phase button.")]
        private Text _endPhaseButtonText;

        [SerializeField, Tooltip("Centered battle result text.")]
        private Text _resultText;

        [SerializeField, Tooltip("Restart button shown only after victory or defeat.")]
        private Button _restartButton;

        /// <summary>
        /// Subscribes to model-view snapshots and UI button actions.
        /// </summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.PhaseChanged += HandlePhaseChanged;
            _eventChannel.PlayerStatusChanged += HandlePlayerStatusChanged;
            _eventChannel.TurnResourcesChanged += HandleTurnResourcesChanged;
            _eventChannel.TurnEffectsChanged += HandleTurnEffectsChanged;
            _eventChannel.BattleResultChanged += HandleBattleResultChanged;

            if (_endPhaseButton != null)
            {
                _endPhaseButton.onClick.AddListener(HandleEndPhaseButtonClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(HandleRestartButtonClicked);
            }
        }

        /// <summary>
        /// Removes all event and UI button subscriptions.
        /// </summary>
        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.PhaseChanged -= HandlePhaseChanged;
                _eventChannel.PlayerStatusChanged -= HandlePlayerStatusChanged;
                _eventChannel.TurnResourcesChanged -= HandleTurnResourcesChanged;
                _eventChannel.TurnEffectsChanged -= HandleTurnEffectsChanged;
                _eventChannel.BattleResultChanged -= HandleBattleResultChanged;
            }

            if (_endPhaseButton != null)
            {
                _endPhaseButton.onClick.RemoveListener(HandleEndPhaseButtonClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestartButtonClicked);
            }
        }

        /// <summary>
        /// Displays current explicit phase and configures the only context-sensitive end-phase button.
        /// </summary>
        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (_phaseText != null)
            {
                _phaseText.text = $"PHASE: {GetPhaseLabel(phase)}";
            }

            bool canEndPhase = phase == BattlePhase.PlacementEdit || phase == BattlePhase.Movement;
            if (_endPhaseButton != null)
            {
                _endPhaseButton.interactable = canEndPhase;
                _endPhaseButton.gameObject.SetActive(canEndPhase);
            }

            if (_endPhaseButtonText != null)
            {
                _endPhaseButtonText.text = phase == BattlePhase.PlacementEdit ? "END EDIT" : "END MOVE";
            }
        }

        /// <summary>
        /// Maps immutable player status to view text.
        /// </summary>
        private void HandlePlayerStatusChanged(PlayerStatusSnapshot snapshot)
        {
            if (_playerStatusText != null)
            {
                _playerStatusText.text = $"PLAYER  HP {snapshot.CurrentHealth}/{snapshot.MaxHealth}   SHIELD {snapshot.Shield}";
            }
        }

        /// <summary>
        /// Maps immutable current-turn resource counters to view text.
        /// </summary>
        private void HandleTurnResourcesChanged(TurnResourceSnapshot snapshot)
        {
            if (_resourceText != null)
            {
                _resourceText.text = $"MOVE {snapshot.RemainingMoves}/{snapshot.MaxMoves}   EDIT {snapshot.RemainingEdits}/{snapshot.MaxEdits}";
            }
        }

        /// <summary>
        /// Maps immutable collected-effect preview to view text.
        /// </summary>
        private void HandleTurnEffectsChanged(TurnEffectSnapshot snapshot)
        {
            if (_effectText != null)
            {
                _effectText.text = $"ATK {snapshot.Attack}   SH +{snapshot.PendingShield}   CRIT x{snapshot.CriticalStacks}   FINAL DMG {snapshot.FinalDamagePreview}";
            }
        }

        /// <summary>
        /// Shows/hides terminal result and restart controls.
        /// </summary>
        private void HandleBattleResultChanged(BattleResult result)
        {
            bool hasResult = result != BattleResult.None;

            if (_resultText != null)
            {
                _resultText.gameObject.SetActive(hasResult);
                _resultText.text = result == BattleResult.Victory ? "VICTORY" : result == BattleResult.Defeat ? "DEFEAT" : string.Empty;
            }

            if (_restartButton != null)
            {
                _restartButton.gameObject.SetActive(hasResult);
            }
        }

        /// <summary>
        /// Raises one event-based manual phase completion request.
        /// </summary>
        private void HandleEndPhaseButtonClicked()
        {
            _eventChannel.RaiseEndPhaseRequested();
        }

        /// <summary>
        /// Raises one event-based battle restart request.
        /// </summary>
        private void HandleRestartButtonClicked()
        {
            _eventChannel.RaiseRestartRequested();
        }

        /// <summary>
        /// Converts phase enum to compact POC display text without exposing model data to the view.
        /// </summary>
        private static string GetPhaseLabel(BattlePhase phase)
        {
            switch (phase)
            {
                case BattlePhase.PlayerTurnSetup:
                    return "TURN SETUP";
                case BattlePhase.PlacementEdit:
                    return "PLACEMENT EDIT";
                case BattlePhase.Movement:
                    return "MOVEMENT";
                case BattlePhase.PlayerBattleResolve:
                    return "PLAYER BATTLE";
                case BattlePhase.EnemyTurn:
                    return "ENEMY TURN";
                case BattlePhase.Victory:
                    return "VICTORY";
                case BattlePhase.Defeat:
                    return "DEFEAT";
                default:
                    return "NONE";
            }
        }
    }
}
