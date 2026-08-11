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
    /// Reason a slide movement stopped or failed.
    /// </summary>
    public enum MoveStopReason
    {
        None = 0,
        Block = 1,
        Wall = 2,
        Boundary = 3,
        Invalid = 4
    }

    /// <summary>
    /// Result of one resolved movement input.
    /// </summary>
    public readonly struct MoveResult
    {
        /// <summary>Whether at least one cell of movement is possible.</summary>
        private readonly bool _isValid;

        /// <summary>Grid position before movement.</summary>
        private readonly Vector2Int _startPosition;

        /// <summary>Final player grid position.</summary>
        private readonly Vector2Int _destination;

        /// <summary>Input cardinal direction.</summary>
        private readonly Vector2Int _direction;

        /// <summary>Logical reason movement stopped.</summary>
        private readonly MoveStopReason _stopReason;

        /// <summary>Block collided with at the destination edge, if any.</summary>
        private readonly BlockRuntime _hitBlock;

        /// <summary>Gets whether the move is valid.</summary>
        public bool IsValid => _isValid;

        /// <summary>Gets start position.</summary>
        public Vector2Int StartPosition => _startPosition;

        /// <summary>Gets destination position.</summary>
        public Vector2Int Destination => _destination;

        /// <summary>Gets input direction.</summary>
        public Vector2Int Direction => _direction;

        /// <summary>Gets stop reason.</summary>
        public MoveStopReason StopReason => _stopReason;

        /// <summary>Gets collided block, if one was hit.</summary>
        public BlockRuntime HitBlock => _hitBlock;

        /// <summary>
        /// Creates an immutable movement result.
        /// </summary>
        public MoveResult(
            bool isValid,
            Vector2Int startPosition,
            Vector2Int destination,
            Vector2Int direction,
            MoveStopReason stopReason,
            BlockRuntime hitBlock)
        {
            _isValid = isValid;
            _startPosition = startPosition;
            _destination = destination;
            _direction = direction;
            _stopReason = stopReason;
            _hitBlock = hitBlock;
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
