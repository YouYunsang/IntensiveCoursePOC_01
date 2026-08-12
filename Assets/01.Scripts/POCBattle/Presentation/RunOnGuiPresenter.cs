using DG.Tweening;
using PocBattle.Data;
using PocBattle.Runtime;
using UnityEngine;

namespace PocBattle.Presentation
{
    /// <summary>
    /// POC-only run/loot IMGUI presenter. It consumes immutable snapshots and emits requests without reading runtime models directly.
    /// </summary>
    public sealed class RunOnGuiPresenter : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared battle/run event hub.")]
        private BattleEventChannelSO _eventChannel;

        [SerializeField, Tooltip("Presentation tuning used for critical labels and fade timing.")]
        private BattlePresentationSettingsSO _presentationSettings;

        /// <summary>Current high-level run phase.</summary>
        private RunPhase _runPhase;

        /// <summary>Current explicit loot interaction substate.</summary>
        private LootInteractionPhase _lootInteractionPhase;

        /// <summary>Latest persistent run HUD snapshot.</summary>
        private RunStatusSnapshot _runStatus;

        /// <summary>Current offered loot definition.</summary>
        private BlockDefinitionSO _lootDefinition;

        /// <summary>Current runtime deck discard rows.</summary>
        private DeckDiscardSelectionSnapshot _discardSnapshot;

        /// <summary>Cached run status label rebuilt only when run status changes.</summary>
        private string _runStatusLabel = "STAGE -/-   |   GOLD 0   |   DECK -/-";

        /// <summary>Cached current loot effect label rebuilt only when loot changes.</summary>
        private string _lootLabel = "UNKNOWN REWARD";

        /// <summary>Cached per-row deck button labels rebuilt only when discard snapshot changes.</summary>
        private string[] _discardLabels;

        /// <summary>Cached selected-count label rebuilt only when discard snapshot changes.</summary>
        private string _discardCountLabel = "Selected: 0/3";

        /// <summary>Cached full-deck prompt rebuilt only when the loot definition changes.</summary>
        private string _fullDeckDecisionLabel = "UNKNOWN REWARD\n\nDiscard 1 to 3 existing blocks and take the new block?";

        /// <summary>Scroll position retained between IMGUI calls for the deck list.</summary>
        private Vector2 _deckScrollPosition;

        /// <summary>Current full-screen fade alpha.</summary>
        private float _fadeAlpha;

        /// <summary>Active fade tween.</summary>
        private Tween _fadeTween;

        /// <summary>Cached GUI styles initialized lazily inside OnGUI.</summary>
        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _selectedButtonStyle;

