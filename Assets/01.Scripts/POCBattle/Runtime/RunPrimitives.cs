using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// High-level run phases above the existing battle state machine.
    /// </summary>
    public enum RunPhase
    {
        None = 0,
        StageSetup = 1,
        Battle = 2,
        Looting = 3,
        StageTransition = 4,
        RunComplete = 5,
        Defeat = 6
    }

    /// <summary>
    /// Explicit looting interaction substates used instead of multiple modal boolean flags.
    /// </summary>
    public enum LootInteractionPhase
    {
        None = 0,
        WaitingForWorldClick = 1,
        RewardDecision = 2,
        FullDeckDecision = 3,
        DiscardSelection = 4,
        Resolved = 5
    }

    /// <summary>
    /// Direction requested from the POC OnGUI stage fade presenter.
    /// </summary>
    public enum StageFadeDirection
    {
        Out = 0,
        In = 1
    }

    /// <summary>
    /// Immutable persistent run HUD snapshot.
    /// </summary>
    public readonly struct RunStatusSnapshot
    {
        private readonly int _stageNumber;
        private readonly int _totalStages;
        private readonly int _gold;
        private readonly int _deckCount;
        private readonly int _deckCapacity;

        /// <summary>Gets one-based current stage number.</summary>
        public int StageNumber => _stageNumber;

        /// <summary>Gets total stage count.</summary>
        public int TotalStages => _totalStages;

        /// <summary>Gets current run gold.</summary>
        public int Gold => _gold;

        /// <summary>Gets current physical deck item count.</summary>
        public int DeckCount => _deckCount;

        /// <summary>Gets player deck capacity shared by blocks and cell effects.</summary>
        public int DeckCapacity => _deckCapacity;

        /// <summary>Creates immutable run status data.</summary>
        public RunStatusSnapshot(int stageNumber, int totalStages, int gold, int deckCount, int deckCapacity)
        {
            _stageNumber = stageNumber;
            _totalStages = totalStages;
            _gold = gold;
            _deckCount = deckCount;
            _deckCapacity = deckCapacity;
        }
    }

    /// <summary>
    /// Immutable world loot presentation snapshot for any collectible deck item.
    /// </summary>
    public readonly struct LootDropSnapshot
    {
        private readonly bool _isVisible;
        private readonly DeckItemDefinitionSO _definition;
        private readonly Vector3 _worldPosition;

        /// <summary>Gets whether a world loot item should be visible.</summary>
        public bool IsVisible => _isVisible;

        /// <summary>Gets the offered player deck item definition.</summary>
        public DeckItemDefinitionSO Definition => _definition;

        /// <summary>Gets defeated-enemy world anchor used by loot presentation.</summary>
        public Vector3 WorldPosition => _worldPosition;

        /// <summary>Creates immutable loot drop presentation data.</summary>
        public LootDropSnapshot(bool isVisible, DeckItemDefinitionSO definition, Vector3 worldPosition)
        {
            _isVisible = isVisible;
            _definition = definition;
            _worldPosition = worldPosition;
        }
    }

    /// <summary>
    /// One runtime deck row used by the discard-selection OnGUI modal.
    /// </summary>
    public readonly struct DeckItemSelectionSnapshot
    {
        private readonly int _instanceId;
        private readonly DeckItemDefinitionSO _definition;
        private readonly bool _isSelected;

        /// <summary>Gets exact physical deck instance id.</summary>
        public int InstanceId => _instanceId;

        /// <summary>Gets immutable deck item definition.</summary>
        public DeckItemDefinitionSO Definition => _definition;

        /// <summary>Gets whether this physical item is currently selected for discard.</summary>
        public bool IsSelected => _isSelected;

        /// <summary>Creates one immutable discard-selection row.</summary>
        public DeckItemSelectionSnapshot(int instanceId, DeckItemDefinitionSO definition, bool isSelected)
        {
            _instanceId = instanceId;
            _definition = definition;
            _isSelected = isSelected;
        }
    }

    /// <summary>
    /// Snapshot for the full runtime deck while choosing up to three items to discard.
    /// </summary>
    public sealed class DeckDiscardSelectionSnapshot
    {
        /// <summary>Maximum discard selections allowed by the current loot rule.</summary>
        public const int MAX_DISCARD_COUNT = 3;

        private readonly DeckItemSelectionSnapshot[] _items;
        private readonly int _selectedCount;

        /// <summary>Gets runtime deck rows.</summary>
        public DeckItemSelectionSnapshot[] Items => _items;

        /// <summary>Gets currently selected discard count.</summary>
        public int SelectedCount => _selectedCount;

        /// <summary>Creates an immutable discard-selection snapshot.</summary>
        public DeckDiscardSelectionSnapshot(DeckItemSelectionSnapshot[] items, int selectedCount)
        {
            _items = items;
            _selectedCount = selectedCount;
        }
    }
}
