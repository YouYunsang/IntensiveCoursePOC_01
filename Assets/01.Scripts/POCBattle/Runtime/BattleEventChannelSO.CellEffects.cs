using System;
using System.Collections.Generic;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Runtime
{
    /// <summary>
    /// Immutable cell-effect presentation snapshot kept separate from block occupancy snapshots.
    /// </summary>
    public readonly struct CellEffectSnapshot
    {
        private readonly int _effectId;
        private readonly CellEffectDefinitionSO _definition;
        private readonly Vector2Int _coordinate;
        private readonly Vector2Int _direction;

        /// <summary>Gets stable runtime deck instance id.</summary>
        public int EffectId => _effectId;

        /// <summary>Gets immutable cell effect definition.</summary>
        public CellEffectDefinitionSO Definition => _definition;

        /// <summary>Gets current turn cell coordinate.</summary>
        public Vector2Int Coordinate => _coordinate;

        /// <summary>Gets current turn assigned cardinal direction.</summary>
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

    /// <summary>
    /// Cell-effect extensions for the shared POC event channel.
    /// </summary>
    public sealed partial class BattleEventChannelSO
    {
        /// <summary>Raised when every player-owned cell effect should be synchronized to the board view layer.</summary>
        public event Action<IReadOnlyList<CellEffectSnapshot>> CellEffectLayoutChanged;

        /// <summary>Raised at the beginning of each manual move so effects used by the previous move visually reactivate.</summary>
        public event Action CellEffectMoveUsageResetRequested;

        /// <summary>Raised after a cell effect activates so its view can visibly deactivate for the rest of that manual move.</summary>
        public event Action<int> CellEffectTriggeredVisualRequested;

        /// <summary>Publishes all current player-turn cell effects.</summary>
        public void RaiseCellEffectLayoutChanged(IReadOnlyList<CellEffectSnapshot> snapshots) => CellEffectLayoutChanged?.Invoke(snapshots);

        /// <summary>Requests resetting per-manual-move visual activation state.</summary>
        public void RaiseCellEffectMoveUsageResetRequested() => CellEffectMoveUsageResetRequested?.Invoke();

        /// <summary>Requests visually disabling one triggered cell effect until the next manual move.</summary>
        public void RaiseCellEffectTriggeredVisualRequested(int effectId) => CellEffectTriggeredVisualRequested?.Invoke(effectId);
    }
}
