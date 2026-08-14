using System;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>Base stage-local runtime instance for a field effect that does not occupy a board cell.</summary>
    public abstract class StageFieldEffectRuntime
    {
        private readonly int _id;
        private readonly StageFieldEffectDefinitionSO _definition;

        /// <summary>Gets stable stage-local runtime id.</summary>
        public int Id => _id;

        /// <summary>Gets immutable field-effect definition.</summary>
        public StageFieldEffectDefinitionSO Definition => _definition;

        /// <summary>Creates one stage-local field effect runtime.</summary>
        protected StageFieldEffectRuntime(int id, StageFieldEffectDefinitionSO definition)
        {
            _id = id;
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
        }
    }

    /// <summary>Runtime row/column selection fixed from stage start until that stage ends.</summary>
    public sealed class RowColumnActivationFieldEffectRuntime : StageFieldEffectRuntime
    {
        private readonly int _specialRow;
        private readonly int _specialColumn;

        /// <summary>Gets zero-based special row index.</summary>
        public int SpecialRow => _specialRow;

        /// <summary>Gets zero-based special column index.</summary>
        public int SpecialColumn => _specialColumn;

        /// <summary>Gets typed immutable definition.</summary>
        public RowColumnActivationFieldEffectSO RowColumnDefinition => Definition as RowColumnActivationFieldEffectSO;

        /// <summary>Creates one fixed random row/column runtime.</summary>
        public RowColumnActivationFieldEffectRuntime(
            int id,
            RowColumnActivationFieldEffectSO definition,
            int specialRow,
            int specialColumn)
            : base(id, definition)
        {
            _specialRow = specialRow;
            _specialColumn = specialColumn;
        }

        /// <summary>Returns whether the coordinate belongs to the special row.</summary>
        public bool IsOnSpecialRow(Vector2Int coordinate) => coordinate.y == _specialRow;

        /// <summary>Returns whether the coordinate belongs to the special column.</summary>
        public bool IsOnSpecialColumn(Vector2Int coordinate) => coordinate.x == _specialColumn;
    }
}
