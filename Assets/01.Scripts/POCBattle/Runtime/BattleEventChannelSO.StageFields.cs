using System;
using System.Collections.Generic;
using PocBattle.Data;

namespace PocBattle.Runtime
{
    /// <summary>Immutable row/column field layout snapshot.</summary>
    public readonly struct StageFieldEffectSnapshot
    {
        private readonly int _fieldEffectId;
        private readonly RowColumnActivationFieldEffectSO _definition;
        private readonly int _specialRow;
        private readonly int _specialColumn;

        public int FieldEffectId => _fieldEffectId;
        public RowColumnActivationFieldEffectSO Definition => _definition;
        public int SpecialRow => _specialRow;
        public int SpecialColumn => _specialColumn;

        /// <summary>Creates one immutable stage-field snapshot.</summary>
        public StageFieldEffectSnapshot(int fieldEffectId, RowColumnActivationFieldEffectSO definition, int specialRow, int specialColumn)
        {
            _fieldEffectId = fieldEffectId;
            _definition = definition;
            _specialRow = specialRow;
            _specialColumn = specialColumn;
        }
    }

    /// <summary>Immutable visual activation request for a special row and/or column.</summary>
    public readonly struct StageFieldActivationSnapshot
    {
        private readonly RowColumnActivationFieldEffectSO _definition;
        private readonly int _specialRow;
        private readonly int _specialColumn;
        private readonly bool _activatesRow;
        private readonly bool _activatesColumn;

        public RowColumnActivationFieldEffectSO Definition => _definition;
        public int SpecialRow => _specialRow;
        public int SpecialColumn => _specialColumn;
        public bool ActivatesRow => _activatesRow;
        public bool ActivatesColumn => _activatesColumn;

        /// <summary>Creates one immutable field activation visual request.</summary>
        public StageFieldActivationSnapshot(RowColumnActivationFieldEffectSO definition, int specialRow, int specialColumn, bool activatesRow, bool activatesColumn)
        {
            _definition = definition;
            _specialRow = specialRow;
            _specialColumn = specialColumn;
            _activatesRow = activatesRow;
            _activatesColumn = activatesColumn;
        }
    }

    /// <summary>Stage-field extensions for the shared event channel.</summary>
    public sealed partial class BattleEventChannelSO
    {
        public event Action<IReadOnlyList<StageFieldEffectSnapshot>> StageFieldEffectLayoutChanged;
        public event Action<StageFieldActivationSnapshot> StageFieldEffectTriggeredVisualRequested;

        /// <summary>Publishes all fixed field effects for the current stage.</summary>
        public void RaiseStageFieldEffectLayoutChanged(IReadOnlyList<StageFieldEffectSnapshot> snapshots) => StageFieldEffectLayoutChanged?.Invoke(snapshots);

        /// <summary>Requests a short row/column flash for one direct block collision.</summary>
        public void RaiseStageFieldEffectTriggeredVisualRequested(StageFieldActivationSnapshot snapshot) => StageFieldEffectTriggeredVisualRequested?.Invoke(snapshot);
    }
}
