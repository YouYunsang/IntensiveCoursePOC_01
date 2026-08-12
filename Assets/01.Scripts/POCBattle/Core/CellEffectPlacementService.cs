using System;
using System.Collections.Generic;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Owns stage-local runtime cell-effect copies and randomly places/directions them every player turn.
    /// </summary>
    public sealed class CellEffectPlacementService
    {
        /// <summary>Number of cardinal directions used by the current direction-change effect.</summary>
        private const int CARDINAL_DIRECTION_COUNT = 4;

        /// <summary>Board whose cell-effect layer is mutated.</summary>
        private readonly BoardModel _board;

        /// <summary>Persistent stage-local runtime copies sourced from the mutable player deck.</summary>
        private readonly List<CellEffectRuntime> _effects;

        /// <summary>Reusable candidate coordinate list.</summary>
        private readonly List<Vector2Int> _candidateCoordinates;

        /// <summary>Independent deterministic random source derived from this stage's board seed.</summary>
        private readonly System.Random _random;

        /// <summary>Gets all stage-local runtime cell-effect copies.</summary>
        public IReadOnlyList<CellEffectRuntime> Effects => _effects;

        /// <summary>Creates runtime copies for only the cell-effect items currently present in the player's deck.</summary>
        public CellEffectPlacementService(BoardModel board, PlayerDeckModel deck, int randomSeed)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _effects = new List<CellEffectRuntime>();
            _candidateCoordinates = new List<Vector2Int>(board.Columns * board.Rows);
            _random = new System.Random(randomSeed);
            BuildRuntimeEffects(deck);
        }

        /// <summary>Clears previous-turn cell effects before blocks are re-randomized.</summary>
        public void ClearForNewTurn()
        {
            _board.ClearCellEffects();
        }

        /// <summary>Randomly places every runtime cell effect after blocks/traps have taken their cells.</summary>
        public void PlaceAllForNewTurn()
        {
            _board.ClearCellEffects();
            RebuildCandidates();
            Shuffle(_candidateCoordinates);

            if (_candidateCoordinates.Count < _effects.Count)
            {
                throw new InvalidOperationException(
                    $"Not enough empty normal cells to place {_effects.Count} cell effects after block placement.");
            }

            for (int effectIndex = 0; effectIndex < _effects.Count; effectIndex++)
            {
                CellEffectRuntime effect = _effects[effectIndex];
                Vector2Int assignedDirection = effect.Definition.RequiresRandomDirection
                    ? CreateRandomCardinalDirection()
                    : Vector2Int.zero;
                _board.PlaceCellEffect(effect, _candidateCoordinates[effectIndex], assignedDirection);
            }
        }

        /// <summary>Copies player-owned cell effect definitions while preserving stable deck instance ids.</summary>
        private void BuildRuntimeEffects(PlayerDeckModel deck)
        {
            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            IReadOnlyList<DeckItemInstance> items = deck.Items;
            for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
            {
                DeckItemInstance item = items[itemIndex];
                if (item.Definition is CellEffectDefinitionSO definition)
                {
                    _effects.Add(new CellEffectRuntime(item.InstanceId, definition));
                }
            }
        }

        /// <summary>Collects cells that are not walls, player position, blocks/traps, or another cell effect.</summary>
        private void RebuildCandidates()
        {
            _candidateCoordinates.Clear();
            for (int row = 0; row < _board.Rows; row++)
            {
                for (int column = 0; column < _board.Columns; column++)
                {
                    Vector2Int coordinate = new Vector2Int(column, row);
                    if (_board.CanPlaceCellEffect(coordinate))
                    {
                        _candidateCoordinates.Add(coordinate);
                    }
                }
            }
        }

        /// <summary>Returns one of the four cardinal directions with uniform probability.</summary>
        private Vector2Int CreateRandomCardinalDirection()
        {
            int directionIndex = _random.Next(0, CARDINAL_DIRECTION_COUNT);
            switch (directionIndex)
            {
                case 0:
                    return Vector2Int.up;
                case 1:
                    return Vector2Int.right;
                case 2:
                    return Vector2Int.down;
                default:
                    return Vector2Int.left;
            }
        }

        /// <summary>Performs an in-place Fisher-Yates shuffle without temporary allocations.</summary>
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
