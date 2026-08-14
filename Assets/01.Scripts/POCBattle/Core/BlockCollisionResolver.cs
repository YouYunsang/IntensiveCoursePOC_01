using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>Immutable description of one row/column field effect triggered by the directly collided block.</summary>
    public readonly struct StageFieldActivation
    {
        private readonly RowColumnActivationFieldEffectRuntime _runtime;
        private readonly bool _activatesRow;
        private readonly bool _activatesColumn;

        /// <summary>Gets triggered field runtime.</summary>
        public RowColumnActivationFieldEffectRuntime Runtime => _runtime;

        /// <summary>Gets whether the special row is triggered.</summary>
        public bool ActivatesRow => _activatesRow;

        /// <summary>Gets whether the special column is triggered.</summary>
        public bool ActivatesColumn => _activatesColumn;

        /// <summary>Creates one field activation descriptor.</summary>
        public StageFieldActivation(
            RowColumnActivationFieldEffectRuntime runtime,
            bool activatesRow,
            bool activatesColumn)
        {
            _runtime = runtime;
            _activatesRow = activatesRow;
            _activatesColumn = activatesColumn;
        }
    }

    /// <summary>
    /// Reusable deterministic collision plan. Index zero is always the directly collided block; later entries are field-effect activations.
    /// </summary>
    public sealed class BlockCollisionPlan
    {
        private readonly List<BlockRuntime> _orderedBlocks;
        private readonly List<StageFieldActivation> _fieldActivations;

        /// <summary>Gets direct + unique field-triggered blocks in deterministic effect order.</summary>
        public IReadOnlyList<BlockRuntime> OrderedBlocks => _orderedBlocks;

        /// <summary>Gets field effects activated by the direct collision.</summary>
        public IReadOnlyList<StageFieldActivation> FieldActivations => _fieldActivations;

        /// <summary>Creates reusable collision-plan storage.</summary>
        internal BlockCollisionPlan(List<BlockRuntime> orderedBlocks, List<StageFieldActivation> fieldActivations)
        {
            _orderedBlocks = orderedBlocks;
            _fieldActivations = fieldActivations;
        }
    }

    /// <summary>
    /// Pure collision planner that applies no combat effects itself. It expands only direct collisions into stage field-effect blocks,
    /// preventing field-triggered blocks from recursively triggering the field again.
    /// </summary>
    public sealed class BlockCollisionResolver
    {
        private readonly BoardModel _board;
        private readonly StageFieldEffectService _fieldEffects;
        private readonly List<BlockRuntime> _orderedBlocks;
        private readonly List<StageFieldActivation> _fieldActivations;
        private readonly HashSet<int> _visitedBlockIds;
        private readonly BlockCollisionPlan _plan;

        /// <summary>Creates reusable collision buffers for one stage battle.</summary>
        public BlockCollisionResolver(BoardModel board, StageFieldEffectService fieldEffects)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _fieldEffects = fieldEffects ?? throw new ArgumentNullException(nameof(fieldEffects));
            _orderedBlocks = new List<BlockRuntime>(board.Columns + board.Rows);
            _fieldActivations = new List<StageFieldActivation>();
            _visitedBlockIds = new HashSet<int>();
            _plan = new BlockCollisionPlan(_orderedBlocks, _fieldActivations);
        }

        /// <summary>
        /// Builds direct-first, row-left-to-right, column-bottom-to-top activation order with stable block-id deduplication.
        /// </summary>
        public BlockCollisionPlan ResolveDirectCollision(BlockRuntime directBlock)
        {
            if (directBlock == null)
            {
                throw new ArgumentNullException(nameof(directBlock));
            }

            _orderedBlocks.Clear();
            _fieldActivations.Clear();
            _visitedBlockIds.Clear();
            AddUniqueBlock(directBlock);

            Vector2Int directCoordinate = directBlock.Coordinate;
            IReadOnlyList<StageFieldEffectRuntime> fields = _fieldEffects.Effects;
            for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
            {
                if (!(fields[fieldIndex] is RowColumnActivationFieldEffectRuntime rowColumnRuntime))
                {
                    continue;
                }

                bool activatesRow = rowColumnRuntime.IsOnSpecialRow(directCoordinate);
                bool activatesColumn = rowColumnRuntime.IsOnSpecialColumn(directCoordinate);
                if (!activatesRow && !activatesColumn)
                {
                    continue;
                }

                _fieldActivations.Add(new StageFieldActivation(rowColumnRuntime, activatesRow, activatesColumn));
                if (activatesRow)
                {
                    AppendSpecialRow(rowColumnRuntime.SpecialRow);
                }

                if (activatesColumn)
                {
                    AppendSpecialColumn(rowColumnRuntime.SpecialColumn);
                }
            }

            return _plan;
        }

        /// <summary>Appends row blocks from lowest X to highest X.</summary>
        private void AppendSpecialRow(int row)
        {
            for (int column = 0; column < _board.Columns; column++)
            {
                AddUniqueBlock(_board.GetBlock(new Vector2Int(column, row)));
            }
        }

        /// <summary>Appends column blocks from lowest Y to highest Y.</summary>
        private void AppendSpecialColumn(int column)
        {
            for (int row = 0; row < _board.Rows; row++)
            {
                AddUniqueBlock(_board.GetBlock(new Vector2Int(column, row)));
            }
        }

        /// <summary>Adds one non-null block only once to prevent intersection/direct duplicate effects.</summary>
        private void AddUniqueBlock(BlockRuntime block)
        {
            if (block == null || !_visitedBlockIds.Add(block.Id))
            {
                return;
            }

            _orderedBlocks.Add(block);
        }
    }
}
