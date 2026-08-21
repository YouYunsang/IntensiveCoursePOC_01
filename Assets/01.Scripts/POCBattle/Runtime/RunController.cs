using System;
using System.Collections.Generic;
using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Explicit stage-transition substates used while the screen is faded.
    /// </summary>
    internal enum StageTransitionStep
    {
        None = 0,
        WaitingForFadeOut = 1,
        WaitingForStageReady = 2,
        WaitingForFadeIn = 3
    }

    /// <summary>
    /// Reason the current stage transition was started.
    /// </summary>
    internal enum StageTransitionPurpose
    {
        None = 0,
        NextStage = 1,
        RestartRun = 2,
        DebugStageJump = 3
    }

    /// <summary>
    /// Top-level run coordinator. Owns persistent run progression and drives BattleController one-way without a reverse reference.
    /// </summary>
    [RequireComponent(typeof(BattleController))]
    public sealed class RunController : MonoBehaviour
    {
        /// <summary>Sentinel indicating that no defeated enemy anchor has been recorded for the current stage.</summary>
        private const int NO_ENEMY_INDEX = -1;

        [SerializeField, Tooltip("Same-GameObject stage battle controller. RunController owns startup order; BattleController never references RunController.")]
        private BattleController _battleController;

        [SerializeField, Tooltip("Player base HP, turn resources, and runtime deck capacity.")]
        private PlayerBaseStatsSO _playerBaseStats;

        [SerializeField, Tooltip("Immutable starting deck copied into PlayerDeckModel at the beginning of every run.")]
        private DeckDefinitionSO _startingDeck;

        [SerializeField, Tooltip("Five-stage POC run definition containing stage encounter and loot pools.")]
        private RunDefinitionSO _runDefinition;

        [SerializeField, Tooltip("Board settings used only to derive independent stage placement seeds.")]
        private BattleBoardSettingsSO _boardSettings;

        [SerializeField, Tooltip("Presentation settings used for loot fallback position and stage flow timing.")]
        private BattlePresentationSettingsSO _presentationSettings;

        [SerializeField, Tooltip("Shared event hub for battle results, run UI requests, loot, and fade presentation.")]
        private BattleEventChannelSO _eventChannel;

        /// <summary>Persistent player progression for the current run.</summary>
        private PlayerRunModel _runModel;

        /// <summary>Zero-based current stage index.</summary>
        private int _currentStageIndex;

        /// <summary>Current high-level run phase.</summary>
        private RunPhase _runPhase;

        /// <summary>Current explicit looting interaction substate.</summary>
        private LootInteractionPhase _lootInteractionPhase;

        /// <summary>Current stage transition substep.</summary>
        private StageTransitionStep _transitionStep;

        /// <summary>Current stage transition purpose.</summary>
        private StageTransitionPurpose _transitionPurpose;

        /// <summary>Zero-based target stage used only by an in-progress debug stage jump.</summary>
        private int _debugTargetStageIndex;

        /// <summary>Independent random source used only for stage encounter selection.</summary>
        private System.Random _stageRandom;

        /// <summary>Independent random source used only for loot reward selection.</summary>
        private System.Random _lootRandom;

        /// <summary>Independent random source used only to seed board placement per stage.</summary>
        private System.Random _boardSeedRandom;

        /// <summary>Current offered loot block during Looting.</summary>
        private DeckItemDefinitionSO _pendingLootDefinition;

        /// <summary>Latest defeated enemy index used as the loot drop anchor.</summary>
        private int _lastDefeatedEnemyIndex;

        /// <summary>Enemy world anchors received from EnemyPartyView without direct GameObject references.</summary>
        private Dictionary<int, Vector3> _enemyWorldAnchors;

        /// <summary>Selected runtime deck instance ids during discard replacement.</summary>
        private List<int> _selectedDiscardIds;

        /// <summary>
        /// Caches same-GameObject BattleController and allocates reusable collections.
        /// </summary>
        private void Awake()
        {
            if (_battleController == null)
            {
                _battleController = GetComponent<BattleController>();
            }

            _enemyWorldAnchors = new Dictionary<int, Vector3>(4);
            _selectedDiscardIds = new List<int>(DeckDiscardSelectionSnapshot.MAX_DISCARD_COUNT);
            _lastDefeatedEnemyIndex = NO_ENEMY_INDEX;
            _runPhase = RunPhase.None;
            _lootInteractionPhase = LootInteractionPhase.None;
            _transitionStep = StageTransitionStep.None;
            _transitionPurpose = StageTransitionPurpose.None;
            _debugTargetStageIndex = -1;
        }

        /// <summary>
        /// Subscribes to battle-result and run interaction events only through the shared event channel.
        /// </summary>
        private void OnEnable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.BattleResultChanged += HandleBattleResultChanged;
            _eventChannel.EnemyDefeated += HandleEnemyDefeated;
            _eventChannel.EnemyWorldAnchorChanged += HandleEnemyWorldAnchorChanged;
            _eventChannel.LootWorldClickedRequested += HandleLootWorldClickedRequested;
            _eventChannel.LootAcquireDecisionRequested += HandleLootAcquireDecisionRequested;
            _eventChannel.LootReplaceDecisionRequested += HandleLootReplaceDecisionRequested;
            _eventChannel.DeckDiscardToggleRequested += HandleDeckDiscardToggleRequested;
            _eventChannel.DeckDiscardConfirmRequested += HandleDeckDiscardConfirmRequested;
            _eventChannel.DeckDiscardCancelRequested += HandleDeckDiscardCancelRequested;
            _eventChannel.StageFadeVisualCompleted += HandleStageFadeVisualCompleted;
            _eventChannel.BlockLayoutVisualCompleted += HandleBlockLayoutVisualCompleted;
            _eventChannel.RunRestartDecisionRequested += HandleRunRestartDecisionRequested;
            _eventChannel.RunCompleteDecisionRequested += HandleRunCompleteDecisionRequested;
            _eventChannel.DebugStageJumpRequested += HandleDebugStageJumpRequested;
        }

        /// <summary>Removes every run-flow event subscription.</summary>
        private void OnDisable()
        {
            if (_eventChannel == null)
            {
                return;
            }

            _eventChannel.BattleResultChanged -= HandleBattleResultChanged;
            _eventChannel.EnemyDefeated -= HandleEnemyDefeated;
            _eventChannel.EnemyWorldAnchorChanged -= HandleEnemyWorldAnchorChanged;
            _eventChannel.LootWorldClickedRequested -= HandleLootWorldClickedRequested;
            _eventChannel.LootAcquireDecisionRequested -= HandleLootAcquireDecisionRequested;
            _eventChannel.LootReplaceDecisionRequested -= HandleLootReplaceDecisionRequested;
            _eventChannel.DeckDiscardToggleRequested -= HandleDeckDiscardToggleRequested;
            _eventChannel.DeckDiscardConfirmRequested -= HandleDeckDiscardConfirmRequested;
            _eventChannel.DeckDiscardCancelRequested -= HandleDeckDiscardCancelRequested;
            _eventChannel.StageFadeVisualCompleted -= HandleStageFadeVisualCompleted;
            _eventChannel.BlockLayoutVisualCompleted -= HandleBlockLayoutVisualCompleted;
            _eventChannel.RunRestartDecisionRequested -= HandleRunRestartDecisionRequested;
            _eventChannel.RunCompleteDecisionRequested -= HandleRunCompleteDecisionRequested;
            _eventChannel.DebugStageJumpRequested -= HandleDebugStageJumpRequested;
        }

        /// <summary>
        /// Starts a fresh run after every scene subscriber has completed OnEnable.
        /// </summary>
        private void Start()
        {
            ValidateSerializedData();
            StartFreshRun(false);
        }

        /// <summary>
        /// Caches same-GameObject BattleController for inspector-friendly setup.
        /// </summary>
        private void Reset()
        {
            _battleController = GetComponent<BattleController>();
        }

        /// <summary>
        /// Creates fresh persistent progression, independent RNG streams, and begins stage one.
        /// </summary>
        private void StartFreshRun(bool keepTransitionPhase)
        {
            _runModel = new PlayerRunModel(_playerBaseStats, _startingDeck);
            _currentStageIndex = 0;
            _pendingLootDefinition = null;
            _lastDefeatedEnemyIndex = NO_ENEMY_INDEX;
            _debugTargetStageIndex = -1;
            _enemyWorldAnchors.Clear();
            _selectedDiscardIds.Clear();
            SetLootInteractionPhase(LootInteractionPhase.None);
            InitializeRandomStreams();
            PublishRunStatus();
            BeginCurrentStageBattle(keepTransitionPhase);
        }

        /// <summary>
        /// Creates independent deterministic/random streams so board shuffle calls never change future enemy or loot selection.
        /// </summary>
        private void InitializeRandomStreams()
        {
            int baseSeed = _runDefinition.UseFixedRandomSeed ? _runDefinition.FixedRandomSeed : Environment.TickCount;
            _stageRandom = new System.Random(baseSeed);
            _lootRandom = new System.Random(unchecked(baseSeed * 397 ^ 0x4A39B70D));
            _boardSeedRandom = new System.Random(unchecked(baseSeed * 7919 ^ 0x1B56C4E9));
        }

        /// <summary>
        /// Selects one encounter from the current stage and starts BattleController while preserving run HP/deck/gold.
        /// </summary>
        private void BeginCurrentStageBattle(bool keepTransitionPhase)
        {
            StageDefinitionSO stage = GetCurrentStage();
            EncounterDefinitionSO encounter = SelectEncounter(stage);
            ValidateStageCapacity(encounter);
            _lastDefeatedEnemyIndex = NO_ENEMY_INDEX;
            _enemyWorldAnchors.Clear();
            _pendingLootDefinition = null;
            _selectedDiscardIds.Clear();
            SetLootInteractionPhase(LootInteractionPhase.None);

            if (!keepTransitionPhase)
            {
                SetRunPhase(RunPhase.StageSetup);
            }

            PublishRunStatus();
            int boardSeed = _boardSettings.UseFixedRandomSeed
                ? unchecked(_boardSettings.FixedRandomSeed + _currentStageIndex * 1009)
                : _boardSeedRandom.Next();
            StageBattleSetup stageSetup = new StageBattleSetup(encounter, stage.FieldEffects, boardSeed);
            _battleController.BeginStage(_runModel, stageSetup);

            if (!keepTransitionPhase)
            {
                SetRunPhase(RunPhase.Battle);
            }
        }

        /// <summary>
        /// Handles only terminal stage-battle results; intermediate BattleResult.None resets are ignored.
        /// </summary>
        private void HandleBattleResultChanged(BattleResult result)
        {
            if (_runPhase != RunPhase.Battle)
            {
                return;
            }

            if (result == BattleResult.Victory)
            {
                BeginLooting();
                return;
            }

            if (result == BattleResult.Defeat)
            {
                _pendingLootDefinition = null;
                _eventChannel.RaiseLootDropChanged(new LootDropSnapshot(false, null, Vector3.zero));
                SetLootInteractionPhase(LootInteractionPhase.None);
                SetRunPhase(RunPhase.Defeat);
            }
        }

        /// <summary>
        /// Grants gold exactly when an enemy transitions from alive to defeated and records its loot anchor index.
        /// </summary>
        private void HandleEnemyDefeated(int enemyIndex, int goldReward)
        {
            if (_runPhase != RunPhase.Battle || _runModel == null)
            {
                return;
            }

            _lastDefeatedEnemyIndex = enemyIndex;
            _runModel.AddGold(goldReward);
            PublishRunStatus();
        }

        /// <summary>
        /// Caches enemy world anchors published by EnemyPartyView for later world-loot placement.
        /// </summary>
        private void HandleEnemyWorldAnchorChanged(int enemyIndex, Vector3 worldPosition)
        {
            _enemyWorldAnchors[enemyIndex] = worldPosition;
        }

        /// <summary>
        /// Selects one weighted stage reward and shows it floating at the defeated enemy's world position.
        /// </summary>
        private void BeginLooting()
        {
            StageDefinitionSO stage = GetCurrentStage();
            _pendingLootDefinition = SelectLoot(stage);
            Vector3 lootAnchor = ResolveLootWorldAnchor();
            SetRunPhase(RunPhase.Looting);
            SetLootInteractionPhase(LootInteractionPhase.WaitingForWorldClick);
            _eventChannel.RaiseLootDropChanged(new LootDropSnapshot(true, _pendingLootDefinition, lootAnchor));
            PublishRunStatus();
        }

        /// <summary>
        /// Opens reward decision only while the floating loot is the active interaction target.
        /// </summary>
        private void HandleLootWorldClickedRequested()
        {
            if (_runPhase != RunPhase.Looting || _lootInteractionPhase != LootInteractionPhase.WaitingForWorldClick)
            {
                return;
            }

            SetLootInteractionPhase(LootInteractionPhase.RewardDecision);
        }

        /// <summary>
        /// Accepts/declines the reward. A full deck routes to its explicit replacement decision instead of mutating data early.
        /// </summary>
        private void HandleLootAcquireDecisionRequested(bool acquire)
        {
            if (_runPhase != RunPhase.Looting || _lootInteractionPhase != LootInteractionPhase.RewardDecision)
            {
                return;
            }

            if (!acquire)
            {
                ResolveLootAndContinue();
                return;
            }

            if (_runModel.Deck.HasFreeSlot)
            {
                if (!_runModel.Deck.TryAddItem(_pendingLootDefinition))
                {
                    throw new InvalidOperationException("Loot reward could not be added even though PlayerDeckModel reported a free slot.");
                }

                ResolveLootAndContinue();
                return;
            }

            SetLootInteractionPhase(LootInteractionPhase.FullDeckDecision);
        }

        /// <summary>
        /// Enters discard selection for a full deck or declines the reward.
        /// </summary>
        private void HandleLootReplaceDecisionRequested(bool replace)
        {
            if (_runPhase != RunPhase.Looting || _lootInteractionPhase != LootInteractionPhase.FullDeckDecision)
            {
                return;
            }

            if (!replace)
            {
                ResolveLootAndContinue();
                return;
            }

            _selectedDiscardIds.Clear();
            SetLootInteractionPhase(LootInteractionPhase.DiscardSelection);
            PublishDiscardSelection();
        }

        /// <summary>
        /// Toggles one physical runtime deck block while enforcing the maximum-three selection rule.
        /// </summary>
        private void HandleDeckDiscardToggleRequested(int instanceId)
        {
            if (_runPhase != RunPhase.Looting || _lootInteractionPhase != LootInteractionPhase.DiscardSelection)
            {
                return;
            }

            int selectedIndex = _selectedDiscardIds.IndexOf(instanceId);
            if (selectedIndex >= 0)
            {
                _selectedDiscardIds.RemoveAt(selectedIndex);
                PublishDiscardSelection();
                return;
            }

            if (_selectedDiscardIds.Count >= DeckDiscardSelectionSnapshot.MAX_DISCARD_COUNT
                || !_runModel.Deck.Contains(instanceId))
            {
                return;
            }

            _selectedDiscardIds.Add(instanceId);
            PublishDiscardSelection();
        }

        /// <summary>
        /// Removes one to three selected physical blocks and inserts the single new reward into the resulting free deck space.
        /// </summary>
        private void HandleDeckDiscardConfirmRequested()
        {
            if (_runPhase != RunPhase.Looting || _lootInteractionPhase != LootInteractionPhase.DiscardSelection)
            {
                return;
            }

            if (_selectedDiscardIds.Count <= 0 || _selectedDiscardIds.Count > DeckDiscardSelectionSnapshot.MAX_DISCARD_COUNT)
            {
                return;
            }

            for (int selectedIndex = 0; selectedIndex < _selectedDiscardIds.Count; selectedIndex++)
            {
                _runModel.Deck.RemoveItem(_selectedDiscardIds[selectedIndex]);
            }

            if (!_runModel.Deck.TryAddItem(_pendingLootDefinition))
            {
                throw new InvalidOperationException("Loot replacement failed after valid deck discards created free capacity.");
            }

            _selectedDiscardIds.Clear();
            ResolveLootAndContinue();
        }

        /// <summary>
        /// Returns from discard selection to the full-deck decision without mutating the runtime deck.
        /// </summary>
        private void HandleDeckDiscardCancelRequested()
        {
            if (_runPhase != RunPhase.Looting || _lootInteractionPhase != LootInteractionPhase.DiscardSelection)
            {
                return;
            }

            _selectedDiscardIds.Clear();
            SetLootInteractionPhase(LootInteractionPhase.FullDeckDecision);
        }

        /// <summary>
        /// Hides world loot, publishes deck/gold status, then completes the run or starts a fade transition to the next stage.
        /// </summary>
        private void ResolveLootAndContinue()
        {
            _eventChannel.RaiseLootDropChanged(new LootDropSnapshot(false, null, Vector3.zero));
            _pendingLootDefinition = null;
            _selectedDiscardIds.Clear();
            SetLootInteractionPhase(LootInteractionPhase.Resolved);
            PublishRunStatus();

            if (_currentStageIndex >= _runDefinition.Stages.Count - 1)
            {
                SetRunPhase(RunPhase.RunComplete);
                return;
            }

            BeginStageTransition(StageTransitionPurpose.NextStage);
        }

        /// <summary>
        /// Starts fade-out for either normal next-stage progression or a full run restart.
        /// </summary>
        private void BeginStageTransition(StageTransitionPurpose purpose)
        {
            _transitionPurpose = purpose;
            _transitionStep = StageTransitionStep.WaitingForFadeOut;
            SetRunPhase(RunPhase.StageTransition);
            _eventChannel.RaiseStageFadeRequested(StageFadeDirection.Out);
        }

        /// <summary>
        /// Advances stage/run data only while fully black, then waits for fade-in before re-enabling run battle input.
        /// </summary>
        private void HandleStageFadeVisualCompleted(StageFadeDirection direction)
        {
            if (_runPhase != RunPhase.StageTransition)
            {
                return;
            }

            if (_transitionStep == StageTransitionStep.WaitingForFadeOut && direction == StageFadeDirection.Out)
            {
                // Keep the screen fully black until the new board layout reaches its authoritative cell centers.
                // This prevents the player from seeing a half-rebuilt stage during fade-in.
                _transitionStep = StageTransitionStep.WaitingForStageReady;

                if (_transitionPurpose == StageTransitionPurpose.NextStage)
                {
                    _currentStageIndex++;
                    BeginCurrentStageBattle(true);
                }
                else if (_transitionPurpose == StageTransitionPurpose.RestartRun)
                {
                    StartFreshRun(true);
                }
                else if (_transitionPurpose == StageTransitionPurpose.DebugStageJump)
                {
                    if (_debugTargetStageIndex < 0 || _debugTargetStageIndex >= _runDefinition.Stages.Count)
                    {
                        throw new InvalidOperationException("Debug stage transition reached fade-out completion without a valid target stage.");
                    }

                    _currentStageIndex = _debugTargetStageIndex;
                    _debugTargetStageIndex = -1;
                    BeginCurrentStageBattle(true);
                }
                else
                {
                    throw new InvalidOperationException("Stage transition reached fade-out completion without a valid purpose.");
                }

                return;
            }

            if (_transitionStep == StageTransitionStep.WaitingForFadeIn && direction == StageFadeDirection.In)
            {
                _transitionStep = StageTransitionStep.None;
                _transitionPurpose = StageTransitionPurpose.None;
                _debugTargetStageIndex = -1;
                SetRunPhase(RunPhase.Battle);
            }
        }


        /// <summary>
        /// Starts fade-in only after BoardView reports that the newly created stage layout has finished settling.
        /// </summary>
        private void HandleBlockLayoutVisualCompleted()
        {
            if (_runPhase != RunPhase.StageTransition || _transitionStep != StageTransitionStep.WaitingForStageReady)
            {
                return;
            }

            _transitionStep = StageTransitionStep.WaitingForFadeIn;
            _eventChannel.RaiseStageFadeRequested(StageFadeDirection.In);
        }

        /// <summary>
        /// Handles development-only direct stage navigation while preserving the current run deck, gold, and living HP.
        /// A dead player is restored to full HP so a debug jump from the defeat screen can still start the requested stage.
        /// </summary>
        private void HandleDebugStageJumpRequested(int stageNumber)
        {
            if (_runModel == null
                || _runDefinition == null
                || _runPhase == RunPhase.None
                || _runPhase == RunPhase.StageTransition)
            {
                return;
            }

            int targetIndex = stageNumber - 1;
            if (targetIndex < 0
                || targetIndex >= _runDefinition.Stages.Count
                || targetIndex == _currentStageIndex)
            {
                return;
            }

            _battleController.AbortForExternalStageTransition();
            _eventChannel.RaiseLootDropChanged(new LootDropSnapshot(false, null, Vector3.zero));
            _pendingLootDefinition = null;
            _lastDefeatedEnemyIndex = NO_ENEMY_INDEX;
            _selectedDiscardIds.Clear();
            SetLootInteractionPhase(LootInteractionPhase.None);

            if (!_runModel.Player.IsAlive)
            {
                _runModel.Player.Heal(_runModel.Player.MaxHealth);
            }

            _debugTargetStageIndex = targetIndex;
            BeginStageTransition(StageTransitionPurpose.DebugStageJump);
        }

        /// <summary>
        /// Handles the defeat prompt. Restart performs a full run reset; decline exits the built player or stops editor play mode.
        /// </summary>
        private void HandleRunRestartDecisionRequested(bool restart)
        {
            if (_runPhase != RunPhase.Defeat)
            {
                return;
            }

            if (restart)
            {
                BeginStageTransition(StageTransitionPurpose.RestartRun);
                return;
            }

            ExitApplication();
        }

        /// <summary>
        /// Handles the run-complete prompt using the same full-run restart semantics as defeat.
        /// </summary>
        private void HandleRunCompleteDecisionRequested(bool restart)
        {
            if (_runPhase != RunPhase.RunComplete)
            {
                return;
            }

            if (restart)
            {
                BeginStageTransition(StageTransitionPurpose.RestartRun);
                return;
            }

            ExitApplication();
        }

        /// <summary>
        /// Publishes stage/gold/deck values as immutable UI data.
        /// </summary>
        private void PublishRunStatus()
        {
            if (_runModel == null)
            {
                return;
            }

            _eventChannel.RaiseRunStatusChanged(
                new RunStatusSnapshot(
                    _currentStageIndex + 1,
                    _runDefinition.Stages.Count,
                    _runModel.Gold,
                    _runModel.Deck.Count,
                    _runModel.Deck.Capacity));
        }

        /// <summary>
        /// Publishes exact physical runtime deck rows with selection state for the OnGUI discard list.
        /// </summary>
        private void PublishDiscardSelection()
        {
            IReadOnlyList<DeckItemInstance> deckItems = _runModel.Deck.Items;
            DeckItemSelectionSnapshot[] items = new DeckItemSelectionSnapshot[deckItems.Count];
            for (int itemIndex = 0; itemIndex < deckItems.Count; itemIndex++)
            {
                DeckItemInstance item = deckItems[itemIndex];
                items[itemIndex] = new DeckItemSelectionSnapshot(
                    item.InstanceId,
                    item.Definition,
                    _selectedDiscardIds.Contains(item.InstanceId));
            }

            _eventChannel.RaiseDeckDiscardSelectionChanged(
                new DeckDiscardSelectionSnapshot(items, _selectedDiscardIds.Count));
        }

        /// <summary>Updates and publishes the high-level run phase.</summary>
        private void SetRunPhase(RunPhase phase)
        {
            _runPhase = phase;
            _eventChannel.RaiseRunPhaseChanged(phase);
        }

        /// <summary>Updates and publishes the explicit looting interaction substate.</summary>
        private void SetLootInteractionPhase(LootInteractionPhase phase)
        {
            _lootInteractionPhase = phase;
            _eventChannel.RaiseLootInteractionPhaseChanged(phase);
        }

        /// <summary>Returns the configured current stage or fails fast for invalid run data.</summary>
        private StageDefinitionSO GetCurrentStage()
        {
            if (_currentStageIndex < 0 || _currentStageIndex >= _runDefinition.Stages.Count)
            {
                throw new InvalidOperationException($"Current stage index {_currentStageIndex} is outside configured run stages.");
            }

            StageDefinitionSO stage = _runDefinition.Stages[_currentStageIndex];
            return stage != null ? stage : throw new InvalidOperationException($"Run stage {_currentStageIndex + 1} is null.");
        }

        /// <summary>Selects one non-null encounter uniformly from the stage pool.</summary>
        private EncounterDefinitionSO SelectEncounter(StageDefinitionSO stage)
        {
            int validCount = 0;
            for (int encounterIndex = 0; encounterIndex < stage.EncounterPool.Count; encounterIndex++)
            {
                if (stage.EncounterPool[encounterIndex] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                throw new InvalidOperationException($"Stage '{stage.name}' has no valid encounter candidates.");
            }

            int selectedValidIndex = _stageRandom.Next(0, validCount);
            int visitedValidCount = 0;
            for (int encounterIndex = 0; encounterIndex < stage.EncounterPool.Count; encounterIndex++)
            {
                EncounterDefinitionSO encounter = stage.EncounterPool[encounterIndex];
                if (encounter == null)
                {
                    continue;
                }

                if (visitedValidCount == selectedValidIndex)
                {
                    return encounter;
                }

                visitedValidCount++;
            }

            throw new InvalidOperationException($"Encounter selection failed for stage '{stage.name}'.");
        }

        /// <summary>Selects one player-collectible deck item using positive weighted stage loot entries.</summary>
        private DeckItemDefinitionSO SelectLoot(StageDefinitionSO stage)
        {
            int totalWeight = 0;
            for (int entryIndex = 0; entryIndex < stage.LootPool.Count; entryIndex++)
            {
                WeightedBlockRewardEntry entry = stage.LootPool[entryIndex];
                if (entry != null && entry.Item != null && entry.Item.IsPlayerCollectible)
                {
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight <= 0)
            {
                throw new InvalidOperationException($"Stage '{stage.name}' has no valid positive-weight player loot.");
            }

            int roll = _lootRandom.Next(0, totalWeight);
            int accumulatedWeight = 0;
            for (int entryIndex = 0; entryIndex < stage.LootPool.Count; entryIndex++)
            {
                WeightedBlockRewardEntry entry = stage.LootPool[entryIndex];
                if (entry == null || entry.Item == null || !entry.Item.IsPlayerCollectible || entry.Weight <= 0)
                {
                    continue;
                }

                accumulatedWeight += entry.Weight;
                if (roll < accumulatedWeight)
                {
                    return entry.Item;
                }
            }

            throw new InvalidOperationException($"Weighted loot selection failed for stage '{stage.name}'.");
        }

        /// <summary>
        /// Uses the latest defeated enemy view anchor, falling back to configured first enemy position if presentation arrived late.
        /// </summary>
        private Vector3 ResolveLootWorldAnchor()
        {
            if (_lastDefeatedEnemyIndex != NO_ENEMY_INDEX
                && _enemyWorldAnchors.TryGetValue(_lastDefeatedEnemyIndex, out Vector3 worldPosition))
            {
                return worldPosition;
            }

            return _presentationSettings.EnemyStartPosition;
        }

        /// <summary>
        /// Validates total deck occupancy and blocking-only occupancy independently. Cell effects occupy cells but do not block sliding.
        /// </summary>
        private void ValidateStageCapacity(EncounterDefinitionSO encounter)
        {
            int totalNormalCapacity = _boardSettings.Columns * _boardSettings.Rows - 4 - 1;
            int guaranteedBlockingCapacity = totalNormalCapacity - 1;
            int trapCount = encounter.TrapDefinition != null ? encounter.TrapCount : 0;
            int totalOccupiedCount = _runModel.Deck.Count + trapCount;
            int totalBlockingCount = _runModel.Deck.BlockCount + trapCount;

            if (_runModel.Deck.Count > _boardSettings.MaxDeckItemCount)
            {
                throw new InvalidOperationException(
                    $"Runtime deck count {_runModel.Deck.Count} exceeds board MaxDeckItemCount {_boardSettings.MaxDeckItemCount}.");
            }

            if (totalOccupiedCount > totalNormalCapacity)
            {
                throw new InvalidOperationException(
                    $"Stage needs {totalOccupiedCount} deck/trap occupied cells, but board capacity excluding walls/player is {totalNormalCapacity}.");
            }

            if (totalBlockingCount > guaranteedBlockingCapacity)
            {
                throw new InvalidOperationException(
                    $"Stage needs {totalBlockingCount} blocking block/trap cells, but guaranteed-movement capacity is {guaranteedBlockingCapacity}.");
            }
        }

        /// <summary>Fails fast when run composition data is incomplete.</summary>
        private void ValidateSerializedData()
        {
            if (_battleController == null
                || _playerBaseStats == null
                || _startingDeck == null
                || _runDefinition == null
                || _boardSettings == null
                || _presentationSettings == null
                || _eventChannel == null)
            {
                throw new InvalidOperationException("RunController is missing one or more required serialized references.");
            }

            if (_runDefinition.Stages.Count == 0)
            {
                throw new InvalidOperationException("RunDefinitionSO must contain at least one stage.");
            }

            if (_playerBaseStats.DeckCapacity > _boardSettings.MaxDeckItemCount)
            {
                throw new InvalidOperationException(
                    $"Player deck capacity {_playerBaseStats.DeckCapacity} exceeds board MaxDeckItemCount {_boardSettings.MaxDeckItemCount}.");
            }
        }

        /// <summary>Exits player builds and stops Play Mode while testing in the Unity Editor.</summary>
        private static void ExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