        /// <summary>Subscribes to run snapshots, decisions, and fade requests.</summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.RunPhaseChanged += HandleRunPhaseChanged;
            _eventChannel.RunStatusChanged += HandleRunStatusChanged;
            _eventChannel.LootInteractionPhaseChanged += HandleLootInteractionPhaseChanged;
            _eventChannel.LootDropChanged += HandleLootDropChanged;
            _eventChannel.DeckDiscardSelectionChanged += HandleDeckDiscardSelectionChanged;
            _eventChannel.StageFadeRequested += HandleStageFadeRequested;
        }

        /// <summary>Removes subscriptions and kills active presentation tween.</summary>
        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.RunPhaseChanged -= HandleRunPhaseChanged;
                _eventChannel.RunStatusChanged -= HandleRunStatusChanged;
                _eventChannel.LootInteractionPhaseChanged -= HandleLootInteractionPhaseChanged;
                _eventChannel.LootDropChanged -= HandleLootDropChanged;
                _eventChannel.DeckDiscardSelectionChanged -= HandleDeckDiscardSelectionChanged;
                _eventChannel.StageFadeRequested -= HandleStageFadeRequested;
            }

            KillFadeTween();
        }

        /// <summary>
        /// Draws only cached POC run information. Gameplay state changes occur through event requests from button clicks.
        /// </summary>
        private void OnGUI()
        {
            EnsureStyles();
            DrawRunStatus();
            DrawLootInteraction();
            DrawTerminalPrompt();
            DrawFadeOverlay();
        }

        /// <summary>Initializes reusable styles when GUI.skin is valid.</summary>
        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(16, 16, 14, 14)
            };
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            _centerStyle = new GUIStyle(_bodyStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            _selectedButtonStyle = new GUIStyle(_buttonStyle);
            _selectedButtonStyle.normal.textColor = Color.yellow;
        }

        /// <summary>Draws stage/gold/deck capacity information at the top center.</summary>
        private void DrawRunStatus()
        {
            if (_runPhase == RunPhase.None)
            {
                return;
            }

            Rect rect = RunOnGuiLayoutUtility.GetRunStatusRect();
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 6f, rect.width - 20f, rect.height - 12f), _runStatusLabel, _headerStyle);
        }

        /// <summary>Draws waiting guidance or the active reward/deck-selection modal during looting.</summary>
        private void DrawLootInteraction()
        {
            if (_runPhase != RunPhase.Looting)
            {
                return;
            }

            switch (_lootInteractionPhase)
            {
                case LootInteractionPhase.WaitingForWorldClick:
                    DrawLootWorldHint();
                    break;
                case LootInteractionPhase.RewardDecision:
                    DrawRewardDecision();
                    break;
                case LootInteractionPhase.FullDeckDecision:
                    DrawFullDeckDecision();
                    break;
                case LootInteractionPhase.DiscardSelection:
                    DrawDiscardSelection();
                    break;
            }
        }

        /// <summary>Draws a small instruction while the floating world block is the only loot target.</summary>
        private void DrawLootWorldHint()
        {
            Rect rect = RunOnGuiLayoutUtility.GetLootDecisionRect();
            Rect hintRect = new Rect(rect.x, rect.y + rect.height + 16f, rect.width, 44f);
            GUI.Box(hintRect, GUIContent.none, _panelStyle);
            GUI.Label(hintRect, "Click the floating glowing block to inspect the reward.", _centerStyle);
        }

        /// <summary>Draws normal take/leave choice when the runtime deck has room or capacity has not been checked yet.</summary>
        private void DrawRewardDecision()
        {
            Rect rect = RunOnGuiLayoutUtility.GetLootDecisionRect();
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 40f), "NEW BLOCK", _headerStyle);
            GUI.Label(new Rect(rect.x + 30f, rect.y + 76f, rect.width - 60f, 80f), FormatLootLabel(), _headerStyle);
            GUI.Label(new Rect(rect.x + 30f, rect.y + 145f, rect.width - 60f, 45f), "Add this block to your deck?", _centerStyle);

            float buttonWidth = (rect.width - 90f) * 0.5f;
            if (GUI.Button(new Rect(rect.x + 30f, rect.y + 215f, buttonWidth, 48f), "TAKE", _buttonStyle))
            {
                _eventChannel?.RaiseLootAcquireDecisionRequested(true);
            }

            if (GUI.Button(new Rect(rect.x + 60f + buttonWidth, rect.y + 215f, buttonWidth, 48f), "LEAVE", _buttonStyle))
            {
                _eventChannel?.RaiseLootAcquireDecisionRequested(false);
            }
        }

        /// <summary>Draws full-deck replacement decision without modifying the runtime deck yet.</summary>
        private void DrawFullDeckDecision()
        {
            Rect rect = RunOnGuiLayoutUtility.GetLootDecisionRect();
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 40f), "DECK FULL", _headerStyle);
            GUI.Label(new Rect(rect.x + 30f, rect.y + 70f, rect.width - 60f, 100f),
                _fullDeckDecisionLabel,
                _centerStyle);

            float buttonWidth = (rect.width - 90f) * 0.5f;
            if (GUI.Button(new Rect(rect.x + 30f, rect.y + 215f, buttonWidth, 48f), "DISCARD & TAKE", _buttonStyle))
            {
                _eventChannel?.RaiseLootReplaceDecisionRequested(true);
            }

            if (GUI.Button(new Rect(rect.x + 60f + buttonWidth, rect.y + 215f, buttonWidth, 48f), "LEAVE", _buttonStyle))
            {
                _eventChannel?.RaiseLootReplaceDecisionRequested(false);
            }
        }

        /// <summary>Draws every physical runtime deck block and allows at most three exact copies to be selected.</summary>
        private void DrawDiscardSelection()
        {
            Rect rect = RunOnGuiLayoutUtility.GetDeckDiscardRect();
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 14f, rect.width - 40f, 40f), "SELECT 1-3 BLOCKS TO DISCARD", _headerStyle);

            if (_discardSnapshot == null)
            {
                GUI.Label(new Rect(rect.x + 30f, rect.y + 80f, rect.width - 60f, 40f), "Waiting for deck snapshot...", _centerStyle);
                return;
            }

            Rect scrollRect = new Rect(rect.x + 28f, rect.y + 68f, rect.width - 56f, rect.height - 170f);
            float rowHeight = 44f;
            float contentHeight = Mathf.Max(scrollRect.height, _discardSnapshot.Items.Length * rowHeight);
            _deckScrollPosition = GUI.BeginScrollView(scrollRect, _deckScrollPosition, new Rect(0f, 0f, scrollRect.width - 20f, contentHeight));

            for (int itemIndex = 0; itemIndex < _discardSnapshot.Items.Length; itemIndex++)
            {
                DeckBlockSelectionSnapshot item = _discardSnapshot.Items[itemIndex];
                string buttonText = _discardLabels != null && itemIndex < _discardLabels.Length
                    ? _discardLabels[itemIndex]
                    : item.Definition != null ? item.Definition.DisplayName : "Unknown";
                GUIStyle style = item.IsSelected ? _selectedButtonStyle : _buttonStyle;
                if (GUI.Button(new Rect(0f, itemIndex * rowHeight, scrollRect.width - 28f, rowHeight - 5f), buttonText, style))
                {
                    _eventChannel?.RaiseDeckDiscardToggleRequested(item.InstanceId);
                }
            }

            GUI.EndScrollView();

            GUI.Label(
                new Rect(rect.x + 30f, rect.y + rect.height - 92f, 220f, 32f),
                _discardCountLabel,
                _bodyStyle);

            bool previousEnabled = GUI.enabled;
            GUI.enabled = _discardSnapshot.SelectedCount > 0;
            if (GUI.Button(new Rect(rect.x + rect.width - 300f, rect.y + rect.height - 100f, 120f, 48f), "CONFIRM", _buttonStyle))
            {
                _eventChannel?.RaiseDeckDiscardConfirmRequested();
            }
            GUI.enabled = previousEnabled;

            if (GUI.Button(new Rect(rect.x + rect.width - 160f, rect.y + rect.height - 100f, 120f, 48f), "CANCEL", _buttonStyle))
            {
                _eventChannel?.RaiseDeckDiscardCancelRequested();
            }
        }

        /// <summary>Draws run defeat and run-complete prompts owned by the top-level run system.</summary>
        private void DrawTerminalPrompt()
        {
            if (_runPhase != RunPhase.Defeat && _runPhase != RunPhase.RunComplete)
            {
                return;
            }

            Rect rect = RunOnGuiLayoutUtility.GetTerminalRect();
            GUI.Box(rect, GUIContent.none, _panelStyle);
            string title = _runPhase == RunPhase.Defeat ? "DEFEAT" : "RUN COMPLETE";
            string question = _runPhase == RunPhase.Defeat ? "Restart the run from Stage 1?" : "Start a new run?";
            GUI.Label(new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 48f), title, _headerStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 82f, rect.width - 40f, 42f), question, _centerStyle);

            float buttonWidth = (rect.width - 90f) * 0.5f;
            if (GUI.Button(new Rect(rect.x + 30f, rect.y + 160f, buttonWidth, 48f), "RESTART RUN", _buttonStyle))
            {
                if (_runPhase == RunPhase.Defeat)
                {
                    _eventChannel?.RaiseRunRestartDecisionRequested(true);
                }
                else
                {
                    _eventChannel?.RaiseRunCompleteDecisionRequested(true);
                }
            }

            if (GUI.Button(new Rect(rect.x + 60f + buttonWidth, rect.y + 160f, buttonWidth, 48f), "QUIT", _buttonStyle))
            {
                if (_runPhase == RunPhase.Defeat)
                {
                    _eventChannel?.RaiseRunRestartDecisionRequested(false);
                }
                else
                {
                    _eventChannel?.RaiseRunCompleteDecisionRequested(false);
                }
            }
        }

        /// <summary>Draws a black full-screen overlay after every other run/battle IMGUI element.</summary>
        private void DrawFadeOverlay()
        {
            if (_fadeAlpha <= 0f)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(_fadeAlpha));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        /// <summary>Formats the actual offered block effect values without asset-name parsing.</summary>
        private string FormatLootLabel()
        {
            return _lootLabel;
        }

        /// <summary>Caches current run phase.</summary>
        private void HandleRunPhaseChanged(RunPhase phase)
        {
            _runPhase = phase;
        }

        /// <summary>Caches persistent run HUD data.</summary>
        private void HandleRunStatusChanged(RunStatusSnapshot snapshot)
        {
            _runStatus = snapshot;
            _runStatusLabel = $"STAGE {snapshot.StageNumber}/{snapshot.TotalStages}   |   GOLD {snapshot.Gold}   |   DECK {snapshot.DeckCount}/{snapshot.DeckCapacity}";
        }

        /// <summary>Caches looting modal state.</summary>
        private void HandleLootInteractionPhaseChanged(LootInteractionPhase phase)
        {
            _lootInteractionPhase = phase;
        }

        /// <summary>Caches current offered loot definition while visible.</summary>
        private void HandleLootDropChanged(LootDropSnapshot snapshot)
        {
            _lootDefinition = snapshot.IsVisible ? snapshot.Definition : null;
            _lootLabel = _lootDefinition != null
                ? BlockEffectTextFormatter.Format(_lootDefinition, _presentationSettings.CriticalMultiplier)
                : "UNKNOWN REWARD";
            _fullDeckDecisionLabel = _lootLabel + "\n\nDiscard 1 to 3 existing blocks and take the new block?";
        }

        /// <summary>Caches runtime deck discard rows.</summary>
        private void HandleDeckDiscardSelectionChanged(DeckDiscardSelectionSnapshot snapshot)
        {
            _discardSnapshot = snapshot;
            if (snapshot == null)
            {
                _discardLabels = null;
                return;
            }

            _discardLabels = new string[snapshot.Items.Length];
            for (int itemIndex = 0; itemIndex < snapshot.Items.Length; itemIndex++)
            {
                DeckBlockSelectionSnapshot item = snapshot.Items[itemIndex];
                BlockDefinitionSO definition = item.Definition;
                string label = definition != null
                    ? BlockEffectTextFormatter.Format(definition, _presentationSettings.CriticalMultiplier).Replace("\n", " / ")
                    : "Unknown";
                _discardLabels[itemIndex] = item.IsSelected ? "[SELECTED]  " + label : label;
            }

            _discardCountLabel = $"Selected: {snapshot.SelectedCount}/{DeckDiscardSelectionSnapshot.MAX_DISCARD_COUNT}";
        }

        /// <summary>
        /// Tweens only presentation alpha, then reports completion to RunController through the event channel.
        /// </summary>
        private void HandleStageFadeRequested(StageFadeDirection direction)
        {
            KillFadeTween();
            float targetAlpha = direction == StageFadeDirection.Out ? 1f : 0f;
            _fadeTween = DOVirtual.Float(
                    _fadeAlpha,
                    targetAlpha,
                    _presentationSettings.StageFadeDuration,
                    value => _fadeAlpha = value)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _fadeTween = null;
                    _fadeAlpha = targetAlpha;
                    _eventChannel?.RaiseStageFadeVisualCompleted(direction);
                });
        }

        /// <summary>Kills only the active stage fade tween.</summary>
        private void KillFadeTween()
        {
            if (_fadeTween != null && _fadeTween.IsActive())
            {
                _fadeTween.Kill();
            }

            _fadeTween = null;
        }
    }
}
