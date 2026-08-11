using PocBattle.Data;
using UnityEngine;

namespace PocBattle.Core
{
    /// <summary>
    /// Mutable runtime identity and coordinate for one physical block copy.
    /// </summary>
    public sealed class BlockRuntime
    {
        /// <summary>Stable runtime block id used by presentation without storing GameObject references.</summary>
        private readonly int _id;

        /// <summary>Immutable data definition shared by all copies of this block type.</summary>
        private readonly BlockDefinitionSO _definition;

        /// <summary>Current logical board position.</summary>
        private Vector2Int _coordinate;

        /// <summary>Gets stable runtime id.</summary>
        public int Id => _id;

        /// <summary>Gets immutable block definition.</summary>
        public BlockDefinitionSO Definition => _definition;

        /// <summary>Gets current logical coordinate.</summary>
        public Vector2Int Coordinate => _coordinate;

        /// <summary>Gets whether this runtime block is an encounter trap.</summary>
        public bool IsTrap => _definition != null && _definition.IsTrap;

        /// <summary>
        /// Creates one runtime block copy.
        /// </summary>
        public BlockRuntime(int id, BlockDefinitionSO definition)
        {
            _id = id;
            _definition = definition;
            _coordinate = new Vector2Int(-1, -1);
        }

        /// <summary>
        /// Updates this block's logical coordinate after a validated board operation.
        /// </summary>
        internal void SetCoordinate(Vector2Int coordinate)
        {
            _coordinate = coordinate;
        }
    }
}
