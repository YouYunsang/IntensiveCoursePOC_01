using System.Collections.Generic;
using PocBattle.Core;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// POC-only IMGUI presenter. It caches event snapshots and never reads battle runtime models directly.
    /// </summary>
    public sealed class BattleOnGuiPresenter : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared event hub used instead of direct BattleController or Enemy references.")]
        private BattleEventChannelSO _eventChannel;

        /// <summary>Current explicit battle phase received through the event channel.</summary>
        private BattlePhase _currentPhase;

        /// <summary>Current terminal battle result.</summary>
        private BattleResult _battleResult;

        /// <summary>Current top-level run phase. Non-None means terminal battle UI is owned by RunOnGuiPresenter.</summary>
        private RunPhase _currentRunPhase;

        /// <summary>Cached phase label rebuilt only when phase changes.</summary>
        private string _phaseLabel = "PHASE: NONE";

        /// <summary>Cached player HP/shield label rebuilt only when player status changes.</summary>
        private string _playerStatusLabel = "PLAYER  HP -/-   SHIELD -";

        /// <summary>Cached move/edit resource label rebuilt only when resources change.</summary>
        private string _resourceLabel = "MOVE -/-   EDIT -/-";

        /// <summary>Cached collected-effect label rebuilt only when effects change.</summary>
        private string _effectLabel = "ATK 0   SH +0   CRIT x0   FINAL DMG 0";

        /// <summary>Cached context-sensitive manual phase button label.</summary>
        private string _endPhaseButtonLabel = "END PHASE";

        /// <summary>Cached terminal result label.</summary>
        private string _resultLabel = string.Empty;

        /// <summary>Reusable enemy POC status caches indexed by stable party index.</summary>
        private List<EnemyGuiState> _enemyStates;

        /// <summary>GUI style cache initialized once during the first valid OnGUI call.</summary>
        private GUIStyle _panelStyle;

        /// <summary>GUI style cache for header labels.</summary>
        private GUIStyle _headerStyle;

        /// <summary>GUI style cache for normal status labels.</summary>
        private GUIStyle _bodyStyle;

        /// <summary>GUI style cache for centered result labels.</summary>
        private GUIStyle _resultStyle;

        /// <summary>GUI style cache for buttons.</summary>
        private GUIStyle _buttonStyle;

        /// <summary>
        /// Allocates reusable enemy presentation state once.
        /// </summary>
        private void Awake()
        {
            _enemyStates = new List<EnemyGuiState>(4);
        }

        /// <summary>
        /// Subscribes to immutable battle presentation snapshots only.
        /// </summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.PhaseChanged += HandlePhaseChanged;
            _eventChannel.RunPhaseChanged += HandleRunPhaseChanged;
            _eventChannel.PlayerStatusChanged += HandlePlayerStatusChanged;
            _eventChannel.TurnResourcesChanged += HandleTurnResourcesChanged;
            _eventChannel.TurnEffectsChanged += HandleTurnEffectsChanged;
            _eventChannel.EnemyPartySetupRequested += HandleEnemyPartySetupRequested;
            _eventChannel.EnemyStatusChanged += HandleEnemyStatusChanged;
            _eventChannel.EnemyIntentChanged += HandleEnemyIntentChanged;
            _eventChannel.BattleResultChanged += HandleBattleResultChanged;
        }

        /// <summary>
        /// Removes every event subscription when this presenter is disabled.
        /// </summary>
        private void OnDisable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.PhaseChanged -= HandlePhaseChanged;
            _eventChannel.RunPhaseChanged -= HandleRunPhaseChanged;
            _eventChannel.PlayerStatusChanged -= HandlePlayerStatusChanged;
            _eventChannel.TurnResourcesChanged -= HandleTurnResourcesChanged;
            _eventChannel.TurnEffectsChanged -= HandleTurnEffectsChanged;
            _eventChannel.EnemyPartySetupRequested -= HandleEnemyPartySetupRequested;
            _eventChannel.EnemyStatusChanged -= HandleEnemyStatusChanged;
            _eventChannel.EnemyIntentChanged -= HandleEnemyIntentChanged;
            _eventChannel.BattleResultChanged -= HandleBattleResultChanged;
        }

        /// <summary>
        /// Draws cached POC information and emits only event-channel requests from button interactions.
        /// No battle calculation is performed from OnGUI.
        /// </summary>
        private void OnGUI()
        {
            // Standalone battle mode has RunPhase.None. In run-managed mode, battle HUD is intentionally hidden
            // during looting, fade transitions, and terminal run prompts so modal/world-loot input cannot overlap stale battle UI.
            if (_currentRunPhase != RunPhase.None && _currentRunPhase != RunPhase.Battle)
            {
                return;
            }

            EnsureStyles();
            DrawPlayerBattlePanel();
            DrawEnemyPanels();
            DrawManualPhaseButton();
            DrawBattleResult();
        }

        /// <summary>
        /// Initializes GUI styles lazily because GUI.skin is valid only during IMGUI processing.
        /// </summary>
        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft
            };

            _resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
        }

        /// <summary>
        /// Draws phase, player status, turn resources, and accumulated player effects.
        /// </summary>
        private void DrawPlayerBattlePanel()
        {
            Rect panelRect = BattleOnGuiLayoutUtility.GetStatusPanelRect();
            GUI.Box(panelRect, GUIContent.none, _panelStyle);

            float x = panelRect.x + 14f;
            float y = panelRect.y + 10f;
            float width = panelRect.width - 28f;
            GUI.Label(new Rect(x, y, width, 28f), _phaseLabel, _headerStyle);
            GUI.Label(new Rect(x, y + 36f, width, 24f), _playerStatusLabel, _bodyStyle);
            GUI.Label(new Rect(x, y + 67f, width, 24f), _resourceLabel, _bodyStyle);
            GUI.Label(new Rect(x, y + 98f, width, 24f), _effectLabel, _bodyStyle);
            GUI.Label(new Rect(x, y + 129f, width, 24f), "WASD / Arrow Keys: Move   |   Mouse: Edit", _bodyStyle);
        }

        /// <summary>
        /// Draws one cached enemy status/intent panel per active encounter enemy.
        /// </summary>
        private void DrawEnemyPanels()
        {
            int displayIndex = 0;
            for (int enemyIndex = 0; enemyIndex < _enemyStates.Count; enemyIndex++)
            {
                EnemyGuiState state = _enemyStates[enemyIndex];
                if (!state.IsActive)
                {
                    continue;
                }

                Rect panelRect = BattleOnGuiLayoutUtility.GetEnemyPanelRect(displayIndex);
                GUI.Box(panelRect, GUIContent.none, _panelStyle);
                GUI.Label(
                    new Rect(panelRect.x + 12f, panelRect.y + 8f, panelRect.width - 24f, 24f),
                    state.DisplayName,
                    _headerStyle);
                GUI.Label(
                    new Rect(panelRect.x + 12f, panelRect.y + 36f, panelRect.width - 24f, 20f),
                    state.StatusLabel,
                    _bodyStyle);
                GUI.Label(
                    new Rect(panelRect.x + 12f, panelRect.y + 59f, panelRect.width - 24f, 20f),
                    state.IntentLabel,
                    _bodyStyle);
                displayIndex++;
            }
        }

        /// <summary>
        /// Draws the context-sensitive END EDIT / END MOVE button when manual phase completion is allowed.
        /// </summary>
        private void DrawManualPhaseButton()
        {
            if ((_currentRunPhase != RunPhase.None && _currentRunPhase != RunPhase.Battle)
                || !BattleOnGuiLayoutUtility.CanManuallyEndPhase(_currentPhase))
            {
                return;
            }

            if (GUI.Button(BattleOnGuiLayoutUtility.GetEndPhaseButtonRect(), _endPhaseButtonLabel, _buttonStyle))
            {
                _eventChannel?.RaiseEndPhaseRequested();
            }
        }

        /// <summary>
        /// Draws victory/defeat feedback and restart button only after the battle reaches a terminal result.
        /// </summary>
        private void DrawBattleResult()
        {
            if (_currentRunPhase != RunPhase.None || _battleResult == BattleResult.None)
            {
                return;
            }

            Rect resultRect = BattleOnGuiLayoutUtility.GetResultPanelRect();
            GUI.Box(resultRect, GUIContent.none, _panelStyle);
            GUI.Label(
                new Rect(resultRect.x, resultRect.y + 16f, resultRect.width, 70f),
                _resultLabel,
                _resultStyle);

            if (GUI.Button(BattleOnGuiLayoutUtility.GetRestartButtonRect(), "RESTART", _buttonStyle))
            {
                _eventChannel?.RaiseRestartRequested();
            }
        }

        /// <summary>
        /// Caches current phase and its human-readable POC label.
        /// </summary>
        private void HandlePhaseChanged(BattlePhase phase)
        {
            _currentPhase = phase;
            _phaseLabel = $"PHASE: {GetPhaseLabel(phase)}";
            _endPhaseButtonLabel = phase == BattlePhase.PlacementEdit ? "END EDIT" : "END MOVE";
        }

        /// <summary>Stores the top-level run phase so run-managed victory/defeat overlays are not duplicated.</summary>
        private void HandleRunPhaseChanged(RunPhase phase)
        {
            _currentRunPhase = phase;
        }

        /// <summary>
        /// Caches player HP/shield text only when the underlying snapshot changes.
        /// </summary>
        private void HandlePlayerStatusChanged(PlayerStatusSnapshot snapshot)
        {
            _playerStatusLabel = $"PLAYER  HP {snapshot.CurrentHealth}/{snapshot.MaxHealth}   SHIELD {snapshot.Shield}";
        }

        /// <summary>
        /// Caches move/edit resource text only when the underlying snapshot changes.
        /// </summary>
        private void HandleTurnResourcesChanged(TurnResourceSnapshot snapshot)
        {
            _resourceLabel = $"MOVE {snapshot.RemainingMoves}/{snapshot.MaxMoves}   EDIT {snapshot.RemainingEdits}/{snapshot.MaxEdits}";
        }

        /// <summary>
        /// Caches accumulated block-effect preview text only when the underlying snapshot changes.
        /// </summary>
        private void HandleTurnEffectsChanged(TurnEffectSnapshot snapshot)
        {
            _effectLabel =
                $"ATK {snapshot.Attack}   SH +{snapshot.PendingShield}   CRIT x{snapshot.CriticalStacks}   FINAL DMG {snapshot.FinalDamagePreview}";
        }

        /// <summary>
        /// Activates/reuses enemy UI cache slots from the current encounter setup.
        /// </summary>
        private void HandleEnemyPartySetupRequested(IReadOnlyList<EnemySetupSnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            for (int enemyIndex = 0; enemyIndex < _enemyStates.Count; enemyIndex++)
            {
                _enemyStates[enemyIndex].IsActive = false;
            }

            for (int snapshotIndex = 0; snapshotIndex < snapshots.Count; snapshotIndex++)
            {
                EnemySetupSnapshot snapshot = snapshots[snapshotIndex];
                EnemyGuiState state = GetOrCreateEnemyState(snapshot.EnemyIndex);
                state.IsActive = true;
                state.DisplayName = snapshot.DisplayName;
                state.StatusLabel = $"HP {snapshot.MaxHealth}/{snapshot.MaxHealth}   SHIELD 0";
                state.IntentLabel = "INTENT: ...";
            }
        }

        /// <summary>
        /// Caches one enemy HP/shield state by stable party index.
        /// </summary>
        private void HandleEnemyStatusChanged(EnemyStatusSnapshot snapshot)
        {
            EnemyGuiState state = GetOrCreateEnemyState(snapshot.EnemyIndex);
            state.StatusLabel = snapshot.IsAlive
                ? $"HP {snapshot.CurrentHealth}/{snapshot.MaxHealth}   SHIELD {snapshot.Shield}"
                : "DEFEATED";
        }

        /// <summary>
        /// Caches one enemy deterministic next-action intent by stable party index.
        /// </summary>
        private void HandleEnemyIntentChanged(EnemyIntentSnapshot snapshot)
        {
            EnemyGuiState state = GetOrCreateEnemyState(snapshot.EnemyIndex);
            state.IntentLabel = $"INTENT: {snapshot.IntentText}";
        }

        /// <summary>
        /// Caches terminal result text without changing battle state directly.
        /// </summary>
        private void HandleBattleResultChanged(BattleResult result)
        {
            _battleResult = result;
            _resultLabel = result == BattleResult.Victory
                ? "VICTORY"
                : result == BattleResult.Defeat
                    ? "DEFEAT"
                    : string.Empty;
        }

        /// <summary>
        /// Returns an existing reusable enemy cache slot or expands the list up to the requested stable party index.
        /// </summary>
        private EnemyGuiState GetOrCreateEnemyState(int enemyIndex)
        {
            while (_enemyStates.Count <= enemyIndex)
            {
                _enemyStates.Add(new EnemyGuiState());
            }

            return _enemyStates[enemyIndex];
        }

        /// <summary>
        /// Converts explicit battle phase enum values into compact POC labels.
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

        /// <summary>
        /// Mutable presentation-only enemy cache. It contains no runtime model references.
        /// </summary>
        private sealed class EnemyGuiState
        {
            /// <summary>Whether the latest encounter setup contains this party index.</summary>
            public bool IsActive { get; set; }

            /// <summary>Enemy display name copied from setup snapshot.</summary>
            public string DisplayName { get; set; } = "ENEMY";

            /// <summary>Cached HP/shield display text.</summary>
            public string StatusLabel { get; set; } = "HP -/-   SHIELD -";

            /// <summary>Cached intent display text.</summary>
            public string IntentLabel { get; set; } = "INTENT: ...";
        }
    }
}
