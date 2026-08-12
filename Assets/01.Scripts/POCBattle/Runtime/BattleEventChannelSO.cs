using System;
using System.Collections.Generic;
using PocBattle.Core;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Immutable player combat snapshot sent to presentation subscribers.
    /// </summary>
    public readonly struct PlayerStatusSnapshot
    {
        /// <summary>Current HP.</summary>
        private readonly int _currentHealth;

        /// <summary>Maximum HP.</summary>
        private readonly int _maxHealth;

        /// <summary>Current shield.</summary>
        private readonly int _shield;

        /// <summary>Gets current HP.</summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>Gets maximum HP.</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>Gets current shield.</summary>
        public int Shield => _shield;

        /// <summary>
        /// Creates an immutable player status snapshot.
        /// </summary>
        public PlayerStatusSnapshot(int currentHealth, int maxHealth, int shield)
        {
            _currentHealth = currentHealth;
            _maxHealth = maxHealth;
            _shield = shield;
        }
    }

    /// <summary>
    /// Immutable current-turn move/edit counters.
    /// </summary>
    public readonly struct TurnResourceSnapshot
    {
        /// <summary>Remaining moves.</summary>
        private readonly int _remainingMoves;

        /// <summary>Maximum moves.</summary>
        private readonly int _maxMoves;

        /// <summary>Remaining edits.</summary>
        private readonly int _remainingEdits;

        /// <summary>Maximum edits.</summary>
        private readonly int _maxEdits;

        /// <summary>Gets remaining moves.</summary>
        public int RemainingMoves => _remainingMoves;

        /// <summary>Gets maximum moves.</summary>
        public int MaxMoves => _maxMoves;

        /// <summary>Gets remaining edits.</summary>
        public int RemainingEdits => _remainingEdits;

        /// <summary>Gets maximum edits.</summary>
        public int MaxEdits => _maxEdits;

        /// <summary>
        /// Creates an immutable turn resource snapshot.
        /// </summary>
        public TurnResourceSnapshot(int remainingMoves, int maxMoves, int remainingEdits, int maxEdits)
        {
            _remainingMoves = remainingMoves;
            _maxMoves = maxMoves;
            _remainingEdits = remainingEdits;
            _maxEdits = maxEdits;
        }
    }

    /// <summary>
    /// Immutable collected-effect preview for the current player turn.
    /// </summary>
    public readonly struct TurnEffectSnapshot
    {
        /// <summary>Raw accumulated attack.</summary>
        private readonly int _attack;

        /// <summary>Pending shield.</summary>
        private readonly int _pendingShield;

        /// <summary>Critical stack count.</summary>
        private readonly int _criticalStacks;

        /// <summary>Rounded final damage preview.</summary>
        private readonly int _finalDamagePreview;

        /// <summary>Gets raw attack.</summary>
        public int Attack => _attack;

        /// <summary>Gets pending shield.</summary>
        public int PendingShield => _pendingShield;

        /// <summary>Gets critical stacks.</summary>
        public int CriticalStacks => _criticalStacks;

        /// <summary>Gets rounded final damage preview.</summary>
        public int FinalDamagePreview => _finalDamagePreview;

        /// <summary>
        /// Creates an immutable turn effect snapshot.
        /// </summary>
        public TurnEffectSnapshot(int attack, int pendingShield, int criticalStacks, int finalDamagePreview)
        {
            _attack = attack;
            _pendingShield = pendingShield;
            _criticalStacks = criticalStacks;
            _finalDamagePreview = finalDamagePreview;
        }
    }

    /// <summary>
    /// Lightweight block presentation snapshot decoupled from BlockView GameObjects.
    /// </summary>
    public readonly struct BlockSnapshot
    {
        /// <summary>Stable block runtime id.</summary>
        private readonly int _blockId;

        /// <summary>Immutable visual/effect definition.</summary>
        private readonly BlockDefinitionSO _definition;

        /// <summary>Current logical coordinate.</summary>
        private readonly Vector2Int _coordinate;

        /// <summary>Gets stable block id.</summary>
        public int BlockId => _blockId;

        /// <summary>Gets block definition.</summary>
        public BlockDefinitionSO Definition => _definition;

        /// <summary>Gets current coordinate.</summary>
        public Vector2Int Coordinate => _coordinate;

        /// <summary>
        /// Creates an immutable block snapshot.
        /// </summary>
        public BlockSnapshot(int blockId, BlockDefinitionSO definition, Vector2Int coordinate)
        {
            _blockId = blockId;
            _definition = definition;
            _coordinate = coordinate;
        }
    }

    /// <summary>
    /// Visual movement command generated from an already-resolved logical move.
    /// </summary>
    public readonly struct PlayerMoveVisualCommand
    {
        /// <summary>Whether the move actually changes logical cells.</summary>
        private readonly bool _isValid;

        /// <summary>Logical starting coordinate.</summary>
        private readonly Vector2Int _startPosition;

        /// <summary>Logical destination coordinate.</summary>
        private readonly Vector2Int _destination;

        /// <summary>Input direction used for invalid feedback.</summary>
        private readonly Vector2Int _direction;

        /// <summary>Gets whether this is a valid slide.</summary>
        public bool IsValid => _isValid;

        /// <summary>Gets start coordinate.</summary>
        public Vector2Int StartPosition => _startPosition;

        /// <summary>Gets destination coordinate.</summary>
        public Vector2Int Destination => _destination;

        /// <summary>Gets input direction.</summary>
        public Vector2Int Direction => _direction;

        /// <summary>
        /// Creates one immutable visual movement command.
        /// </summary>
        public PlayerMoveVisualCommand(bool isValid, Vector2Int startPosition, Vector2Int destination, Vector2Int direction)
        {
            _isValid = isValid;
            _startPosition = startPosition;
            _destination = destination;
            _direction = direction;
        }
    }

    /// <summary>
    /// Immutable enemy view setup data.
    /// </summary>
    public readonly struct EnemySetupSnapshot
    {
        /// <summary>Stable party index.</summary>
        private readonly int _enemyIndex;

        /// <summary>Displayed name.</summary>
        private readonly string _displayName;

        /// <summary>POC sprite tint.</summary>
        private readonly Color _displayColor;

        /// <summary>Maximum HP.</summary>
        private readonly int _maxHealth;

        /// <summary>Gets stable party index.</summary>
        public int EnemyIndex => _enemyIndex;

        /// <summary>Gets displayed name.</summary>
        public string DisplayName => _displayName;

        /// <summary>Gets sprite tint.</summary>
        public Color DisplayColor => _displayColor;

        /// <summary>Gets maximum HP.</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>
        /// Creates immutable enemy setup data.
        /// </summary>
        public EnemySetupSnapshot(int enemyIndex, string displayName, Color displayColor, int maxHealth)
        {
            _enemyIndex = enemyIndex;
            _displayName = displayName;
            _displayColor = displayColor;
            _maxHealth = maxHealth;
        }
    }

    /// <summary>
    /// Immutable current enemy combat status.
    /// </summary>
    public readonly struct EnemyStatusSnapshot
    {
        /// <summary>Stable party index.</summary>
        private readonly int _enemyIndex;

        /// <summary>Current HP.</summary>
        private readonly int _currentHealth;

        /// <summary>Maximum HP.</summary>
        private readonly int _maxHealth;

        /// <summary>Current shield.</summary>
        private readonly int _shield;

        /// <summary>Whether enemy remains alive.</summary>
        private readonly bool _isAlive;

        /// <summary>Gets stable party index.</summary>
        public int EnemyIndex => _enemyIndex;

        /// <summary>Gets current HP.</summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>Gets maximum HP.</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>Gets current shield.</summary>
        public int Shield => _shield;

        /// <summary>Gets whether enemy remains alive.</summary>
        public bool IsAlive => _isAlive;

        /// <summary>
        /// Creates immutable enemy status data.
        /// </summary>
        public EnemyStatusSnapshot(int enemyIndex, int currentHealth, int maxHealth, int shield, bool isAlive)
        {
            _enemyIndex = enemyIndex;
            _currentHealth = currentHealth;
            _maxHealth = maxHealth;
            _shield = shield;
            _isAlive = isAlive;
        }
    }

    /// <summary>
    /// Immutable text preview of the next deterministic enemy pattern step.
    /// </summary>
    public readonly struct EnemyIntentSnapshot
    {
        /// <summary>Stable party index.</summary>
        private readonly int _enemyIndex;

        /// <summary>Presentation-ready intent summary generated from action definitions.</summary>
        private readonly string _intentText;

        /// <summary>Gets stable party index.</summary>
        public int EnemyIndex => _enemyIndex;

        /// <summary>Gets next-action intent summary.</summary>
        public string IntentText => _intentText;

        /// <summary>
        /// Creates immutable intent preview data.
        /// </summary>
        public EnemyIntentSnapshot(int enemyIndex, string intentText)
        {
            _enemyIndex = enemyIndex;
            _intentText = intentText;
        }
    }

    /// <summary>
    /// Shared ScriptableObject signal hub used so separate GameObjects communicate through events instead of direct references.
    /// </summary>
    [CreateAssetMenu(menuName = "POC Battle/Runtime/Battle Event Channel", fileName = "BattleEventChannel")]
    public sealed partial class BattleEventChannelSO : ScriptableObject
    {
        /// <summary>Raised by player input when a cardinal move is pressed.</summary>
        public event Action<Vector2Int> MoveInputRequested;

        /// <summary>Legacy click event retained for compatibility with older scene scripts; drag editing no longer uses it.</summary>
        public event Action<Vector2Int> BoardCellClicked;

        /// <summary>Raised on placement-edit left-button press with the logical source coordinate.</summary>
        public event Action<Vector2Int> PlacementDragBeginRequested;

        /// <summary>Raised on placement-edit left-button release over a valid board coordinate.</summary>
        public event Action<Vector2Int> PlacementDragDropRequested;

        /// <summary>Raised when a held edit pointer is released over GUI or outside the board.</summary>
        public event Action PlacementDragCancelRequested;

        /// <summary>Raised by UI when the current editable/player movement phase should end early.</summary>
        public event Action EndPhaseRequested;

        /// <summary>Raised by UI when the POC battle should restart.</summary>
        public event Action RestartRequested;

        /// <summary>Raised after PlayerMovement finishes either a valid slide or invalid punch.</summary>
        public event Action PlayerMoveVisualCompleted;

        /// <summary>Raised after BoardView finishes an authoritative block layout/drop tween window.</summary>
        public event Action BlockLayoutVisualCompleted;

        /// <summary>Raised whenever high-level battle state changes.</summary>
        public event Action<BattlePhase> PhaseChanged;

        /// <summary>Raised whenever player HP/shield changes.</summary>
        public event Action<PlayerStatusSnapshot> PlayerStatusChanged;

        /// <summary>Raised whenever turn move/edit counters change.</summary>
        public event Action<TurnResourceSnapshot> TurnResourcesChanged;

        /// <summary>Raised whenever collected block effect totals change.</summary>
        public event Action<TurnEffectSnapshot> TurnEffectsChanged;

        /// <summary>Raised when all block positions should be synchronized to presentation.</summary>
        public event Action<IReadOnlyList<BlockSnapshot>, bool> BlockLayoutChanged;

        /// <summary>Legacy selection event retained for compatibility; the current edit interaction uses drag visuals.</summary>
        public event Action<int> BlockSelectionChanged;

        /// <summary>Raised by edit logic after validating which runtime block may visually lift from the board.</summary>
        public event Action<int> BlockDragVisualStarted;

        /// <summary>Raised while the edit pointer is held so BoardView can move only the accepted dragged block.</summary>
        public event Action<Vector3> BlockDragPointerMoved;

        /// <summary>Raised when player movement contacts a block so BoardView can play slime-like feedback.</summary>
        public event Action<int> BlockHitVisualRequested;

        /// <summary>Raised when player presentation should snap to a logical coordinate.</summary>
        public event Action<Vector2Int> PlayerPositionSyncRequested;

        /// <summary>Raised when player presentation should animate a movement result.</summary>
        public event Action<PlayerMoveVisualCommand> PlayerMoveVisualRequested;

        /// <summary>Raised once per battle initialization so enemy view instances can be built.</summary>
        public event Action<IReadOnlyList<EnemySetupSnapshot>> EnemyPartySetupRequested;

        /// <summary>Raised whenever one enemy HP/shield changes.</summary>
        public event Action<EnemyStatusSnapshot> EnemyStatusChanged;

        /// <summary>Raised whenever one enemy's next deterministic intent changes.</summary>
        public event Action<EnemyIntentSnapshot> EnemyIntentChanged;

        /// <summary>Raised when an enemy receives player damage for DOTween hit feedback.</summary>
        public event Action<int> EnemyHitVisualRequested;

        /// <summary>Raised when the player receives enemy or trap damage for presentation feedback.</summary>
        public event Action PlayerHitVisualRequested;

        /// <summary>Raised when the battle result changes, including reset to None.</summary>
        public event Action<BattleResult> BattleResultChanged;

        /// <summary>Raises a cardinal movement input request.</summary>
        public void RaiseMoveInputRequested(Vector2Int direction) => MoveInputRequested?.Invoke(direction);

        /// <summary>Raises one legacy edit pointer coordinate selection.</summary>
        public void RaiseBoardCellClicked(Vector2Int coordinate) => BoardCellClicked?.Invoke(coordinate);

        /// <summary>Requests the beginning of one press-hold placement drag from a logical source cell.</summary>
        public void RaisePlacementDragBeginRequested(Vector2Int coordinate) => PlacementDragBeginRequested?.Invoke(coordinate);

        /// <summary>Requests committing one held placement block onto a logical destination cell.</summary>
        public void RaisePlacementDragDropRequested(Vector2Int coordinate) => PlacementDragDropRequested?.Invoke(coordinate);

        /// <summary>Requests cancellation of the currently held placement block.</summary>
        public void RaisePlacementDragCancelRequested() => PlacementDragCancelRequested?.Invoke();

        /// <summary>Raises a request to end the current player phase early.</summary>
        public void RaiseEndPhaseRequested() => EndPhaseRequested?.Invoke();

        /// <summary>Raises a battle restart request.</summary>
        public void RaiseRestartRequested() => RestartRequested?.Invoke();

        /// <summary>Notifies logic that player movement feedback finished.</summary>
        public void RaisePlayerMoveVisualCompleted() => PlayerMoveVisualCompleted?.Invoke();

        /// <summary>Notifies logic that the latest authoritative block layout presentation has settled.</summary>
        public void RaiseBlockLayoutVisualCompleted() => BlockLayoutVisualCompleted?.Invoke();

        /// <summary>Publishes a battle phase change.</summary>
        public void RaisePhaseChanged(BattlePhase phase) => PhaseChanged?.Invoke(phase);

        /// <summary>Publishes player HP/shield state.</summary>
        public void RaisePlayerStatusChanged(PlayerStatusSnapshot snapshot) => PlayerStatusChanged?.Invoke(snapshot);

        /// <summary>Publishes current turn resources.</summary>
        public void RaiseTurnResourcesChanged(TurnResourceSnapshot snapshot) => TurnResourcesChanged?.Invoke(snapshot);

        /// <summary>Publishes current collected effects.</summary>
        public void RaiseTurnEffectsChanged(TurnEffectSnapshot snapshot) => TurnEffectsChanged?.Invoke(snapshot);

        /// <summary>Publishes all runtime block positions.</summary>
        public void RaiseBlockLayoutChanged(IReadOnlyList<BlockSnapshot> snapshots, bool animate) => BlockLayoutChanged?.Invoke(snapshots, animate);

        /// <summary>Publishes a legacy editable block selection id, or -1 for none.</summary>
        public void RaiseBlockSelectionChanged(int blockId) => BlockSelectionChanged?.Invoke(blockId);

        /// <summary>Starts lift/hold presentation for the runtime block accepted by edit logic.</summary>
        public void RaiseBlockDragVisualStarted(int blockId) => BlockDragVisualStarted?.Invoke(blockId);

        /// <summary>Publishes the current world-space edit pointer location while left mouse is held.</summary>
        public void RaiseBlockDragPointerMoved(Vector3 worldPosition) => BlockDragPointerMoved?.Invoke(worldPosition);

        /// <summary>Requests slime-like collision feedback on one stable runtime block id.</summary>
        public void RaiseBlockHitVisualRequested(int blockId) => BlockHitVisualRequested?.Invoke(blockId);

        /// <summary>Requests an immediate player visual position synchronization.</summary>
        public void RaisePlayerPositionSyncRequested(Vector2Int coordinate) => PlayerPositionSyncRequested?.Invoke(coordinate);

        /// <summary>Requests either a valid player slide or invalid-move feedback tween.</summary>
        public void RaisePlayerMoveVisualRequested(PlayerMoveVisualCommand command) => PlayerMoveVisualRequested?.Invoke(command);

        /// <summary>Publishes enemy instances required by the current encounter.</summary>
        public void RaiseEnemyPartySetupRequested(IReadOnlyList<EnemySetupSnapshot> snapshots) => EnemyPartySetupRequested?.Invoke(snapshots);

        /// <summary>Publishes one enemy combat status.</summary>
        public void RaiseEnemyStatusChanged(EnemyStatusSnapshot snapshot) => EnemyStatusChanged?.Invoke(snapshot);

        /// <summary>Publishes one enemy next-turn intent.</summary>
        public void RaiseEnemyIntentChanged(EnemyIntentSnapshot snapshot) => EnemyIntentChanged?.Invoke(snapshot);

        /// <summary>Requests enemy hit feedback for one stable party index.</summary>
        public void RaiseEnemyHitVisualRequested(int enemyIndex) => EnemyHitVisualRequested?.Invoke(enemyIndex);

        /// <summary>Requests player hit feedback.</summary>
        public void RaisePlayerHitVisualRequested() => PlayerHitVisualRequested?.Invoke();

        /// <summary>Publishes battle result or clears the previous result with None.</summary>
        public void RaiseBattleResultChanged(BattleResult result) => BattleResultChanged?.Invoke(result);
    }
}
