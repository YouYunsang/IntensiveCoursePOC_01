using System;
using System.Collections.Generic;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Owns physical runtime block copies and places every deck/trap block each player turn.
    /// </summary>
    public sealed class BlockPlacementService
    {
        /// <summary>Exactly four corner cells are permanent walls.</summary>
        private const int CORNER_WALL_COUNT = 4;

        /// <summary>Exactly one cell is occupied by the player.</summary>
        private const int PLAYER_CELL_COUNT = 1;

        /// <summary>Exactly one adjacent cell is reserved to guarantee an initial movement direction.</summary>
        private const int RESERVED_ESCAPE_CELL_COUNT = 1;

        /// <summary>Maximum number of cardinal adjacent coordinates.</summary>
        private const int CARDINAL_DIRECTION_COUNT = 4;

        /// <summary>Board whose occupancy is mutated by placement operations.</summary>
        private readonly BoardModel _board;

        /// <summary>Board rules used for capacity validation.</summary>
        private readonly BattleBoardSettingsSO _boardSettings;

        /// <summary>All persistent runtime block copies for this battle.</summary>
        private readonly List<BlockRuntime> _blocks;

        /// <summary>Reusable list of currently eligible placement coordinates.</summary>
        private readonly List<Vector2Int> _candidateCoordinates;

        /// <summary>Reusable list of valid adjacent cells used to reserve one escape direction.</summary>
        private readonly List<Vector2Int> _adjacentCandidates;

        /// <summary>Deterministic or time-seeded random source used only by placement.</summary>
        private readonly System.Random _random;

        /// <summary>Gets all persistent runtime block copies.</summary>
        public IReadOnlyList<BlockRuntime> Blocks => _blocks;

        /// <summary>
        /// Creates physical deck/trap runtime copies and validates board capacity up front.
        /// </summary>
        public BlockPlacementService(
            BoardModel board,
            BattleBoardSettingsSO boardSettings,
            DeckDefinitionSO deck,
            EncounterDefinitionSO encounter,
            int randomSeed)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _boardSettings = boardSettings != null ? boardSettings : throw new ArgumentNullException(nameof(boardSettings));
            _blocks = new List<BlockRuntime>();
            _candidateCoordinates = new List<Vector2Int>(boardSettings.Columns * boardSettings.Rows);
            _adjacentCandidates = new List<Vector2Int>(CARDINAL_DIRECTION_COUNT);
            _random = new System.Random(randomSeed);
            BuildRuntimeBlocks(deck, encounter);
            ValidateCapacity(deck, encounter);
        }

        /// <summary>
        /// Clears prior occupancy, reserves one adjacent escape cell, and randomly places every runtime block.
        /// </summary>
        public void PlaceAllBlocksForNewTurn()
        {
            _board.ClearBlocks();
            Vector2Int reservedCoordinate = ChooseReservedAdjacentCoordinate();
            RebuildCandidateCoordinates(reservedCoordinate);
            Shuffle(_candidateCoordinates);

            for (int blockIndex = 0; blockIndex < _blocks.Count; blockIndex++)
            {
                _board.PlaceBlock(_blocks[blockIndex], _candidateCoordinates[blockIndex]);
            }
        }

        /// <summary>
        /// Creates one runtime object for every physical player-deck and trap copy.
        /// </summary>
        private void BuildRuntimeBlocks(DeckDefinitionSO deck, EncounterDefinitionSO encounter)
        {
            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            if (encounter == null)
            {
                throw new ArgumentNullException(nameof(encounter));
            }

            int nextBlockId = 0;
            IReadOnlyList<DeckBlockEntry> entries = deck.Entries;
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                DeckBlockEntry entry = entries[entryIndex];
                if (entry == null || entry.Block == null)
                {
                    continue;
                }

                for (int copyIndex = 0; copyIndex < entry.Count; copyIndex++)
                {
                    _blocks.Add(new BlockRuntime(nextBlockId++, entry.Block));
                }
            }

            if (encounter.TrapDefinition != null)
            {
                for (int trapIndex = 0; trapIndex < encounter.TrapCount; trapIndex++)
                {
                    _blocks.Add(new BlockRuntime(nextBlockId++, encounter.TrapDefinition));
                }
            }
        }

        /// <summary>
        /// Validates configured player deck maximum and the guaranteed-movement placement capacity.
        /// </summary>
        private void ValidateCapacity(DeckDefinitionSO deck, EncounterDefinitionSO encounter)
        {
            IReadOnlyList<DeckBlockEntry> entries = deck.Entries;
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                DeckBlockEntry entry = entries[entryIndex];
                if (entry != null && entry.Block != null && entry.Block.IsTrap)
                {
                    throw new InvalidOperationException($"Player deck cannot contain trap block '{entry.Block.DisplayName}'. Traps belong to EncounterDefinitionSO.");
                }
            }

            if (encounter.TrapDefinition != null && !encounter.TrapDefinition.IsTrap)
            {
                throw new InvalidOperationException("Encounter TrapDefinition must reference a BlockDefinitionSO with IsTrap enabled.");
            }

            int deckCount = deck.GetTotalBlockCount();
            if (deckCount > _boardSettings.MaxDeckBlockCount)
            {
                throw new InvalidOperationException(
                    $"Deck contains {deckCount} blocks, exceeding board MaxDeckBlockCount {_boardSettings.MaxDeckBlockCount}.");
            }

            int usableCells = _boardSettings.Columns * _boardSettings.Rows;
            int guaranteedCapacity = usableCells - CORNER_WALL_COUNT - PLAYER_CELL_COUNT - RESERVED_ESCAPE_CELL_COUNT;
            int totalPlacementCount = deckCount + (encounter.TrapDefinition != null ? encounter.TrapCount : 0);

            if (totalPlacementCount > guaranteedCapacity)
            {
                throw new InvalidOperationException(
                    $"Encounter needs {totalPlacementCount} block cells, but guaranteed-movement capacity is {guaranteedCapacity}.");
            }
        }

        /// <summary>
        /// Chooses one normal adjacent player cell that remains empty after random placement.
        /// </summary>
        private Vector2Int ChooseReservedAdjacentCoordinate()
        {
            _adjacentCandidates.Clear();
            AddAdjacentCandidate(Vector2Int.up);
            AddAdjacentCandidate(Vector2Int.down);
            AddAdjacentCandidate(Vector2Int.left);
            AddAdjacentCandidate(Vector2Int.right);

            if (_adjacentCandidates.Count == 0)
            {
                throw new InvalidOperationException("Player position has no adjacent normal cell available for guaranteed movement.");
            }

            int selectedIndex = _random.Next(0, _adjacentCandidates.Count);
            return _adjacentCandidates[selectedIndex];
        }

        /// <summary>
        /// Adds one adjacent normal coordinate to the reserve candidate list.
        /// </summary>
        private void AddAdjacentCandidate(Vector2Int direction)
        {
            Vector2Int coordinate = _board.PlayerPosition + direction;
            if (_board.IsInside(coordinate) && !_board.IsWall(coordinate))
            {
                _adjacentCandidates.Add(coordinate);
            }
        }

        /// <summary>
        /// Rebuilds all block-placement coordinates except walls, player cell, and the reserved escape cell.
        /// </summary>
        private void RebuildCandidateCoordinates(Vector2Int reservedCoordinate)
        {
            _candidateCoordinates.Clear();

            for (int row = 0; row < _board.Rows; row++)
            {
                for (int column = 0; column < _board.Columns; column++)
                {
                    Vector2Int coordinate = new Vector2Int(column, row);
                    if (_board.IsWall(coordinate) || coordinate == _board.PlayerPosition || coordinate == reservedCoordinate)
                    {
                        continue;
                    }

                    _candidateCoordinates.Add(coordinate);
                }
            }

            if (_candidateCoordinates.Count < _blocks.Count)
            {
                throw new InvalidOperationException("Not enough candidate cells to place all blocks.");
            }
        }

        /// <summary>
        /// Performs an in-place Fisher-Yates shuffle without temporary allocations.
        /// </summary>
        private void Shuffle(List<Vector2Int> coordinates)
        {
            for (int index = coordinates.Count - 1; index > 0; index--)
            {
                int swapIndex = _random.Next(0, index + 1);
                Vector2Int temporaryCoordinate = coordinates[index];
                coordinates[index] = coordinates[swapIndex];
                coordinates[swapIndex] = temporaryCoordinate;
            }
        }
    }
}
