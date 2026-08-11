using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Resolves deterministic ice-slide movement without relying on Rigidbody collision timing.
    /// </summary>
    public sealed class GridMovementResolver
    {
        /// <summary>Logical board queried for walls, blocks, and player position.</summary>
        private readonly BoardModel _board;

        /// <summary>
        /// Creates a movement resolver for one board model.
        /// </summary>
        public GridMovementResolver(BoardModel board)
        {
            _board = board;
        }

        /// <summary>
        /// Resolves one cardinal input into a final destination and optional collided block.
        /// </summary>
        public MoveResult Resolve(Vector2Int direction)
        {
            Vector2Int startPosition = _board.PlayerPosition;
            if (!IsCardinalDirection(direction))
            {
                return CreateInvalidResult(startPosition, direction);
            }

            Vector2Int adjacentCoordinate = startPosition + direction;
            if (!_board.IsInside(adjacentCoordinate) || _board.IsWall(adjacentCoordinate) || _board.GetBlock(adjacentCoordinate) != null)
            {
                return CreateInvalidResult(startPosition, direction);
            }

            Vector2Int lastFreeCoordinate = adjacentCoordinate;
            Vector2Int probeCoordinate = adjacentCoordinate + direction;

            while (_board.IsInside(probeCoordinate))
            {
                if (_board.IsWall(probeCoordinate))
                {
                    return new MoveResult(true, startPosition, lastFreeCoordinate, direction, MoveStopReason.Wall, null);
                }

                BlockRuntime hitBlock = _board.GetBlock(probeCoordinate);
                if (hitBlock != null)
                {
                    return new MoveResult(true, startPosition, lastFreeCoordinate, direction, MoveStopReason.Block, hitBlock);
                }

                lastFreeCoordinate = probeCoordinate;
                probeCoordinate += direction;
            }

            return new MoveResult(true, startPosition, lastFreeCoordinate, direction, MoveStopReason.Boundary, null);
        }

        /// <summary>
        /// Returns whether an input vector represents exactly one cardinal cell direction.
        /// </summary>
        private static bool IsCardinalDirection(Vector2Int direction)
        {
            return Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1;
        }

        /// <summary>
        /// Creates a standardized zero-distance invalid result.
        /// </summary>
        private static MoveResult CreateInvalidResult(Vector2Int startPosition, Vector2Int direction)
        {
            return new MoveResult(false, startPosition, startPosition, direction, MoveStopReason.Invalid, null);
        }
    }
}
