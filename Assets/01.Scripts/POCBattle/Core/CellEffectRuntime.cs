using System;
using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// One stage-local runtime copy of a player-owned cell effect, with a fresh coordinate and optional direction every turn.
    /// </summary>
    public sealed class CellEffectRuntime
    {
        /// <summary>Stable runtime deck instance id.</summary>
        private readonly int _id;

        /// <summary>Immutable cell effect definition.</summary>
        private readonly CellEffectDefinitionSO _definition;

        /// <summary>Current turn coordinate.</summary>
        private Vector2Int _coordinate;

        /// <summary>Current turn random cardinal direction when required by the definition.</summary>
        private Vector2Int _assignedDirection;

        /// <summary>Gets stable runtime id.</summary>
        public int Id => _id;

        /// <summary>Gets immutable effect definition.</summary>
        public CellEffectDefinitionSO Definition => _definition;

        /// <summary>Gets current turn coordinate.</summary>
        public Vector2Int Coordinate => _coordinate;

        /// <summary>Gets current turn direction.</summary>
        public Vector2Int AssignedDirection => _assignedDirection;

        /// <summary>Creates one runtime cell effect copy.</summary>
        public CellEffectRuntime(int id, CellEffectDefinitionSO definition)
        {
            _id = id;
            _definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _coordinate = new Vector2Int(-1, -1);
            _assignedDirection = Vector2Int.zero;
        }

        /// <summary>Updates authoritative turn placement data.</summary>
        public void SetPlacement(Vector2Int coordinate, Vector2Int assignedDirection)
        {
            _coordinate = coordinate;
            _assignedDirection = assignedDirection;
        }
    }
}
