using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Resolves one deterministic ice-slide segment. Active cell effects split one manual move into chained segments.
    /// </summary>
    public sealed class GridMovementResolver
    {
        /// <summary>Logical board queried for walls, blocks, cell effects, and player position.</summary>
        private readonly BoardModel _board;

        /// <summary>Creates a movement resolver for one board model.</summary>
        public GridMovementResolver(BoardModel board)
        {
            _board = board;
        }

        /// <summary>
        /// Resolves the next segment in one cardinal direction. Cell effects already used by the current manual move are ignored.
        /// </summary>
        public MoveResult Resolve(Vector2Int direction, ISet<int> disabledCellEffectIds)
        {
            Vector2Int startPosition = _board.PlayerPosition;
            if (!IsCardinalDirection(direction))
            {
                return CreateInvalidResult(startPosition, direction);
            }

            Vector2Int adjacentCoordinate = startPosition + direction;
            if (!_board.IsInside(adjacentCoordinate)
                || _board.IsWall(adjacentCoordinate)
                || _board.GetBlock(adjacentCoordinate) != null)
            {
                return CreateInvalidResult(startPosition, direction);
            }

            CellEffectRuntime adjacentEffect = GetActiveCellEffect(adjacentCoordinate, disabledCellEffectIds);
            if (adjacentEffect != null)
            {
                return new MoveResult(
                    true,
                    startPosition,
                    adjacentCoordinate,
                    direction,
                    MoveStopReason.CellEffect,
                    null,
                    adjacentEffect);
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
                CellEffectRuntime cellEffect = GetActiveCellEffect(probeCoordinate, disabledCellEffectIds);
                if (cellEffect != null)
                {
                    return new MoveResult(
                        true,
                        startPosition,
                        probeCoordinate,
                        direction,
                        MoveStopReason.CellEffect,
                        null,
                        cellEffect);
                }

                probeCoordinate += direction;
            }

            return new MoveResult(true, startPosition, lastFreeCoordinate, direction, MoveStopReason.Boundary, null);
        }

        /// <summary>Compatibility overload for movement without any temporarily disabled cell effects.</summary>
        public MoveResult Resolve(Vector2Int direction)
        {
            return Resolve(direction, null);
        }

        /// <summary>Returns an unused cell effect at a coordinate or null when it should behave like a normal cell.</summary>
        private CellEffectRuntime GetActiveCellEffect(Vector2Int coordinate, ISet<int> disabledCellEffectIds)
        {
            CellEffectRuntime effect = _board.GetCellEffect(coordinate);
            if (effect == null || (disabledCellEffectIds != null && disabledCellEffectIds.Contains(effect.Id)))
            {
                return null;
            }

            return effect;
        }

        /// <summary>Returns whether an input vector represents exactly one cardinal cell direction.</summary>
        private static bool IsCardinalDirection(Vector2Int direction)
        {
            return Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1;
        }

        /// <summary>Creates a standardized zero-distance invalid result.</summary>
        private static MoveResult CreateInvalidResult(Vector2Int startPosition, Vector2Int direction)
        {
            return new MoveResult(false, startPosition, startPosition, direction, MoveStopReason.Invalid, null);
        }
    }
}
