using System;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Pure logical board model with independent O(1) block and cell-effect layers.
    /// </summary>
    public sealed class BoardModel
    {
        /// <summary>Board configuration copied by reference as immutable battle data.</summary>
        private readonly BattleBoardSettingsSO _settings;

        /// <summary>Flat cell type array indexed by row-major coordinate.</summary>
        private readonly BoardCellType[] _cellTypes;

        /// <summary>Flat block occupancy array indexed by row-major coordinate.</summary>
        private readonly BlockRuntime[] _blockOccupants;

        /// <summary>Flat cell-effect occupancy array indexed by row-major coordinate.</summary>
        private readonly CellEffectRuntime[] _cellEffects;

        /// <summary>Current persistent player coordinate.</summary>
        private Vector2Int _playerPosition;

        /// <summary>Gets current player coordinate.</summary>
        public Vector2Int PlayerPosition => _playerPosition;

        /// <summary>Gets configured number of columns.</summary>
        public int Columns => _settings.Columns;

        /// <summary>Gets configured number of rows.</summary>
        public int Rows => _settings.Rows;

        /// <summary>Builds cells, assigns the four corner walls, and validates the player start position.</summary>
        public BoardModel(BattleBoardSettingsSO settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            int cellCount = settings.Columns * settings.Rows;
            _cellTypes = new BoardCellType[cellCount];
            _blockOccupants = new BlockRuntime[cellCount];
            _cellEffects = new CellEffectRuntime[cellCount];
            InitializeCornerWalls();

            if (!IsInside(settings.PlayerStartPosition) || IsWall(settings.PlayerStartPosition))
            {
                throw new InvalidOperationException("Player start position must be inside the board and cannot be one of the four corner wall cells.");
            }

            _playerPosition = settings.PlayerStartPosition;
        }

        /// <summary>Returns whether a coordinate is inside board bounds.</summary>
        public bool IsInside(Vector2Int coordinate)
        {
            return coordinate.x >= 0 && coordinate.x < Columns && coordinate.y >= 0 && coordinate.y < Rows;
        }

        /// <summary>Returns the logical type of one valid cell.</summary>
        public BoardCellType GetCellType(Vector2Int coordinate)
        {
            EnsureInside(coordinate);
            return _cellTypes[ToIndex(coordinate)];
        }

        /// <summary>Returns whether a coordinate is a wall cell.</summary>
        public bool IsWall(Vector2Int coordinate)
        {
            return IsInside(coordinate) && _cellTypes[ToIndex(coordinate)] == BoardCellType.Wall;
        }

        /// <summary>Gets the block occupying a coordinate, or null when empty/outside.</summary>
        public BlockRuntime GetBlock(Vector2Int coordinate)
        {
            return IsInside(coordinate) ? _blockOccupants[ToIndex(coordinate)] : null;
        }

        /// <summary>Gets the cell effect occupying a coordinate, or null when empty/outside.</summary>
        public CellEffectRuntime GetCellEffect(Vector2Int coordinate)
        {
            return IsInside(coordinate) ? _cellEffects[ToIndex(coordinate)] : null;
        }

        /// <summary>Returns whether a coordinate can hold a block for random/edit placement operations.</summary>
        public bool CanPlaceBlock(Vector2Int coordinate)
        {
            return IsInside(coordinate)
                   && !IsWall(coordinate)
                   && coordinate != _playerPosition
                   && GetBlock(coordinate) == null
                   && GetCellEffect(coordinate) == null;
        }

        /// <summary>Returns whether a coordinate can hold one immutable-per-turn cell effect.</summary>
        public bool CanPlaceCellEffect(Vector2Int coordinate)
        {
            return IsInside(coordinate)
                   && !IsWall(coordinate)
                   && coordinate != _playerPosition
                   && GetBlock(coordinate) == null
                   && GetCellEffect(coordinate) == null;
        }

        /// <summary>Removes every block occupancy while preserving cells, cell effects, and player position.</summary>
        public void ClearBlocks()
        {
            Array.Clear(_blockOccupants, 0, _blockOccupants.Length);
        }

        /// <summary>Removes every cell effect while preserving cells, blocks, and player position.</summary>
        public void ClearCellEffects()
        {
            Array.Clear(_cellEffects, 0, _cellEffects.Length);
        }

        /// <summary>Places one block into a known-valid empty cell.</summary>
        public void PlaceBlock(BlockRuntime block, Vector2Int coordinate)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            if (!CanPlaceBlock(coordinate))
            {
                throw new InvalidOperationException($"Cannot place block {block.Id} at {coordinate}.");
            }

            _blockOccupants[ToIndex(coordinate)] = block;
            block.SetCoordinate(coordinate);
        }

        /// <summary>Places one cell effect into a known-valid empty normal cell.</summary>
        public void PlaceCellEffect(CellEffectRuntime cellEffect, Vector2Int coordinate, Vector2Int assignedDirection)
        {
            if (cellEffect == null)
            {
                throw new ArgumentNullException(nameof(cellEffect));
            }

            if (!CanPlaceCellEffect(coordinate))
            {
                throw new InvalidOperationException($"Cannot place cell effect {cellEffect.Id} at {coordinate}.");
            }

            _cellEffects[ToIndex(coordinate)] = cellEffect;
            cellEffect.SetPlacement(coordinate, assignedDirection);
        }

        /// <summary>Moves an editable block to an empty valid coordinate. Cell-effect cells are never valid drop targets.</summary>
        public bool TryMoveBlock(Vector2Int source, Vector2Int destination)
        {
            BlockRuntime sourceBlock = GetBlock(source);
            if (sourceBlock == null || sourceBlock.IsTrap || !CanPlaceBlock(destination))
            {
                return false;
            }

            _blockOccupants[ToIndex(source)] = null;
            _blockOccupants[ToIndex(destination)] = sourceBlock;
            sourceBlock.SetCoordinate(destination);
            return true;
        }

        /// <summary>Swaps two editable player-deck blocks.</summary>
        public bool TrySwapBlocks(Vector2Int firstCoordinate, Vector2Int secondCoordinate)
        {
            BlockRuntime firstBlock = GetBlock(firstCoordinate);
            BlockRuntime secondBlock = GetBlock(secondCoordinate);
            if (firstBlock == null || secondBlock == null || firstBlock.IsTrap || secondBlock.IsTrap)
            {
                return false;
            }

            int firstIndex = ToIndex(firstCoordinate);
            int secondIndex = ToIndex(secondCoordinate);
            _blockOccupants[firstIndex] = secondBlock;
            _blockOccupants[secondIndex] = firstBlock;
            firstBlock.SetCoordinate(secondCoordinate);
            secondBlock.SetCoordinate(firstCoordinate);
            return true;
        }

        /// <summary>Updates the persistent player coordinate after a validated movement result.</summary>
        public void SetPlayerPosition(Vector2Int coordinate)
        {
            if (!IsInside(coordinate) || IsWall(coordinate) || GetBlock(coordinate) != null)
            {
                throw new InvalidOperationException($"Player cannot occupy board coordinate {coordinate}.");
            }

            _playerPosition = coordinate;
        }

        /// <summary>Initializes exactly the four corner cells as walls.</summary>
        private void InitializeCornerWalls()
        {
            SetCellType(new Vector2Int(0, 0), BoardCellType.Wall);
            SetCellType(new Vector2Int(Columns - 1, 0), BoardCellType.Wall);
            SetCellType(new Vector2Int(0, Rows - 1), BoardCellType.Wall);
            SetCellType(new Vector2Int(Columns - 1, Rows - 1), BoardCellType.Wall);
        }

        /// <summary>Writes one cell type into the flat row-major storage.</summary>
        private void SetCellType(Vector2Int coordinate, BoardCellType type)
        {
            _cellTypes[ToIndex(coordinate)] = type;
        }

        /// <summary>Converts a valid coordinate to a flat row-major index.</summary>
        private int ToIndex(Vector2Int coordinate)
        {
            return coordinate.y * Columns + coordinate.x;
        }

        /// <summary>Throws for invalid coordinates to make setup errors explicit.</summary>
        private void EnsureInside(Vector2Int coordinate)
        {
            if (!IsInside(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is outside the board.");
            }
        }
    }
}
