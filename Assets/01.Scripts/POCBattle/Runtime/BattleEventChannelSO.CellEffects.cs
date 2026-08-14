using System;
using System.Collections.Generic;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>Immutable cell-effect presentation snapshot.</summary>
    public readonly struct CellEffectSnapshot
    {
        private readonly int _effectId;
        private readonly CellEffectDefinitionSO _definition;
        private readonly Vector2Int _coordinate;
        private readonly Vector2Int _direction;

        public int EffectId => _effectId;
        public CellEffectDefinitionSO Definition => _definition;
        public Vector2Int Coordinate => _coordinate;
        public Vector2Int Direction => _direction;

        /// <summary>Creates one immutable cell-effect snapshot.</summary>
        public CellEffectSnapshot(int effectId, CellEffectDefinitionSO definition, Vector2Int coordinate, Vector2Int direction)
        {
            _effectId = effectId;
            _definition = definition;
            _coordinate = coordinate;
            _direction = direction;
        }
    }

    /// <summary>Cell-effect extensions for the shared event channel.</summary>
    public sealed partial class BattleEventChannelSO
    {
        public event Action<IReadOnlyList<CellEffectSnapshot>, bool> CellEffectLayoutChanged;
        public event Action CellEffectLayoutVisualCompleted;
        public event Action<int> CellEffectDragVisualStarted;
        public event Action<Vector3> CellEffectDragPointerMoved;
        public event Action CellEffectMoveUsageResetRequested;
        public event Action<int> CellEffectTriggeredVisualRequested;

        /// <summary>Publishes the authoritative cell-effect layout.</summary>
        public void RaiseCellEffectLayoutChanged(IReadOnlyList<CellEffectSnapshot> snapshots, bool animate) => CellEffectLayoutChanged?.Invoke(snapshots, animate);

        /// <summary>Signals that cell-effect edit presentation has settled.</summary>
        public void RaiseCellEffectLayoutVisualCompleted() => CellEffectLayoutVisualCompleted?.Invoke();

        /// <summary>Starts edit drag presentation for one cell effect.</summary>
        public void RaiseCellEffectDragVisualStarted(int effectId) => CellEffectDragVisualStarted?.Invoke(effectId);

        /// <summary>Publishes edit pointer world movement to the active cell-effect drag view.</summary>
        public void RaiseCellEffectDragPointerMoved(Vector3 worldPosition) => CellEffectDragPointerMoved?.Invoke(worldPosition);

        /// <summary>Resets per-manual-move visual activation state.</summary>
        public void RaiseCellEffectMoveUsageResetRequested() => CellEffectMoveUsageResetRequested?.Invoke();

        /// <summary>Dims one triggered cell effect for the current manual move.</summary>
        public void RaiseCellEffectTriggeredVisualRequested(int effectId) => CellEffectTriggeredVisualRequested?.Invoke(effectId);
    }
}
