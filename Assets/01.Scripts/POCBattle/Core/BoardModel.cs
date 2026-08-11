using System;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Pure logical board model with O(1) cell and occupancy access.
    /// </summary>
    public sealed class BoardModel
    {
        /// <summary>Board configuration copied by reference as immutable battle data.</summary>
        private readonly BattleBoardSettingsSO _settings;

        /// <summary>Flat cell type array indexed by row-major coordinate.</summary>
        private readonly BoardCellType[] _cellTypes;

        /// <summary>Flat block occupancy array indexed by row-major coordinate.</summary>
        private readonly BlockRuntime[] _occupants;

        /// <summary>Current persistent player coordinate.</summary>
        private Vector2Int _playerPosition;

        /// <summary>Gets current player coordinate.</summary>
        public Vector2Int PlayerPosition => _playerPosition;

        /// <summary>Gets configured number of columns.</summary>
        public int Columns => _settings.Columns;

        /// <summary>Gets configured number of rows.</summary>
        public int Rows => _settings.Rows;

        /// <summary>
        /// Builds cells, assigns the four corner walls, and validates the player start position.
        /// </summary>
        public BoardModel(BattleBoardSettingsSO settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            int cellCount = settings.Columns * settings.Rows;
            _cellTypes = new BoardCellType[cellCount];
            _occupants = new BlockRuntime[cellCount];
            InitializeCornerWalls();

            if (!IsInside(settings.PlayerStartPosition) || IsWall(settings.PlayerStartPosition))
            {
                throw new InvalidOperationException("Player start position must be inside the board and cannot be one of the four corner wall cells.");
            }

            _playerPosition = settings.PlayerStartPosition;
        }

        /// <summary>
        /// Returns whether a coordinate is inside board bounds.
        /// </summary>
        public bool IsInside(Vector2Int coordinate)
        {
            return coordinate.x >= 0 && coordinate.x < Columns && coordinate.y >= 0 && coordinate.y < Rows;
        }

        /// <summary>
        /// Returns the logical type of one valid cell.
        /// </summary>
        public BoardCellType GetCellType(Vector2Int coordinate)
        {
            EnsureInside(coordinate);
            return _cellTypes[ToIndex(coordinate)];
        }

        /// <summary>
        /// Returns whether a coordinate is a wall cell.
        /// </summary>
        public bool IsWall(Vector2Int coordinate)
        {
            return IsInside(coordinate) && _cellTypes[ToIndex(coordinate)] == BoardCellType.Wall;
        }

        /// <summary>
        /// Gets the block occupying a coordinate, or null when empty/outside.
        /// </summary>
        public BlockRuntime GetBlock(Vector2Int coordinate)
        {
            return IsInside(coordinate) ? _occupants[ToIndex(coordinate)] : null;
        }

        /// <summary>
        /// Returns whether a coordinate can hold a block for placement operations.
        /// </summary>
        public bool CanPlaceBlock(Vector2Int coordinate)
        {
            return IsInside(coordinate)
                   && !IsWall(coordinate)
                   && coordinate != _playerPosition
                   && GetBlock(coordinate) == null;
        }

        /// <summary>
        /// Removes every block occupancy while preserving cells and player position.
        /// </summary>
        public void ClearBlocks()
        {
            Array.Clear(_occupants, 0, _occupants.Length);
        }

        /// <summary>
        /// Places one block into a known-valid empty cell.
        /// </summary>
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

            _occupants[ToIndex(coordinate)] = block;
            block.SetCoordinate(coordinate);
        }

        /// <summary>
        /// Moves an editable block to an empty valid coordinate.
        /// </summary>
        public bool TryMoveBlock(Vector2Int source, Vector2Int destination)
        {
            BlockRuntime sourceBlock = GetBlock(source);
            if (sourceBlock == null || sourceBlock.IsTrap || !CanPlaceBlock(destination))
            {
                return false;
            }

            _occupants[ToIndex(source)] = null;
            _occupants[ToIndex(destination)] = sourceBlock;
            sourceBlock.SetCoordinate(destination);
            return true;
        }

        /// <summary>
        /// Swaps two editable player-deck blocks.
        /// </summary>
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
            _occupants[firstIndex] = secondBlock;
            _occupants[secondIndex] = firstBlock;
            firstBlock.SetCoordinate(secondCoordinate);
            secondBlock.SetCoordinate(firstCoordinate);
            return true;
        }

        /// <summary>
        /// Updates the persistent player coordinate after a validated movement result.
        /// </summary>
        public void SetPlayerPosition(Vector2Int coordinate)
        {
            if (!IsInside(coordinate) || IsWall(coordinate) || GetBlock(coordinate) != null)
            {
                throw new InvalidOperationException($"Player cannot occupy board coordinate {coordinate}.");
            }

            _playerPosition = coordinate;
        }

        /// <summary>
        /// Initializes exactly the four corner cells as walls.
        /// </summary>
        private void InitializeCornerWalls()
        {
            SetCellType(new Vector2Int(0, 0), BoardCellType.Wall);
            SetCellType(new Vector2Int(Columns - 1, 0), BoardCellType.Wall);
            SetCellType(new Vector2Int(0, Rows - 1), BoardCellType.Wall);
            SetCellType(new Vector2Int(Columns - 1, Rows - 1), BoardCellType.Wall);
        }

        /// <summary>
        /// Writes one cell type into the flat row-major storage.
        /// </summary>
        private void SetCellType(Vector2Int coordinate, BoardCellType type)
        {
            _cellTypes[ToIndex(coordinate)] = type;
        }

        /// <summary>
        /// Converts a valid coordinate to a flat row-major index.
        /// </summary>
        private int ToIndex(Vector2Int coordinate)
        {
            return coordinate.y * Columns + coordinate.x;
        }

        /// <summary>
        /// Throws for invalid coordinates to make setup errors explicit.
        /// </summary>
        private void EnsureInside(Vector2Int coordinate)
        {
            if (!IsInside(coordinate))
            {
                throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Coordinate is outside the board.");
            }
        }
    }
}
