using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// High-level battle phases used by the state machine and presentation layer.
    /// </summary>
    public enum BattlePhase
    {
        None = 0,
        PlayerTurnSetup = 1,
        PlacementEdit = 2,
        Movement = 3,
        PlayerBattleResolve = 4,
        EnemyTurn = 5,
        Victory = 6,
        Defeat = 7
    }

    /// <summary>
    /// Logical cell types supported by the POC board.
    /// </summary>
    public enum BoardCellType
    {
        Normal = 0,
        Wall = 1
    }

    /// <summary>
    /// Reason one movement segment stopped or failed.
    /// </summary>
    public enum MoveStopReason
    {
        None = 0,
        Block = 1,
        Wall = 2,
        Boundary = 3,
        Invalid = 4,
        CellEffect = 5
    }

    /// <summary>
    /// Result of one resolved segment inside a single player movement input.
    /// A direction-change cell may cause several valid segments while only the first manual segment consumes a move count.
    /// </summary>
    public readonly struct MoveResult
    {
        private readonly bool _isValid;
        private readonly Vector2Int _startPosition;
        private readonly Vector2Int _destination;
        private readonly Vector2Int _direction;
        private readonly MoveStopReason _stopReason;
        private readonly BlockRuntime _hitBlock;
        private readonly CellEffectRuntime _hitCellEffect;

        /// <summary>Gets whether at least one cell of movement is possible.</summary>
        public bool IsValid => _isValid;

        /// <summary>Gets grid position before this segment.</summary>
        public Vector2Int StartPosition => _startPosition;

        /// <summary>Gets final grid position for this segment.</summary>
        public Vector2Int Destination => _destination;

        /// <summary>Gets the cardinal direction used by this segment.</summary>
        public Vector2Int Direction => _direction;

        /// <summary>Gets logical reason this segment stopped.</summary>
        public MoveStopReason StopReason => _stopReason;

        /// <summary>Gets collided block when the segment stopped immediately before a block.</summary>
        public BlockRuntime HitBlock => _hitBlock;

        /// <summary>Gets the active cell effect entered at this segment destination.</summary>
        public CellEffectRuntime HitCellEffect => _hitCellEffect;

        /// <summary>Creates an immutable movement segment result.</summary>
        public MoveResult(
            bool isValid,
            Vector2Int startPosition,
            Vector2Int destination,
            Vector2Int direction,
            MoveStopReason stopReason,
            BlockRuntime hitBlock,
            CellEffectRuntime hitCellEffect = null)
        {
            _isValid = isValid;
            _startPosition = startPosition;
            _destination = destination;
            _direction = direction;
            _stopReason = stopReason;
            _hitBlock = hitBlock;
            _hitCellEffect = hitCellEffect;
        }
    }

    /// <summary>
    /// Final battle result presented to the player.
    /// </summary>
    public enum BattleResult
    {
        None = 0,
        Victory = 1,
        Defeat = 2
    }
}
